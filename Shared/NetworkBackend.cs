using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace AudioStreaming
{
    //we inherit from the INotifyPropertyChanged so we can give back feedback when bound variables are updated
    public class NetworkBackend : NotifyPropertyChange
    {
        protected Socket clientSocket = null;
        protected byte connection_init = 0;
        protected bool mp3Mode = false;
        protected bool compressed = false;
        protected sbyte error = Error.NONE;
        private bool threadAlive = false;

        /// <summary>
        /// Get or Set wether the thread is active
        /// </summary>
        public bool ThreadAlive 
        {
            get { return threadAlive; }
            set
            {
                if (value != threadAlive)
                {
                    //something wants to kill the thread. better set killThread as true so functions quit nicely
                    if (threadAlive == true && value == false)
                        killThread = true;

                    threadAlive = value;
                    //report that the value has changed
                    OnPropertyChanged("ThreadAlive");
                    OnPropertyChanged("enableControls");
                }
            }
        }

        /// <summary>
        /// Set the thread to be killed
        /// </summary>
        protected bool killThread = false;

        /// <summary>
        /// returns wether we should disable controls or not
        /// </summary>
        public bool enableControls
        {
            get
            {
                return !ThreadAlive;
            }
        }
        protected byte connected = 0;

        /// <summary>
        /// Check if we are running threaded, and if so, kill it using the variable
        /// </summary>
        public void KillThread()
        {
            if (ThreadAlive == true)
            {
                killThread = true; //threadAlive = false;
                while (ThreadAlive)
                    System.Threading.Thread.Sleep(100);
            }
            return;
        }




        //basically stubs for the networking backend
        protected int DataAvailable()
        {
            try
            {
                return Networking.DataAvailable(clientSocket);
            }
            catch (SocketException ex)
            {
                Debug.WriteLine("DataAvailable FAILURE : {0} : {1}{2}{2}stacktrace : {3}", ex.ErrorCode, ex.Message, Environment.NewLine, ex.StackTrace);
                return Error.EXCEP_FAIL;
            }
        }
        protected int GetData(ref byte[] buffer)
        {
            try
            {
                return Networking.GetData(ref buffer, clientSocket);
            }
            catch (SocketException ex)
            {
                Debug.WriteLine("RECEIVING DATA FAILURE : {0} : {1}{2}{2}stacktrace : {3}", ex.ErrorCode, ex.Message, Environment.NewLine, ex.StackTrace);
                return Error.EXCEP_FAIL;
            }
        }
        protected int SendData(byte command, byte[] buffer)
        {
            return Networking.SendData(command, buffer, clientSocket);
        }
        protected int SendData(byte[] buffer)
        {
            return Networking.SendData(buffer, clientSocket);
        }
        protected bool CheckConnection()
        {
            return Networking.CheckConnection(clientSocket);
        }
        protected bool Connect(string hostname, int port)
        {
            return Networking.Connect(ref clientSocket, hostname, port);
        }
        protected void CleanupNetworking()
        {
            if (clientSocket != null)
            {
                if (clientSocket.Connected)
                {
                    try
                    {
                        clientSocket.Disconnect(false);
                        clientSocket.Shutdown(SocketShutdown.Both);
                        clientSocket.Close();
                    }
                    catch (SocketException e)
                    {
                        Debug.WriteLine("Exception when closing the connection. error {0}\n", e.Message);
                        //do nothing
                    }
                }
                clientSocket.Dispose();
            }
            connection_init = 0;
            connected = 0;
        }
    }
}
