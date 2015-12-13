
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Windows.Controls;
using System.Threading;

namespace AudioStreaming
{
    class audioServer : NetworkBackend
    {
        public audioServer()
        {
            audioPlayer = new AudioRecorder();
            GetDevices();
            return;
        }

        //----------------------
        //subclasses
        //----------------------
        /// <summary>
        /// class to contain the device information
        /// </summary>
        public class Device
        {
            public string Device_name { get; set; }
            public int Channels { get; set; }
        }

        //----------------------
        //variables
        //----------------------
        public IList<Device> Devices { get; set; }
        private int deviceIndex = 0;

        private byte serverStarted = 0;
        private byte data_send = 0;
        private TcpListener serverSocket = null;

        private string mp3Path = "C:\\";
        private List<string> filesList = null;

        AudioRecorder audioPlayer = null;      


        //----------------------
        //functions
        //----------------------

        /// <summary>
        /// Gets all audio devices installed on the device and store them in 'Devices' which is data linked to the GUI
        /// </summary>
        private void GetDevices()
        {
            List<NAudio.Wave.WaveInCapabilities> devices = new List<NAudio.Wave.WaveInCapabilities>();

            //gets the input (wavein) devices and adds them to the list
            for (short i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++)
            {
                devices.Add(NAudio.Wave.WaveIn.GetCapabilities(i));
            }

            //claer the listview module
            Devices = new List<Device>();

            //each device gets inserted into the devices list, which is linked to the listdevices listview module
            foreach (var device in devices)
            {
                ListViewItem item = new ListViewItem();
                item.Content = device.ProductName;
                Devices.Add(new Device() { Device_name = device.ProductName, Channels = device.Channels });
            }
        }

