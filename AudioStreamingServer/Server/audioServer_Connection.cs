
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Windows.Controls;
using System.Threading;

namespace AudioStreaming
{
    public partial class audioServer : NetworkBackend
    {
        
        //----------------------
        //variables
        //----------------------
        private byte serverStarted = 0;
        private byte data_send = 0;
        private TcpListener serverSocket = null;   


        //----------------------
        //functions
        //----------------------

        /// <summary>
        /// Stop the server
        /// </summary>
        public void StopServer()
        {
            if (mp3Mode)
            {
                //this kills the thread which should have exited cleanly and cleaned up after itself
                KillThread(); 
            }
            else
            {
                if (audioPlayer != null)
                    audioPlayer.StopRecording();

                closeServer();
            }
            if (playedListIndexes.Count > 0)
                playedListIndexes.Clear();
        }
        public void StartServer(int indexDevice)
        {
            if (ThreadAlive)
                return;

            deviceIndex = indexDevice;
            Thread oThread = new Thread(new ThreadStart(this.Server));
            oThread.Name = "Server main Thread";
            ThreadAlive = false;
            killThread = false;
            oThread.Start();
            return;
            //Server();

        }
        //Start's the audio server.
        public void Server()
        {
            if (ThreadAlive == true)
                return;

            if (mp3Path == null)
                throw new ArgumentNullException("StartServer : Path is null!");

            //server has officially started
            serverStarted = 1;

            //Start & setup the listeningsocket
            Networking.SetupListener(ref serverSocket, 8666);

            clientSocket = null;
            serverSocket.Start();

            Debug.WriteLine(" >> Server Started");
            Debug.WriteLine("The local End point is  :" + serverSocket.LocalEndpoint);

            //setup the threading var's
            ThreadAlive = true;
            killThread = false;

            //wait for a connection & accept it
            clientSocket = serverSocket.AcceptSocket();
            Debug.WriteLine(" >> Accept connection from client @ " + clientSocket.RemoteEndPoint);

            error = Error.NONE;
            int lenght = 0;
            byte[] bytesFrom = null;

            //connection isn't init and handshake isn't done. so lets do that first.
            if (connection_init == 0)
            {
                //receive data from client. as first packet we expect a YO CAN I HAZ INFO? packet
                while( lenght < 6)
                    lenght = GetData(ref bytesFrom);

                if (lenght > 0 || lenght == 0x0c)
                {
                    /*string hex = "0x" + BitConverter.ToString(bytesFrom);
                    hex = hex.Replace("-", " 0x");
                    Debug.WriteLine(" >> Data from client - {0}", hex);
                    Debug.WriteLine(" lenght : {0}", lenght);*/

                    //check if the packet is correct. it should be 0xINIT_REQ 0x00 0x00 0x00 0x0A 0xDE 0xAD 0xFF 0xFF 0xcompressed
                    if (bytesFrom[0] == Protocol.INIT_REQ && ByteConversion.ByteArrayToUInt(bytesFrom, Protocol.CommandHeaderSize) == 0xDEADFFFF)
                    {
                        //if the compress byte is 0, we wont compress the data. else we will
                        compressed = (bytesFrom[10] > 0) ? true : false;
                        mp3Mode = (bytesFrom[11] > 0) ? true : false;
                        int samples = 0;
                        int channels = 0;


                        if (mp3Mode)
                        {
                            GeneratePlayList();
                            OpenMp3File(true);
                            NAudio.Wave.Mp3Frame frame = audioPlayer.GetNextMp3Frame();
                            if (frame == null)
                            {
                                //failed to read file! abort!
                                error = Error.MP3_READ_ERROR;
                                closeServer();
                                return;
                            }
                            samples = frame.SampleRate;
                            channels = frame.ChannelMode == NAudio.Wave.ChannelMode.Mono ? 1 : 2;
                            audioPlayer.RewindMP3();
                        }
#if !API_REBUILD
                        else
                        {

                            audioPlayer.StartRecording(deviceIndex, SendAudioData);
                            samples = audioPlayer.GetWaveSamples();
                            channels = audioPlayer.GetWaveChannels();

                        }
#endif

                        //generate response. which will contain the response (DEAD) and the wave format followed by an ACK that we will compress or not
#if API_REBUILD
                        Byte[] sendBytes = new byte[5];
#else
                        Byte[] sendBytes = new byte[9];
#endif
                        sendBytes[0] = 0xDE;
                        sendBytes[1] = 0xAD;
                        sendBytes[2] = ByteConversion.ByteFromInt(audioPlayer._VERSION, 2);
                        sendBytes[3] = ByteConversion.ByteFromInt(audioPlayer._VERSION, 3);
                        //and this is why we need the samplerate and channels
#if API_REBUILD
                        sendBytes[4] = (byte)(compressed ? 0x01 : 0x00);
#else
                        sendBytes[4] = ByteConversion.ByteFromInt(samples, 2);
                        sendBytes[5] = ByteConversion.ByteFromInt(samples, 3);
                        sendBytes[6] = ByteConversion.ByteFromInt(channels, 3);
                        sendBytes[7] = (byte)(compressed ? 0x01 : 0x00);
                        sendBytes[8] = (byte)(mp3Mode ? 0x01 : 0x00);
#endif



                        int ret = SendData(Protocol.INIT_REQ_RESPONSE, sendBytes);
                        if (ret != sendBytes.Length || clientSocket.Connected == false)
                        {
                            closeServer();
                            error = Error.GEN_NET_FAIL;
                        }

                        //check if response was good or not
                        if (GetData(ref bytesFrom) != Protocol.CommandHeaderSize || bytesFrom[0] != Protocol.INIT_ACK)
                        {
                            error = Error.RESPONSE_FAIL;
                        }

                        Debug.WriteLine("Connection init successful!");
                        connection_init = 1;
                    }
                    else
                    {
                        Debug.WriteLine("received {0:X}", ByteConversion.ByteArrayToInt(bytesFrom, 2));
                        error = Error.RESPONSE_FAIL;
                    }
                }
                //error occured in the connection
                else if (lenght < 0)
                {
                    error = Error.GEN_NET_FAIL;
                }
                else
                    error = Error.INIT_FAIL;
            }
            if (error != Error.NONE)
            {
                //an error occured while init. bail out!
                closeServer();
                return;
            }
            else
            {
                //if there is no error or the connection is init we will break for now
                //we can do this cause mainly the sending of the audio is dealt by the AudioBackend & SendData
                //no use keeping the thread alive and doing nothing...
                if (mp3Mode == false && (error != Error.NONE || connection_init > 0))
                {
                    return;
                }

                //while the connection is there, try to init and see if more is needed to be done
                while ((killThread == false) && (CheckConnection()) )
                {
                    //byte[] buffer = null;
                    if (DataAvailable() > 0)
                    {
                        int size = GetData(ref bytesFrom);

                        if (size > 0)
                        {
                            switch (bytesFrom[0])
                            {
                                case Protocol.RECQ_PREV_SONG:
                                    Debug.WriteLine("Server : Protocol.RECQ_PREV_SONG Detected!");
                                    OpenPreviousFile();
                                    goto case Protocol.RECQ_TITLE;
                                case Protocol.RECQ_NEXT_SONG:
                                    Debug.WriteLine("Server : Protocol.RECQ_NEXT_SONG Detected!");
                                    OpenMp3File();
                                    goto case Protocol.RECQ_TITLE;
                                case Protocol.RECQ_TITLE:
                                    SendNewTitle();
                                    break;

                                case Protocol.NOP:
                                    SendData(Protocol.NOP, null);
                                    break;

                                case Protocol.RECQ_SEND_MULTI_DATA:
                                case Protocol.RECQ_REINIT_MP3:

                                    byte command = bytesFrom[0];
                                    byte subCommand = 0;
                                    NAudio.Wave.Mp3Frame frame = null;
                                    byte[] header = new byte[5];
                                    byte[] data = new byte[1];
                                    int[] indexes = new int[1];
                                    indexes[0] = 0x00;


                                    frame = audioPlayer.GetNextMp3Frame();
                                    if (frame == null)
                                    {
                                        SendData(Protocol.NOP, null);
                                        error = Error.MP3_READ_ERROR;
                                        break;
                                    }

                                    if (command == Protocol.RECQ_REINIT_MP3)
                                    {
                                        command = Protocol.REINIT_BACKEND;

                                        header[0] = 1;
                                        header[1] = 0; //index of the next frame
                                        header[2] = 0x05;
                                        header[3] = ByteConversion.ByteFromInt(frame.RawData.Length, 2); //size
                                        header[4] = ByteConversion.ByteFromInt(frame.RawData.Length, 3);
                                        data = frame.RawData;
                                    }
                                    else if (command == Protocol.RECQ_SEND_MULTI_DATA)
                                    {
                                        command = Protocol.SEND_MULTI_DATA;

                                        for (byte i = 0; i < 25; i++)
                                        {
                                            if (frame != null)
                                            {
                                                int oldSize = 0;
                                                if (i != 0)
                                                    oldSize = data.Length;

                                                //increase header for the new frame
                                                header[0] = Convert.ToByte(i + 1);// the amount of frames in the packet
                                                Array.Resize(ref header, (header[0] * 4) + 1);

                                                //add index for new frame
                                                Array.Resize(ref indexes, indexes.Length + 1);
                                                indexes[i] = oldSize;

                                                //set the lenght in header
                                                header[(i * 4) + 3] = ByteConversion.ByteFromInt(frame.RawData.Length, 2); //size
                                                header[(i * 4) + 4] = ByteConversion.ByteFromInt(frame.RawData.Length, 3);

                                                Array.Resize(ref data, frame.RawData.Length + oldSize);
                                                Array.Copy(frame.RawData, 0, data, oldSize, frame.RawData.Length);
                                            }
                                            else
                                            {
                                                //frame failed to load. soooooooooo
                                                //its either EOF or read error. in both cases we need to signal the client and server will load new file
                                                //on next loop
                                                subCommand = Protocol.SEND_MULTI_EOF_SUBCOM;
                                                break;
                                            }

                                            if (i < 24)
                                                frame = audioPlayer.GetNextMp3Frame();
                                        }
                                        //complete packet
                                        for (int i = 0; i < header[0]; i++)
                                        {
                                            header[(i * 4) + 1] = ByteConversion.ByteFromInt(indexes[i] + header.Length, 2); //index of the next frame
                                            header[(i * 4) + 2] = ByteConversion.ByteFromInt(indexes[i] + header.Length, 3);
                                        }

                                    }

                                    //copy header + data into buffer to pass on to the compressor/sending
                                    byte[] _tempData = new byte[1];
                                    Array.Resize(ref _tempData, data.Length + header.Length);
                                    Array.Copy(header, _tempData, header.Length);
                                    Array.Copy(data, 0, _tempData, header.Length, data.Length);

                                    data = _tempData;

                                    //compress that shit!
                                    if (compressed)
                                        data = Compressor.Compress(data);

                                    int ret = SendData(command, subCommand, data);
                                    break;

                                case Protocol.KILL_CONNECTION:
                                case Protocol.SEND_DATA:
                                default:
                                    string hex = "0x" + BitConverter.ToString(bytesFrom);
                                    hex = hex.Replace("-", " 0x");
                                    Debug.WriteLine(" >> Data from client - {0}", hex);
                                    break;
                            }

                        }
                        else if (size < 0)
                        {
                            error = Error.GEN_NET_FAIL;
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            if (error != Error.NONE || CheckConnection() != true )
            {
                Debug.WriteLine(" >> exit server " + (
                    (error != Error.NONE) ?
                    (" -> connection closed deu to error : " + error) : ""
                    ));
                closeServer();
            }
            else if(killThread)
                closeServer();

            ThreadAlive = false;
            return;
        }

        private void SendNewTitle()
        {
            byte[] data = compressed ? Compressor.Compress(System.Text.Encoding.UTF8.GetBytes(SongName)) : System.Text.Encoding.UTF8.GetBytes(SongName);

            SendData(Protocol.NEW_TITLE, data);
        }
        
        //send the Audio, in the eventarg's buffer , to the client.
        //TODO : add compression and play with the ACK when getting device output
        private void SendAudioData(object sender, NAudio.Wave.WaveInEventArgs e)
        {
            //the event args has a buffer with said data. its raw pcm, but over lan it'll do :D
            if (serverStarted > 0 && connection_init > 0 && data_send == 0)
            {
                //currently the ACK is disabled because with the PCM data being so big it causes lag on audio
                //data_send = 1;
                byte[] data = null;

                if (compressed)
                {
                    data = Compressor.Compress(e.Buffer);
                }
                else
                {
                    data = e.Buffer;
                }
                int ret = SendData(Protocol.SEND_DATA, data);
                //Debug.WriteLine("Compressed size: {0:F2}%",100 * ((double)data.Length / (double)e.Buffer.Length));

                if (ret != data.Length)
                    closeServer();
            }
            else if (data_send == 1)
            {
                //waiting for the SEND_DATA_ACK
                byte[] buffer = null;
                int ret = GetData(ref buffer);
                if (ret > 0 && ( buffer[0] == Protocol.SEND_DATA_ACK || buffer[0] == Protocol.SEND_MULTI_ACK ) )
                {
                    data_send = 0;
                }
                else
                {
                    //wrong response. kill connection
                    closeServer();
                }
            }

            return;
        }

        //kill server. shutdown socket, close it, and reset everything of the networking
        private void closeServer()
        {
            if (serverStarted == 1)
            {
                CleanupNetworking();

                serverSocket.Stop();
                serverStarted = 0;
                data_send = 0;
            }

            ThreadAlive = false;
            killThread = false;
        }
    }
}
