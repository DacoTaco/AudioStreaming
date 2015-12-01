using System;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;

namespace AudioStreaming
{
    public class audioClient : NetworkBackend
    {
        //variables 
        private string hostname = "";
        float test = 0;
        public float volume
        {
            get
            {
                return audioPlayer.Volume;
                //return test;
            }
            set
            {
                audioPlayer.Volume = value;
                test = value;
                OnPropertyChanged("volume");    
            }
        }
        public byte BufferLenght
        {
            get
            {
                return audioPlayer.BufferSize;
                //return test;
            }
            private set
            {
                OnPropertyChanged("BufferSize");
            }
        }


        //the audioPlayer using our AudioBackend. this will handle the data and play it
        private AudioPlayer audioPlayer = null;

        public audioClient()
        {
            audioPlayer = new AudioPlayer();
            volume = 100;
            return;
        }

        public void StartConnection(string _hostname, bool compressData, bool _mp3Mode)
        {
            //we dont want to have the client run twice
            if (ThreadAlive)
                return;

            hostname = _hostname;
            compressed = compressData;
            mp3Mode = _mp3Mode;
            Thread oThread = new Thread(new ThreadStart(this.ConnectToServer));
            ThreadAlive = false;
            killThread = false;
            oThread.Start();
            return;
            //ConnectToServer();
        }

        //connect to the server
        private void ConnectToServer()
        {
            if (hostname == null || hostname.Length <= 0)
            {
                System.Windows.MessageBox.Show("Invalid hostname entered!");
                return;
            }

            //set the variable to show that our thread is indeed alive and kicking
            ThreadAlive = true;
            killThread = false;

            //currently we force the compressed mode disabled when in mp3 mode.
            //maybe we'll allow compression of mp3 packets but i doubt we would win anything from it
            /*if (mp3Mode == true)
                compressed = false;*/

            //set the socket as a IPv4,stream, TCP/IP socket
            clientSocket = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            //lets connect!
            this.Connect(hostname, 8666);

            //if we are connected, we check if nicely and start the handshake with the server
            //unlike the server, i haven't found a good way to handle it in the background so we are stuck in the while loop
            if (clientSocket.Connected)
            {
                while (CheckConnection() && (!killThread))
                {
                    error = 0;
                    connected = 1;
                    byte[] buffer = null;
                    int size = 0; 
                    

                    if (connection_init == 0)
                    {
                        //start handshake
                        byte[] msg = { 0xDE, 0xAD, 0xFF, 0xFF, (byte)((compressed) ? 0x01 : 0x00), (byte)((mp3Mode) ? 0x01 : 0x00) };
                        size = SendData(Protocol.INIT_REQ,msg);
                        if (size < msg.Length)
                        {
                            //failed to send the init data asking for the info
                            error = Error.GEN_NET_FAIL;
                            break;
                        }

                        //get the response & validate it
                        size = GetData(ref buffer);

                        if (size < 0x0e || buffer[0] != Protocol.INIT_REQ_RESPONSE 
                            || ByteConversion.ByteArrayToUInt(buffer, 5) == 0xDEADFFFF || (buffer[7] << 8) + buffer[8] != audioPlayer._VERSION)
                        {
                            error = Error.RESPONSE_FAIL;
                            break;
                        }

                        //recompile the information from the packet and use it
                        int samplerate = (buffer[9] << 8) + buffer[10];
                        int channels = buffer[11];
                        compressed = (buffer[buffer.Length -2] != 0) ? true : false;
                        mp3Mode = (buffer[buffer.Length - 1] != 0) ? true : false;

                        if (!mp3Mode)
                        {
                            audioPlayer.SetWaveFormat(samplerate, channels);
                        }

                        //send that we were able to init.
                        size = SendData(Protocol.INIT_ACK,null);
                        if (size < 0)
                            error = Error.GEN_NET_FAIL;

                        //connection is init!
                        //if we aren't running in mp3mode then lets start the audiobackend already
                        //in mp3Mode we will wait for the first frame
                        if(mp3Mode == false)
                        {
                            audioPlayer.StartPlaying();
                        }
                        connection_init = 1;
                    }
                    else
                    {
                        size = GetData(ref buffer);
                        if ( size > 0)
                        {
                            if (size - 5 <= 0)
                                continue;
                            size -= 5;
                            //we received data from the server! strip header and pass it on to the bufferedProvider!
                            byte[] data = new byte[size];
                            Array.Copy(buffer, 5, data, 0, size);
                            //we have data to process!
                            switch (buffer[0])
                            {
                                //we received data to play!
                                case Protocol.REINIT_BACKEND:
                                case Protocol.SEND_DATA:
                                    try
                                    {
                                        if (compressed)
                                            data = Compressor.Decompress(data);

                                        if (mp3Mode)
                                        {
                                            //we received command to reinit the backend
                                            if (buffer[0] == Protocol.REINIT_BACKEND)
                                            {
                                                //wait for all data to be played
                                                while (audioPlayer.WaitForMoreData() > 0)
                                                {
                                                }
                                                //stop player, and then add the next frame. this will reinit the player
                                                audioPlayer.StopPlaying();
                                            }
                                            audioPlayer.AddNextFrame(data);
                                        }
                                        else
                                        {
                                            AddDataToBuffer(ref data);
                                        }

                                        BufferLenght = audioPlayer.BufferSize;
                                        audioPlayer.WaitForMoreData();
                                    }
                                    catch (Exception)
                                    {
                                        throw;
                                    }

                                    //we passed the data to the wave backend. send the ACK that we received it fine and we can get more data
                                    //NOTE : currently ACK is disabled deu to audio lag then. im guessing the PCM is to much and going to fast to send an ack in between
                                    if (mp3Mode)
                                    {
                                        SendData(Protocol.SEND_DATA_ACK, null);
                                    }
                                    break;
                                default:
                                    //do nothing
                                    break;
                            }
                        }
                        //network error
                        if (size < 0)
                        {
                            error = Error.GEN_NET_FAIL;
                        }

                    }
                    if(error != Error.NONE)
                        break;
                }

            }
            else
            {
                //failed to connect lol
                System.Windows.MessageBox.Show("Error connecting to Server : " + hostname + " !");
                Debug.WriteLine("CLIENT : CONNECTION FAILURE");
            }
            //ERRORZ
            if (error != 0)
                Debug.WriteLine("CLIENT : ERROR {0}", error);

            CloseClient();
            Debug.WriteLine(" >> exit client");
            return;
        }

        //add the received data to the AudioBackend's buffer
        private void AddDataToBuffer(ref byte[] data)
        {
            if (data == null || data.Length <= 0)
                return;

            audioPlayer.AddSamples(ref data);
            
            return;
        }

        //KILL IT WITH FIRE
        //...please dont... :(
        private void CloseClient()
        {
            CleanupNetworking();

            audioPlayer.StopPlaying();

            ThreadAlive = false;
            killThread = false;

        }

    }
}
