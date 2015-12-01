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

        //init. REQ is for the client, response is the data the server gives to client.
        public const byte INIT_REQ = 0x10;
        public const byte INIT_REQ_RESPONSE = 0x11;
        //ACK is for the client saying " yup, thanks, its cool"
        public const byte INIT_ACK = 0x12;

        //sending data to the other
        public const byte SEND_DATA = 0x20;
        public const byte SEND_DATA_ACK = 0x21;

        //asking client to reinit the backend because format changed
        public const byte REINIT_BACKEND = 0x30;
        public const byte REINIT_DONE = 0x31;

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
                byte[] bpacket = {0,0,0,0,0};
                int size = 0;
                try
                {
                    ret = socket.Receive(bpacket, 5, 0);
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
                size = (bpacket[1] << 24) + (bpacket[2] << 16) + (bpacket[3] << 8) + bpacket[4];
                
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

                        Array.Copy(bpacket, buffer, 5);

                        if (size > 5)
                        {
                            ret = 5;
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
        //byte 1 - 4 : size
        //byte 5 - infinity : data
        static public int SendData(byte Command,byte[] buffer, Socket socket)
        {
            if (socket == null)
                throw new ArgumentNullException("SendDataCommand : argument is null");

            int size = 0;

            if(buffer != null)
                size = buffer.Length;

            byte[] data = new byte[size + 5];

            data[0] = Command;
            if (size > 0)
            {
                data[1] = AudioStreaming.ByteConversion.ByteFromInt(size+5, 0);
                data[2] = AudioStreaming.ByteConversion.ByteFromInt(size+5, 1);
                data[3] = AudioStreaming.ByteConversion.ByteFromInt(size+5, 2);
                data[4] = AudioStreaming.ByteConversion.ByteFromInt(size+5, 3);
                Array.Copy(buffer, 0, data, 5, size);
            }
            else
            {
                data[1] = 0;
                data[2] = 0;
                data[3] = 0;
                data[4] = AudioStreaming.ByteConversion.ByteFromInt(5, 3);
            }

            //made this if to let it throw when it sends data it shouldn't. so far, no exception... :/
            if (data[0] != Command)
                throw new Exception("memory leak detected in ComposeSendData!");

            //send the actual data
            int ret = SendData(data, socket);

            //correct the returned size
            if (ret >= 5)
                ret -= 5;
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
    }
}
