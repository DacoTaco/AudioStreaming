using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace AudioStreaming
{
    static public class Protocol
    {
        //the protocol names/value's
        //-------------------------------------
        public const byte CommandHeaderSize = 6;




        //Command & Subcommands
        //--------------------------
        //init. REQ is for the client, response is the data the server gives to client.
        public const byte INIT_REQ = 0x10;
        public const byte INIT_REQ_RESPONSE = 0x11;
        //ACK is for the client saying " yup, thanks, its cool"
        public const byte INIT_ACK = 0x12;

        //sending data to the other
        public const byte RECQ_SEND_DATA = 0x20;
        public const byte SEND_DATA = 0x21;
        public const byte SEND_DATA_ACK = 0x22;

        public const byte RECQ_SEND_MULTI_DATA = 0x23;
        public const byte SEND_MULTI_DATA = 0x24;
        public const byte SEND_MULTI_ACK = 0x25;
        public const byte SEND_MULTI_EOF_SUBCOM = 0x2a;

        //reinit commands. server telling client to reinit or client asking server for the first frame(so it can setup)
        public const byte RECQ_REINIT = 0x30;
        public const byte RECQ_REINIT_MP3 = 0x31;
        public const byte REINIT_BACKEND = 0x32;
        public const byte REINIT_DONE = 0x33;

        public const byte RECQ_TITLE = 0x40;
        public const byte NEW_TITLE = 0x41;

        public const byte RECQ_NEXT_SONG = 0x50;
        public const byte RECQ_PREV_SONG = 0x51;

        public const byte NOP = 0x98;
        public const byte KILL_CONNECTION = 0x99;

    }
    static public class Error
    {
        //the protocol error codes
        //-------------------------------------

        //no error
        public const sbyte NONE = 0;

        //general networking fail
        public const sbyte GEN_NET_FAIL = -10;

        //unexpected exception
        public const sbyte EXCEP_FAIL = -11;

        //unexpected response
        public const sbyte RESPONSE_FAIL = -12;

        //INIT packet fail
        public const sbyte INIT_FAIL = -13;

        public const sbyte MP3_READ_ERROR = -31;

    }
    static public class Networking
    {

        //receive data
        static private byte HeaderSize = Protocol.CommandHeaderSize;

        static public int GetData(ref byte[] buffer, Socket socket)
        {
            if (socket == null)
                return -1;

            int ret = 0;

            //first check the connection
            if (CheckConnection(socket))
            {
                //first get the packet size. which is the byte 2 to 5 in our protocol.
                //we could use peek, but it was discouraged online :/

                //init temp variable and set it to 0.
                byte[] bpacket = { 0 };
                Array.Resize<byte>(ref bpacket, HeaderSize);
                Array.Clear(bpacket, 0, HeaderSize);

                int size = 0;
                try
                {
                    if (socket.ReceiveTimeout == 0)
                        socket.ReceiveTimeout = 400;
                    ret = socket.Receive(bpacket, HeaderSize, 0);
                }
                catch (SocketException Se)
                {
                    if (Se.SocketErrorCode != SocketError.TimedOut)
                        return -4;
                }
                catch
                {
                    //failure. probably connection issue.
                    return -4;
                }
                if (ret <= 0)
                {
                    if (ret == 0)
                        ret = 0;
                    else
                        ret = -3;

                    return ret;
                }

                //reconstruct size from the packet
                size = (bpacket[2] << 24) + (bpacket[3] << 16) + (bpacket[4] << 8) + bpacket[5];
                
                if (size == 0)
                {
                    ret = 0;
                }
                else
                {
                    try
                    {
                        //we got the size. init/resize the array and shove the data in there
                        if (buffer == null)
                        {
                            buffer = new byte[size];
                        }
                        else
                            Array.Resize<byte>(ref buffer, size);

                        Array.Copy(bpacket, buffer, HeaderSize);

                        if (size > HeaderSize)
                        {
                            ret = HeaderSize;
                            //as long as the size isn't right, read the shit!
                            while (ret < size)
                            {
                                ret += socket.Receive(buffer, ret, size - ret, 0);
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        Debug.WriteLine("GetData : DATA FAILURE : {0} : {1}{2}{2}stacktrace : {3}", ex.ErrorCode, ex.Message, Environment.NewLine, ex.StackTrace);
                        return -3;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetData : DATA FAILURE : {0}{1}{1}stacktrace : {2}{1}{3}", ex.Message, Environment.NewLine, ex.StackTrace, ex.ToString());
                        return -4;
                    }
                }
            }
            else
            {
                ret = -2;
            }

            return ret;
        }

        //send dataaaa!
        //basically this composes the packet the protocol wants
        //byte 0 : command
        //byte 1 : subcommand
        //byte 1 - 4 : size
        //byte 5 - infinity : data
        static public int SendData(byte Command, byte[] buffer, Socket socket)
        {
            return SendData(Command, 0, buffer, socket);
        }
        static public int SendData(byte Command,byte subCommand,byte[] buffer, Socket socket)
        {
            if (socket == null)
                throw new ArgumentNullException("SendDataCommand : argument is null");

            int size = 0;

            if(buffer != null)
                size = buffer.Length;

            byte[] data = new byte[size + Protocol.CommandHeaderSize];

            data[0] = Command;
            data[1] = subCommand;
            if (size > 0)
            {
                data[2] = AudioStreaming.ByteConversion.ByteFromInt(size + Protocol.CommandHeaderSize, 0);
                data[3] = AudioStreaming.ByteConversion.ByteFromInt(size + Protocol.CommandHeaderSize, 1);
                data[4] = AudioStreaming.ByteConversion.ByteFromInt(size + Protocol.CommandHeaderSize, 2);
                data[5] = AudioStreaming.ByteConversion.ByteFromInt(size + Protocol.CommandHeaderSize, 3);
                Array.Copy(buffer, 0, data, HeaderSize, size);
            }
            else
            {
                data[2] = 0;
                data[3] = 0;
                data[4] = 0;
                data[5] = AudioStreaming.ByteConversion.ByteFromInt(HeaderSize, 3);
            }

            //made this if to let it throw when it sends data it shouldn't. so far, no exception... :/
            if (data[0] != Command)
                throw new Exception("memory leak detected in ComposeSendData!");

            //send the actual data
            int ret = SendData(data, socket);

            //correct the returned size
            if (ret >= HeaderSize)
                ret -= HeaderSize;
            return ret;
        }

        //send Data
        static public int SendData(byte[] buffer, Socket socket)
        {
            if (buffer == null || socket == null)
                throw new ArgumentNullException("SendData : argument is null");//return 0;

            int size = buffer.Length;
            int ret = 0;

            //first check the connection
            if (CheckConnection(socket))
            {
                try
                {
                    //send the dataaaaaaa
                    if (size == 0)
                    {
                        ret = socket.Send(buffer);
                    }
                    else
                        ret = socket.Send(buffer, size, 0);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("SendData : DATA FAILURE : {0}{1}{1}stacktrace : {2}{1}", ex.Message, Environment.NewLine, ex.StackTrace);
                    return -6;
                }
            }
            else
                ret = -2;
            return ret;
        }


        //check connection
        static public bool CheckConnection(Socket socket)
        {
            if (socket == null)
                return false;

            try
            {
                //if the socket says connected we test it to check if its true or not
                //if it says not connected then its clearly not connected
                if (socket.Connected)
                {
                    //poll the connection. the poll will return true if :
                    // - a connection is pending
                    // - data is available for reading
                    // - connection is closed or unavailable
                    //
                    // so if it returns false, the connection is there but nothing can be read
                    // if its true, we need to check if we can read data. if we can't, its either pending (useless) or closed (even more useless)
                    // so we mark it as closed.
                    bool bpoll = false;
                    bpoll = socket.Poll(0, SelectMode.SelectRead);
                    if (bpoll)
                    {
                        byte[] buff = new byte[1];
                        int recv_value = socket.Receive(buff, SocketFlags.Peek);
                        if (recv_value <= 0)
                        {
                            // Client disconnected
                            return false;
                        }
                    }
                }
                else
                    return false;

            }
            catch
            {
                //something went wrong. im guessing connection is closed but meh. lets just assume connection is a no go
                return false;
            }
            return true;
        }

        //connect to the given IP or hostname and return if it succeeded or not
        static public bool Connect(Socket socket, IPAddress ipaddress , int port)
        {
            if (socket == null || port <= 0 || port > 65535)
                return false;

            try
            {
                socket.Connect(ipaddress, port);
                return socket.Connected;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        static public bool Connect(ref Socket socket,string hostname,int port)
        {
            if (socket == null || hostname.Length == 0 || port <= 0 || port > 65535)
                return false;

            try
            {
                IPHostEntry hostEntry;

                //get the IP from the hostname
                hostEntry = Dns.GetHostEntry(hostname);

                //go trough the IP's. we use IPv4 so if we have an IPv4 ip we use it to attempt connection
                if (hostEntry.AddressList.Length > 0)
                {
                    for (int i = 0; i < hostEntry.AddressList.Length; i++)
                    {
                        if (hostEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                        {
                            return Connect(socket, hostEntry.AddressList[i], port);
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
            return false;
        }
        static public bool SetupListener(ref TcpListener socket,int port)
        {
            if (port <= 0 || port >= 65535)
                port = 8666;

            //get the local IP so we can setup the listener
            //apparently, if we bind to 0.0.0.0 we can accept connections from any source, local (127.0.0.1) or external!
            IPAddress ipAddress = null;
            /*var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddress = ip;
                }
            }*/
            ipAddress = IPAddress.Parse("0.0.0.0");

            //setup the listener, socket and variables
            socket = new TcpListener(ipAddress, port);
            if (socket != null)
                return true;
            else
                return false;
        }

        static public int DataAvailable(Socket clientSocket)
        {
            if (clientSocket == null || !CheckConnection(clientSocket) )
                return Error.GEN_NET_FAIL;
            if (clientSocket.Available >= 10)
            {
                Debug.WriteLine(String.Format("Data Available for reading : {0}",clientSocket.Available));
            }
            return clientSocket.Available; // return 0;
        }
    }
}
