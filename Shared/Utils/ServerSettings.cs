using System;

namespace AudioStreaming.Utils
{
    public class ServerSettings : BaseSettings<ServerSettings>
    {
        //-------------------------
        //Functions
        //-------------------------

        public ServerSettings()
        {
        }

        //-------------------------
        //Actual Settings
        //-------------------------
        private string directory = System.IO.Directory.GetCurrentDirectory();
        public string Directory
        {
            get
            {
                return directory;
            }
            set
            {
                if (directory != value)
                {
                    directory = value;
                    SaveSettings();
                }
            }
        }

    }
}
