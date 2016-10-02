using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AudioStreaming.Server
{
    public class ServerSettings
    {
        public string directory = System.IO.Directory.GetCurrentDirectory();
        private readonly string serverVersion = "0.0.1";
        public string ServerVersion
        {
            get
            {
                return serverVersion;
            }
            set
            {
                return;
            }
        }
    }
    //it would be great to have these functions IN settings. however, a class can't overload itself. didn't try inheritence & having parent as variable
    public class Settings
    {
        private ServerSettings settings = new ServerSettings();

        public string Directory 
        {
            get
            {
                return settings.directory;
            }
            set
            {
                if (settings.directory != value)
                {
                    settings.directory = value;
                    SaveSettings();
                }
            }
        }
        public string ServerVersion
        {
            get { return settings.ServerVersion; }
        }

        public void LoadSettings()
        {

            System.Xml.Serialization.XmlSerializer serializer = new
            System.Xml.Serialization.XmlSerializer(typeof(ServerSettings));

            System.IO.FileStream fs = null;
            settings = new ServerSettings();

            // A FileStream is needed to read the XML document.
            try
            {
                fs = new System.IO.FileStream("settings.xml", System.IO.FileMode.Open);
            }
            catch (System.IO.FileNotFoundException fex)
            {
                //file not found. create file by saving current(probably defaults) and then load it
                SaveSettings();
                fs = new System.IO.FileStream("settings.xml", System.IO.FileMode.Open);
            }
            catch (Exception e)
            {
                return;
            }
            if (fs != null)
            {

                XmlReader reader = XmlReader.Create(fs);

                // Use the Deserialize method to restore the object's state.
                //test = (Settings)serializer.Deserialize(reader);
                settings = (ServerSettings)serializer.Deserialize(reader);

                fs.Close();
            }
            return;
        }
        public void SaveSettings()
        {
            if (settings == null)
                settings = new ServerSettings();

            System.Xml.Serialization.XmlSerializer writer = null;
            try
            {
                writer = new System.Xml.Serialization.XmlSerializer(typeof(ServerSettings));
            }
            catch (Exception e)
            {
                return;
            }
            System.IO.FileStream file = System.IO.File.Create("settings.xml");
            writer.Serialize(file, settings);
            file.Close();
        }
    }
}
