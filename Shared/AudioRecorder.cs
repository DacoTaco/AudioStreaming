using System;
using NAudio.Wave;

namespace AudioStreaming
{
    public class AudioRecorder : AudioBackend
    {
        //variables
        //since we are using threads, we need to use WaveInEvent instead of WaveIn.
        //WaveIn Dislikes threads it seems
        private WaveInEvent sourceStream = null;//NAudio.Wave.WaveIn sourceStream = null;
        NAudio.Wave.Mp3FileReader mp3Reader = null;



        //functions
        protected override bool IsBackendValid()
        {
            if (out_buffer == null || (mp3Reader == null && sourceStream == null))
                return false;
            return true;
        }

        public void StopRecording()
        {
            if (sourceStream != null)
            {
                sourceStream.StopRecording();
                //sourceStream.Dispose();
                sourceStream = null;
            }

            if (mp3Reader != null)
            {
                mp3Reader.Close();
                mp3Reader.Dispose();
                mp3Reader = null;
            }
            KillAll();
        }
        public void StopRecording(object sender, EventArgs e)
        {
            StopRecording();
        }

        //Set's up the stream for recording/streaming. it makes it so when data is available in the stream it calls function dataAvailable with said data
        public void StartRecording(int index, EventHandler<NAudio.Wave.WaveInEventArgs> dataAvailable)
        {
            if (dataAvailable == null)
                return;

            //setup the input stream. we get the device number from the selected index, setup the format for reading
            sourceStream = new NAudio.Wave.WaveInEvent();//NAudio.Wave.WaveIn();
            sourceStream.DeviceNumber = index;
            sourceStream.WaveFormat = new NAudio.Wave.WaveFormat(44100, NAudio.Wave.WaveIn.GetCapabilities(index).Channels);
            waveFormat = sourceStream.WaveFormat;

            //setup the callbacks when there is data or the recording stopped(suddenly disconnection = no recording = the function)
            sourceStream.DataAvailable += new EventHandler<NAudio.Wave.WaveInEventArgs>(dataAvailable);
            sourceStream.RecordingStopped += new EventHandler<NAudio.Wave.StoppedEventArgs>(StopRecording);

            sourceStream.StartRecording();

        }
        public bool OpenMp3File(string path)
        {
            if (path == null)
                throw new ArgumentNullException("OpenMp3File : path is null!");

            if (mp3Reader != null)
            {
                mp3Reader.Close();
                mp3Reader.Dispose();
                mp3Reader = null;
            }

            mp3Reader = new Mp3FileReader(path);

            return (mp3Reader == null) ? false : true;
        }
        public Mp3Frame GetNextMp3Frame()
        {
            if (mp3Reader == null)
            {
                return null;
                /*string file = @"H:\stuff\MP3's\Anime\Hideyuki Fukasawa - EMIYA (UBW Extended).mp3"; //@"H:\stuff\MP3's\Alestorm\2011 - Back Through Time\13 - You Are A Pirate.mp3";
                mp3Reader = new NAudio.Wave.Mp3FileReader(file);*/
            }
            NAudio.Wave.Mp3Frame frame = mp3Reader.ReadNextFrame();

            if (waveFormat == null && frame != null)
            {
                waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                        frame.FrameLength, frame.BitRate);
            }
            return frame;
        }
        public int RewindMP3()
        {
            mp3Reader.Seek(0, System.IO.SeekOrigin.Begin);
            return 0;
        }

    }
}
