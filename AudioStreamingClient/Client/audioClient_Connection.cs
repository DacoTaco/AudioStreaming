using System;
using System.Net.Sockets;
using System.Diagnostics;

namespace AudioStreaming.Client
{
    //the part of the client that handles connections ^-^;

    public partial class audioClient : NetworkBackend
    {
        //---------------------------
        //       VARIABLES
        //---------------------------
        private byte commandToSend = 0x00;
        private byte recquestCommandToSend = 0x00;
        private byte subCommandToSend = 0x00;
        private byte recquestSubCommandToSend = 0x00;
        private double MAX_BUFFER_LENGHT = 2.5;

        //---------------------------
        //       FUCNTIONS
        //---------------------------
        private void NextCommandToSend(byte command, byte subcommand)
        {
            recquestCommandToSend = command;
            recquestSubCommandToSend = subcommand;
            return;
        }

        //connect to the server
        private void ConnectToServer()
        {
            if (Hostname == null || Hostname.Length <= 0)
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
            this.Connect(Hostname, 8666);

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
                        size = SendData(Protocol.INIT_REQ, msg);
                        if (size < msg.Length)
                        {
                            //failed to send the init data asking for the info
                            error = Error.GEN_NET_FAIL;
                            break;
                        }

                        //get the response & validate it
                        DateTime startTime = DateTime.Now;
                        TimeSpan elapsedTime = DateTime.Now - startTime;
                        int expected_size = 0x0f;

                        while (size < expected_size && elapsedTime.Seconds < 10)
                        {
                            size = GetData(ref buffer);
                            elapsedTime = DateTime.Now - startTime;
                            System.Threading.Thread.Sleep(1);
                        }
                        

                        if (size < expected_size || buffer[0] != Protocol.INIT_REQ_RESPONSE
                            || ByteConversion.ByteArrayToUInt(buffer, Protocol.CommandHeaderSize) == 0xDEADFFFF || (buffer[8] << 8) + buffer[9] != audioPlayer._VERSION)
                        {
                            Debug.WriteLine("size : {0}{1}buffer[0] : {2}{1}Response:{3}{1}Version : {4}",
                                size,
                                Environment.NewLine,
                                (buffer == null) ? "buffer == null" : Convert.ToString(buffer[0]),
                                (buffer == null) ? "buffer == null" :
                                    String.Format("{0}", ByteConversion.ByteArrayToUInt(buffer, Protocol.CommandHeaderSize)),
                                (buffer == null) ? "buffer == null" : String.Format("{0}", (buffer[8] << 8) + buffer[9]));
                            error = Error.RESPONSE_FAIL;
                            break;
                        }

                        //recompile the information from the packet and use it
                        int samplerate = (buffer[10] << 8) + buffer[11];
                        int channels = buffer[12];
                        compressed = (buffer[buffer.Length - 2] != 0) ? true : false;
                        mp3Mode = (buffer[buffer.Length - 1] != 0) ? true : false;

                        //send that we were able to init.
                        size = SendData(Protocol.INIT_ACK, null);
                        if (size < 0)
                            error = Error.GEN_NET_FAIL;

                        //connection is init!

                        //if we aren't running in mp3mode then lets start the audiobackend already
                        //in mp3Mode we will wait for the first frame
                        if (mp3Mode == false)
                        {
                            audioPlayer.SetWaveFormat(samplerate, channels);
                            audioPlayer.StartPlaying();
                            recquestCommandToSend = Protocol.RECQ_TITLE;
                        }
                        connection_init = 1;
                        Debug.WriteLine("Connection Init!");
                    }
                    else
                    {
                        //if a certain command is requested , we will send it first. fuck the rest
                        if (recquestCommandToSend != 0x00)
                        {
                            commandToSend = recquestCommandToSend;
                            subCommandToSend = recquestSubCommandToSend;
                            //reset the requested variable
                            recquestCommandToSend = recquestSubCommandToSend = 0x00;
                        }

                        //if we aren't playing anything and there is no command to send, we request a new title
                        //which will start the process of playing new song
                        else if (!audioPlayer.IsPlaying() && commandToSend == 0x00)
                        {
                            commandToSend = Protocol.RECQ_TITLE;
                        }
                        //if there is enough data atm, we dont need more
                        else if (audioPlayer.GetBufferLenght() >= MAX_BUFFER_LENGHT)
                        {
                            if (audioPlayer.WaitForMoreData() < MAX_BUFFER_LENGHT)
                            {
                                if (recquestCommandToSend == 0x00)
                                {
                                    if (mp3Mode)
                                        commandToSend = Protocol.RECQ_SEND_MULTI_DATA;
                                    else
                                        commandToSend = Protocol.RECQ_SEND_DATA;
                                }
                                else
                                {
                                    commandToSend = recquestCommandToSend;
                                    subCommandToSend = recquestSubCommandToSend;
                                    //reset the requested variable
                                    recquestCommandToSend = recquestSubCommandToSend = 0x00;
                                }
                            }
                            else
                                commandToSend = 0x00;
                        }
                        else if (mp3Mode && !audioPlayer.bFileEnding)
                        {
                            commandToSend = Protocol.RECQ_SEND_MULTI_DATA;
                        }
                        else if (mp3Mode && audioPlayer.bFileEnding) // EOF was signaled. finish song,send the next command and redo! :P
                        {
                            if(audioPlayer.WaitForMoreData() <= 0)
                            {
                                //stop player, and then add the next frame. this will reinit the player
                                audioPlayer.StopPlaying();
                                audioPlayer.bFileEnding = false;
                                commandToSend = Protocol.RECQ_NEXT_SONG;
                            }
                        }
                        else if (!mp3Mode)
                            commandToSend = Protocol.RECQ_SEND_DATA;
                        


                        if (commandToSend != 0x00)
                        {
                            switch (commandToSend)
                            {
                                case Protocol.RECQ_REINIT:
                                    if (mp3Mode)
                                        break;

                                    SendData(Protocol.RECQ_REINIT, null);
                                    ReceiveSocketData(Protocol.RECQ_REINIT);
                                    break;

                                case Protocol.RECQ_REINIT_MP3:
                                    if (!mp3Mode)
                                        break;
                                    SendData(Protocol.RECQ_REINIT_MP3, null);
                                    ReceiveSocketData(Protocol.RECQ_REINIT_MP3);
                                    break;

                                case Protocol.RECQ_NEXT_SONG:
                                case Protocol.RECQ_PREV_SONG:
                                    if (mp3Mode)
                                    {
                                        audioPlayer.StopPlaying();
                                        goto case Protocol.NOP;
                                    }
                                    else
                                        break;
                                case Protocol.RECQ_SEND_MULTI_DATA: //request data to play!                            
                                case Protocol.RECQ_TITLE: //request the title!
                                case Protocol.RECQ_SEND_DATA: //request data!
                                case Protocol.KILL_CONNECTION:
                                case Protocol.NOP:
                                    SendData(commandToSend, null);
                                    ReceiveSocketData(commandToSend);
                                    break;
                                default:
                                    break;
                            }


                            //switch case dealing with what the client should do next
                            switch (commandToSend)
                            {
                                case Protocol.RECQ_PREV_SONG: //the response we get from these 3 is the same, the new title. meaning we need to reinit y0
                                case Protocol.RECQ_NEXT_SONG:
                                case Protocol.RECQ_TITLE:
                                    if (mp3Mode)
                                        commandToSend = Protocol.RECQ_REINIT_MP3;
                                    else
                                        commandToSend = 0x00;
                                    break;
                                case Protocol.RECQ_REINIT_MP3: //backend is init, time to get mp3 data!
                                    commandToSend = Protocol.RECQ_SEND_MULTI_DATA;
                                    break;
                                case Protocol.RECQ_SEND_MULTI_DATA:
                                default:
                                    if (audioPlayer.GetBufferLenght() < MAX_BUFFER_LENGHT && !audioPlayer.bFileEnding) // if we dont have enough data, we'll request some more :')
                                    {
                                        if (mp3Mode)
                                            commandToSend = Protocol.RECQ_SEND_MULTI_DATA;
                                        else
                                            commandToSend = Protocol.RECQ_SEND_DATA;
                                    }
                                    else
                                        commandToSend = 0x00;
                                    break;
                            }
                        }
                    }
                    if (error != Error.NONE)
                        break;
                }

            }
            else
            {
                //failed to connect lol
                System.Windows.MessageBox.Show("Error connecting to Server : " + Hostname + " !");
                Debug.WriteLine("CLIENT : CONNECTION FAILURE");
            }
            //ERRORZ
            if (error != 0)
                Debug.WriteLine("CLIENT : ERROR {0}", error);

            CloseClient();
            Debug.WriteLine(" >> exit client");
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
            BufferLenght = 0;
            SongName = "Unknown";

        }
        private void ReceiveSocketData(byte Command_send)
        {
            byte[] buffer = null;
            int size = 0;
            byte command = 0;
            byte recv_multi = 0;

            while (size < Protocol.CommandHeaderSize && size >= 0)
            {
                size = GetData(ref buffer);
            }
            if (size > Protocol.CommandHeaderSize)
                size -= Protocol.CommandHeaderSize;

            if (size == Protocol.CommandHeaderSize || size < 0)
                return;

            byte[] data = new byte[size];
            Array.Copy(buffer, Protocol.CommandHeaderSize, data, 0, size);
            command = buffer[0];
            byte subCommand = buffer[1];

            if (compressed)
                data = Compressor.Decompress(data);


            /*string hex = "0x" + BitConverter.ToString(data);
            hex = hex.Replace("-", " 0x");
            Debug.WriteLine(" >> Data from server - {0}", hex);*/


            switch (command)
            {
                case Protocol.NEW_TITLE:
                    SongName = System.Text.Encoding.UTF8.GetString(data);
                    Debug.WriteLine(String.Format("new title : {0}", SongName));
                    break;
                case Protocol.REINIT_BACKEND:
                    audioPlayer.StopPlaying();
                    recv_multi = data[0];
                    goto case Protocol.SEND_DATA;
                case Protocol.SEND_MULTI_DATA:
                    if(subCommand == Protocol.SEND_MULTI_EOF_SUBCOM)
                        audioPlayer.bFileEnding = true;
                    recv_multi = data[0];
                    goto case Protocol.SEND_DATA;
                case Protocol.SEND_DATA:
                    try
                    {
                        if (!mp3Mode)
                            recv_multi = 0;
                        int i = 1;
                        do
                        {
                            byte[] frame = new byte[1];
                            int frameSize = 0;
                            int index = 0;

                            if (recv_multi <= 0)
                            {
                                frame = data;
                            }
                            else
                            {
                                //retrieve index of current frame
                                byte[] _temp = new byte[2];

                                //the index is in the 1st & 2nd bytes of the frame's header of the packet
                                Array.Copy(data, (4 * i) - 3, _temp, 0, 2);
                                index = ByteConversion.ByteArrayToInt(_temp, 0);

                                //retrieve size of current frame, which is in the 3th & 4th bytes of the frame's header of the packet
                                Array.Copy(data, (4 * i) - 1, _temp, 0, 2);
                                frameSize = ByteConversion.ByteArrayToInt(_temp, 0);//data.Length - index;


                                Array.Resize(ref frame, frameSize);
                                Array.Copy(data, index, frame, 0, frameSize);
                                //continue;
                            }

                            //and add data...
                            if (mp3Mode)
                            {
                                audioPlayer.AddNextFrame(frame);
                            }
                            else
                            {
                                AddDataToBuffer(ref frame);
                            }

                            i++;
                        } while (i <= recv_multi);
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    recv_multi = 0;
                    break;

                case Protocol.NOP:
                default:
                    break;
            }
        }
    }
}
