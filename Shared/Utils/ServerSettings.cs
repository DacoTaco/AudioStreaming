using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
