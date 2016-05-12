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
                if (value != songName)
                {
                    songName = value;
                }
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
                    SongName = String.Format("{0}",filesList[index]);
                else
                    SongName = String.Format("{0} - {1}", tag.Tag.JoinedPerformers, tag.Tag.Title);

                Debug.WriteLine(String.Format("playing : {0}",SongName));

                tag.Dispose();

                audioPlayer.OpenMp3File(filesList[index]);                
            }
            return index;
        }

        private bool OpenNextFile()
        {
            NAudio.Wave.Mp3Frame frame = null;
            byte[] header = new byte[5];
            byte[] data = new byte[1];
            byte command = Protocol.SEND_MULTI_DATA;

            OpenMp3File();
            frame = audioPlayer.GetNextMp3Frame();
            if (frame == null)
            {
                error = Error.MP3_READ_ERROR;
                return false;
            }
            //we have a new file, lets send the new title first :)
            SendNewTitle();

            //compare the frame with the waveform from the last file.
            if (!audioPlayer.IsWaveformatEqual(frame))
            {
                //the frame is in a different format. we need to let the client know!
                header[0] = 1;
                header[1] = 0; //index of the next frame
                header[2] = 0x05;
                header[3] = ByteConversion.ByteFromInt(frame.RawData.Length, 2); //size
                header[4] = ByteConversion.ByteFromInt(frame.RawData.Length, 3);
                data = frame.RawData;
                command = Protocol.REINIT_BACKEND;
            }

            byte[] _tempData = new byte[1];
            Array.Resize(ref _tempData, data.Length + header.Length);
            Array.Copy(header, _tempData, header.Length);
            Array.Copy(data, 0, _tempData, header.Length, data.Length);

            data = _tempData;

            //compress that shit!
            if (compressed)
                data = Compressor.Compress(data);

            int ret = SendData(command, data);

            if (ret < 0)
                return false;
            else
                return true;
        }

    }
}