        public int GetDevicesCount()
        {
            return Devices.Count;
        }

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
        }
        public void StartServer(int indexDevice, string _mp3Path)
        {
            if (ThreadAlive)
                return;

            mp3Path = _mp3Path;
            deviceIndex = indexDevice;
            Thread oThread = new Thread(new ThreadStart(this.Server));
            oThread.Name = "Server main Thread";
            ThreadAlive = false;
            killThread = false;
            oThread.Start();
            return;
            //Server();

        }
        //Start's the audio server. this needs the samplerate & channels cause we need to send these to the client during init
        public void Server()
        {
            if (ThreadAlive == true)
                return;

            if (mp3Path == null)
                throw new ArgumentNullException("StartServer : Path is null!");

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

            //server has officially started
            serverStarted = 1;
            error = Error.NONE;
            int lenght = 0;
            byte[] bytesFrom = null;

            //connection isn't init and handshake isn't done. so lets do that first.
            if (connection_init == 0)
            {
                //receive data from client. as first packet we expect a YO CAN I HAZ INFO? packet
                lenght = GetData(ref bytesFrom);

                if (lenght > 0)
                {
                    
                    //if the packet was < 11 bytes, it failed
                    if (lenght < 11)
                    {
                        error = Error.INIT_FAIL;
                    }


                    string hex = "0x" + BitConverter.ToString(bytesFrom);
                    hex = hex.Replace("-", " 0x");
                    Debug.WriteLine(" >> Data from client - {0}", hex);
                    Debug.WriteLine(" lenght : {0}", lenght);

                    //check if the packet is correct. it should be 0xINIT_REQ 0x00 0x00 0x00 0x0A 0xDE 0xAD 0xFF 0xFF 0xcompressed
                    if (bytesFrom[0] == Protocol.INIT_REQ && ByteConversion.ByteArrayToUInt(bytesFrom, 5) == 0xDEADFFFF)
                    {
                        //if the compress byte is 0, we wont compress the data. else we will
                        compressed = (bytesFrom[9] > 0) ? true : false;
                        mp3Mode = (bytesFrom[10] > 0) ? true : false;
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
                        else
                        {
                            audioPlayer.StartRecording(deviceIndex, SendData);
                            samples = audioPlayer.GetWaveSamples();
                            channels = audioPlayer.GetWaveChannels();
                        }

                        //generate response. which will contain the response (DEAD) and the wave format followed by an ACK that we will compress or not
                        Byte[] sendBytes = new byte[9];
                        sendBytes[0] = 0xDE;
                        sendBytes[1] = 0xAD;
                        sendBytes[2] = ByteConversion.ByteFromInt(audioPlayer._VERSION, 2);
                        sendBytes[3] = ByteConversion.ByteFromInt(audioPlayer._VERSION, 3);
                        //and this is why we need the samplerate and channels
                        sendBytes[4] = ByteConversion.ByteFromInt(samples, 2);
                        sendBytes[5] = ByteConversion.ByteFromInt(samples, 3);
                        sendBytes[6] = ByteConversion.ByteFromInt(channels, 3);
                        sendBytes[7] = (byte)(compressed ? 0x01 : 0x00);
                        sendBytes[8] = (byte)(mp3Mode ? 0x01 : 0x00);




                        if (SendData(Protocol.INIT_REQ_RESPONSE, sendBytes) != sendBytes.Length || clientSocket.Connected == false)
                        {
                            closeServer();
                            error = Error.GEN_NET_FAIL;
                        }

                        //check if response was good or not
                        if (GetData(ref bytesFrom) != 5 || bytesFrom[0] != Protocol.INIT_ACK)
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
            }
            if (error != Error.NONE)
            {
                //an error occured while init. bail out!
                closeServer();
                return;
            }
            else
            {
                /*if (mp3Mode)
                {
                    //OpenMp3File(true);
                    audioPlayer.OpenMp3File(@"H:\stuff\MP3's\rob zombie\Rob Zombie - Hellbilly Deluxe (MP3@320 kbps)\01. Rob Zombie - Call Of The Zombie.mp3");//"H:\stuff\MP3's\various\Imagine Dragons - Warriors.mp3");
                }*/
                //while the connection is there, try to init and see if more is needed to be done
                while (CheckConnection() && killThread == false)
                {
                    if (mp3Mode)
                    {
                        NAudio.Wave.Mp3Frame frame = null;
                        byte command = Protocol.SEND_DATA;
                        byte[] data = new byte[1];


                        //multi-frame function
                        //--------------------------
                        byte[] header = new byte[5];
                        int[] indexes = new int[1];
                        indexes[0] = 0x00;

                        //compile header for the first frame
                        header[0] = 1;
                        header[1] = header[3] = header[4] = 0;
                        header[2] = 0x05;

                        for (byte i = 0; i < 25; i++)
                        {
                            frame = audioPlayer.GetNextMp3Frame();
                            if (frame == null)
                            {
                                if (i == 0)
                                {
                                    OpenMp3File();
                                    frame = audioPlayer.GetNextMp3Frame();
                                    if (frame == null)
                                    {
                                        error = Error.MP3_READ_ERROR;
                                        break;
                                    }
                                    //compare the frame with the waveform from the last file.
                                    if (!audioPlayer.IsWaveformatEqual(frame))
                                    {
                                        //the frame is in a different format. we need to let the client know!
                                        header[0] = 1;
                                        header[1] = 0; //index of the next frame
                                        header[2] = 0x05;
                                        header[3] = ByteConversion.ByteFromInt(frame.RawData.Length, 2); //size
                                        header[4] = ByteConversion.ByteFromInt(frame.RawData.Length, 3);
                                        data = frame.RawData;
                                        command = Protocol.REINIT_BACKEND;
                                    }
                                }
                                break;
                            }
                            else
                            {
                                int oldSize = 0;
                                if(i != 0)
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
                                Array.Copy(frame.RawData,0, data,oldSize,frame.RawData.Length);
                            }
                        }
                        if (command != Protocol.REINIT_BACKEND)
                        {
                            //complete packet
                            for (int i = 0; i < header[0]; i++)
                            {
                                header[(i * 4) + 1] = ByteConversion.ByteFromInt(indexes[i] + header.Length, 2); //index of the next frame
                                header[(i * 4) + 2] = ByteConversion.ByteFromInt(indexes[i] + header.Length, 3);
                            }
                            
                        }

                        byte[] _tempData = new byte[1];
                        Array.Resize(ref _tempData, data.Length + header.Length);
                        Array.Copy(header, _tempData, header.Length);
                        Array.Copy(data, 0, _tempData, header.Length, data.Length);

                        command = Protocol.SEND_MULTI_DATA;
                        data = _tempData;

                        //compress that shit!
                        if(compressed)
                            data = Compressor.Compress(data);

                        int ret = SendData(command, data);

                        if (ret < 0)
                            break;

                        byte[] buffer = null;
                        do
                        {
                            ret = GetData(ref buffer);
                            if (ret > 0 && ( buffer[0] == Protocol.SEND_DATA_ACK || buffer[0] == Protocol.SEND_MULTI_ACK) )
                            {
                                //continue;
                                break;
                            }
                            else if (ret > 0)
                            {
                                //wrong response. kill connection
                                closeServer();
                                return;
                            }
                            if (ret < 0)
                            {
                                error = Error.RESPONSE_FAIL;
                                break;
                            }
                            else
                            {
                                //no response from client yet, so we wait before we send more data
                                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
                            }
                        } while (true);
                    }
                    else
                    {
                        //in the future we can use this switch for future communication
                        /*switch (bytesFrom[0])
                        {
                            default:
                                break;
                        }*/

                        //if there is no error or the connection is init we will break for now
                        //we can do this cause mainly the sending of the audio is dealt by the AudioBackend & SendData
                        //no use keeping the thread alive and doing nothing...
                        if (mp3Mode == false && (error != Error.NONE || connection_init > 0))
                        {
                            break;
                        }
                    }
                    if (error != Error.NONE)
                    {
                        //an error occured while init. bail out!
                        closeServer();
                        break;
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
            
            return;
        }
        private void GeneratePlayList()
        {
            if (mp3Path == null)
                return;

            filesList = new List<string>();
            filesList.AddRange(Directory.GetFiles(mp3Path, "*.mp3", System.IO.SearchOption.AllDirectories));

            Debug.WriteLine("{0} Files found.", filesList.Count);
        }
        private void OpenMp3File()
        {
            OpenMp3File(true);
        }
        private int OpenMp3File(bool random)
        {
            if (filesList == null || filesList.Count <= 0)
                GeneratePlayList();


            int index = -1;
            if (filesList.Count > 0)
            {
                if (random == true)
                {
                    Random rand = new Random();
                    index = rand.Next(0, filesList.Count);
                }
                else
                {
                    index = 0;
                }
                Debug.WriteLine("opening {0}...", filesList[index]);


                audioPlayer.OpenMp3File(filesList[index]);
            }
            return index;
        }
        //send the Audio, in the eventarg's buffer , to the client.
        //TODO : add compression and play with the ACK when getting device output
        private void SendData(object sender, NAudio.Wave.WaveInEventArgs e)
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
