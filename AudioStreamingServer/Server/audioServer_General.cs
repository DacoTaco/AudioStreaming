using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using TagLib;

namespace AudioStreaming
{
    public partial class audioServer : NetworkBackend
    {
        //----------------------
        //subclasses
        //----------------------
        /// <summary>
        /// class to contain the device information
        /// </summary>
        public class Device
        {
            public string Device_name { get; set; }
            public int Channels { get; set; }
        }

        //----------------------
        //variables
        //----------------------
        public IList<Device> Devices { get; set; }
        private int deviceIndex = 0;

        private string _mp3Path;

        public string mp3Path
        {
            get { return _mp3Path; }
            set 
            {
                if (_mp3Path != value)
                {
                    _mp3Path = value;
                    OnPropertyChanged("mp3Path");
                }
            }
        }

        private string songName;

        public string SongName
        {
            get { return songName; }
            set 
            { 
                if(value != songName)
                    songName = value; 
            }
        }
        
        
        private List<string> filesList = null;

        AudioRecorder audioPlayer = null;

        //----------------------
        //functions
        //----------------------

        public audioServer()
        {
            audioPlayer = new AudioRecorder();
            GetDevices();
            return;
        }

        /// <summary>
        /// Gets all audio devices installed on the device and store them in 'Devices' which is data linked to the GUI
        /// </summary>
        private void GetDevices()
        {
            List<NAudio.Wave.WaveInCapabilities> devices = new List<NAudio.Wave.WaveInCapabilities>();

            //gets the input (wavein) devices and adds them to the list
            for (short i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++)
            {
                devices.Add(NAudio.Wave.WaveIn.GetCapabilities(i));
            }

            //claer the listview module
            Devices = new List<Device>();

            //each device gets inserted into the devices list, which is linked to the listdevices listview module
            foreach (var device in devices)
            {
                ListViewItem item = new ListViewItem();
                item.Content = device.ProductName;
                Devices.Add(new Device() { Device_name = device.ProductName, Channels = device.Channels });
            }
        }

        public int GetDevicesCount()
        {
            return Devices.Count;
        }


        private void GeneratePlayList()
        {
            if (mp3Path == null)
                return;

            filesList = new List<string>();
            filesList.AddRange(Directory.GetFiles(mp3Path, "*.mp3", System.IO.SearchOption.AllDirectories));

            Debug.WriteLine("{0} Files found.", filesList.Count);
        }
        private void OpenMp3File()
        {
            OpenMp3File(true);
        }
        private int OpenMp3File(bool random)
        {
            if (filesList == null || filesList.Count <= 0)
                GeneratePlayList();


            int index = -1;
            if (filesList.Count > 0)
            {
                if (random == true)
                {
                    Random rand = new Random();
                    index = rand.Next(0, filesList.Count);
                }
                else
                {
                    index = 0;
                }

                TagLib.File tag = TagLib.File.Create(filesList[index]);

                if (tag.Tag.JoinedPerformers == "" || tag.Tag.Title == "")
                    SongName = String.Format("playing : {0}",filesList[index]);
                else
                    SongName = String.Format("playing : {0} - {1}", tag.Tag.JoinedPerformers, tag.Tag.Title);

                Debug.WriteLine(SongName);

                tag.Dispose();

                audioPlayer.OpenMp3File(filesList[index]);                
            }
            return index;
        }

    }
}
