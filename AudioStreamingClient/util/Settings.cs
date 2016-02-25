using System;
using System.Xml;

namespace AudioStreaming.Client
{
    public class ClientSettings
    {
        public string hostname = "127.0.0.1";
        public bool mp3Mode = true;
        public bool compressData = true;
        private readonly string clientVersion = "0.0.1";
        public string ClientVersion
        {
            get
            {
                return clientVersion;
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
        private ClientSettings settings = new ClientSettings();
        public string ClientVersion
        {
            get
            {
                return settings.ClientVersion;
            }
        }

        public string Hostname
        {
            get
            {
                return settings.hostname;
            }
            set
            {
                if (value != settings.hostname)
                {
                    settings.hostname = value;
                    SaveSettings();
                }
            }
        }
        public bool Mp3Mode
        {
            get
            {
                return settings.mp3Mode;
            }
            set
            {
                settings.mp3Mode = value ? true : false;
                SaveSettings();
            }
        }
        public bool CompressData
        {
            get
            {
                return settings.compressData;
            }
            set
            {
                settings.compressData = value ? true : false;
                SaveSettings();
            }
        }

        public void LoadSettings()
        {

            System.Xml.Serialization.XmlSerializer serializer = new
            System.Xml.Serialization.XmlSerializer(typeof(ClientSettings));

            System.IO.FileStream fs = null;
            settings = new ClientSettings();

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
                settings = (ClientSettings)serializer.Deserialize(reader);

                fs.Close();
            }
            return;
        }
        public void SaveSettings()
        {
            if (settings == null)
                settings = new ClientSettings();

            System.Xml.Serialization.XmlSerializer writer = null;
            try
            {
                writer = new System.Xml.Serialization.XmlSerializer(typeof(ClientSettings));
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
