using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Data;
using System.Threading;
using System.Windows;

namespace AudioStreaming.Client
{
    //variables for data binding and general functions
    public partial class audioClient
    {
        //---------------------------
        //       VARIABLES
        //---------------------------
        private bool eventRegistered = false;

        public Settings settings = new Settings();

        //the audioPlayer using our AudioBackend. this will handle the data and play it
        private AudioPlayer audioPlayer = null;

        public string Hostname
        {
            get
            {
                return settings.Hostname;
            }
            set
            {
            }
        }
        public float Volume
        {
            get
            {
                return audioPlayer.Volume;
            }
            set
            {
                audioPlayer.Volume = value;   
            }
        }
      
        public double BufferLenght
        {
            get
            {
               return (byte)audioPlayer.BufferLenght;
            }
            set
            {
            }
        }

        private string songName;

        public string SongName
        {
            get 
            {
                if (songName == null || songName == "")
                    return "Unknown";
                return songName; 
            }
            set 
            {
                if (value != songName || value != null)
                {
                    songName = value;
                    OnPropertyChanged("SongName");
                }
            }
        }
        

        //---------------------------
        //       FUCNTIONS
        //---------------------------
        public audioClient()
        {
            audioPlayer = new AudioPlayer();
            audioPlayer.backendHandler += AudioPlayer_backendHandler;
            settings.LoadSettings();
            return;
        }

        void AudioPlayer_backendHandler(object sender, bool State)
        {
            if (State == true)
                RegisterPropertyChanged();
            else
                UnregisterPropertyChanged();
        }

        public void StartConnection(string hostname, bool compressData, bool _mp3Mode)
        {
            //we dont want to have the client run twice
            if (ThreadAlive)
                return;

            //Hostname = hostname;
            compressed = compressData;
            mp3Mode = _mp3Mode;
            Thread oThread = new Thread(new ThreadStart(this.ConnectToServer));
            ThreadAlive = false;
            killThread = false;
            oThread.Start();
            return;
        }

        public void SaveSettings()
        {
            settings.SaveSettings();
        }
        public void LoadSettings()
        {
            settings.LoadSettings();
        }

        //add the received data to the AudioBackend's buffer
        private void AddDataToBuffer(ref byte[] data)
        {
            if (data == null || data.Length <= 0)
                return;

            audioPlayer.AddSamples(ref data);
            
            return;
        }

        private void RegisterPropertyChanged()
        {
            if (eventRegistered)
                return;
            eventRegistered = true;
            audioPlayer.PropertyChanged += HandlePropertyChanged;
        }
        private void UnregisterPropertyChanged()
        {
            if (!eventRegistered)
                return;
            eventRegistered = false;
            audioPlayer.PropertyChanged -= HandlePropertyChanged;
        }
        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender.ToString() == "AudioStreaming.AudioPlayer" && e.PropertyName == "BufferLenght")
                OnPropertyChanged("BufferLenght");
            if (sender.ToString() == "AudioStreaming.AudioPlayer" && e.PropertyName == "Volume")
                OnPropertyChanged("Volume");
        }
    }
}
