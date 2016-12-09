using System;

namespace AudioStreaming.Utils
{
    public sealed class ClientSettings : BaseSettings<ClientSettings>
    {
        //-------------------------
        //Functions
        //-------------------------

        public ClientSettings()
        {
        }

        //-------------------------
        //Actual Settings
        //-------------------------
        private string hostname = "127.0.0.1";
        public string Hostname
        {
            get
            {
                return hostname;
            }
            set
            {
                if (value != hostname)
                {
                    hostname = value;
                    SaveSettings();
                }
            }
        }

        private Boolean mp3Mode = true;
        public Boolean Mp3Mode
        {
            get
            {
                return mp3Mode;
            }
            set
            {
                mp3Mode = value ? true : false;
                SaveSettings();
            }
        }

        private bool compressData = true;
        public bool CompressData
        {
            get
            {
                return compressData;
            }
            set
            {
                compressData = value ? true : false;
                SaveSettings();
            }
        }


    }
}
