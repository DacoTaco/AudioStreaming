﻿using System;
using System.Xml;

namespace AudioStreaming.Utils
{
    public abstract class BaseSettings<SettingType> where SettingType : class, new()
    {
        protected static string filename = "settings.xml";
        protected static readonly object padlock = new object();
        protected static bool loadingSettings = false;
        protected static SettingType settings;
        public static SettingType Settings
        {
            get
            {
                if (settings == null)
                {
                    lock (padlock)
                    {
                        //place all init of the settings here
                        settings = new SettingType();
                        LoadSettings();
                    }
                }
                return settings;
            }
            protected set
            {
                if (value != null)
                {
                    lock (padlock)
                    {
                        settings = value;
                    }
                }
            }
        }

        protected readonly string version = "0.0.1";
        public string Version
        {
            get
            {
                return version;
            }
            set
            {
                return;
            }
        }

        //-------------------------
        //Functions
        //-------------------------
        protected BaseSettings()
        {
        }

        static protected void LoadSettings()
        {
            Type T = Settings.GetType();
            System.Xml.Serialization.XmlSerializer serializer = new
            System.Xml.Serialization.XmlSerializer(T);

            System.IO.FileStream fs = null;


            // A FileStream is needed to read the XML document.
            try
            {
                fs = new System.IO.FileStream(filename, System.IO.FileMode.Open);
            }
            catch (System.IO.FileNotFoundException fex)
            {
                //file not found. create file by saving current(probably defaults) and then load it
                SaveSettings();
                fs = new System.IO.FileStream(filename, System.IO.FileMode.Open);
            }
            catch (Exception e)
            {
                return;
            }
            if (fs != null)
            {
                try
                {
                    XmlReader reader = XmlReader.Create(fs);

                    // Use the Deserialize method to restore the object's state.
                    lock (padlock)
                    {
                        loadingSettings = true;
                        try
                        {
                            var loadedSettings = serializer.Deserialize(reader);
                            Settings = Cast(loadedSettings, T);
                        }
                        catch (Exception e)
                        {
                            //error loading settings. we'll have to remake the file with the default settings :)
                            System.IO.File.Delete(filename);
                            SaveSettings();
                        }
                    }

                    fs.Close();
                }
                catch (Exception e)
                {
                    fs.Close();
                }
            }
            loadingSettings = false;
            return;
        }
        private static dynamic Cast(dynamic obj, Type castTo)
        {
            return Convert.ChangeType(obj, castTo);
        }
        static protected void SaveSettings()
        {
            if (settings == null)
                settings = new SettingType();

            if (loadingSettings)
                return;

            System.Xml.Serialization.XmlSerializer writer = null;
            try
            {
                writer = new System.Xml.Serialization.XmlSerializer(Settings.GetType());
            }
            catch (Exception e)
            {
                return;
            }
            System.IO.FileStream file = System.IO.File.Create(filename);
            writer.Serialize(file, settings);
            file.Close();
        }
    }


}
