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
        //variables
        //----------------------
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
            return;
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
