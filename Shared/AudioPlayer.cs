using System;
using NAudio.Wave;
using System.ComponentModel;
using System.Diagnostics;

namespace AudioStreaming
{
    public class AudioPlayer : AudioBackend
    {
        //variables
        //-------------------
        private NAudio.Wave.DirectSoundOut waveOut = null;
        private AcmMp3FrameDecompressor decompressor = null;
        protected NAudio.Wave.VolumeWaveProvider16 volumeHandler = null;

        /// <summary>
        /// Get or Set the Volume of the player. 1 = 100% , 0 = 0%
        /// </summary>
        private float volume = 1;
        public float Volume
        {
            get
            {
                if (volumeHandler == null)
                    return -1;
                return (volumeHandler.Volume >= 1)?100:volumeHandler.Volume*100;
            }
            set
            {
                if (volumeHandler != null)
                {
                    //if the value is > 1, we set it to 1. 1 = 100% volume in the handler
                    value = (value >= 100) ? 1 : value / 100;
                    volumeHandler.Volume = value;
                }
                volume = value;
                OnPropertyChanged("Volume");
            }
        }
        //functions
        //--------------------------------

        protected override bool IsBackendValid()
        {
            if (out_buffer == null || ( waveOut == null && decompressor == null) )
                return false;
            return true;
        }

        //The StopPlaying/Recording functions which will call KillAll which kills both recording and playing streams.
        //public void StopPlaying(object sender, EventArgs e)
        public void StopPlaying()
        {

            if (decompressor != null)
            {
                decompressor.Dispose();
                decompressor = null;
            }

            if (volumeHandler != null)
            {
                volumeHandler = null;
            }

            if (waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
            }
            KillAll();
        }

        //Set's up for playing a stream. basically sets up the DirectSound and the buffer. to add audio to the buffer we call AddSamples with the data
        public void StartPlaying(float volume)
        {
            if (waveFormat == null)
                return;

            //setup the output stream, using the provider
            waveOut = new NAudio.Wave.DirectSoundOut();

            //setup the 'provider'. from what i understood this is what takes the input wave and converts it into what directsound understands. which afaik is PCM
            out_buffer = new BufferedWaveProvider(waveFormat);
            volumeHandler = new VolumeWaveProvider16(out_buffer);
            Volume = volume;

            waveOut.Init(volumeHandler);

            waveOut.Play();
        }


        //Add Samples to the Buffer which is played by the player
        public byte AddSamples(ref byte[] data)
        {
            return AddSamples(data, data.Length);
        }
        public byte AddSamples(byte[] data, int data_lenght)
        {
            if (data == null || data.Length <= 0 || !IsBackendValid())
                return 0;

            if (out_buffer.BufferLength > out_buffer.BufferedBytes + data.Length)
            {
                out_buffer.AddSamples(data, 0, data.Length);
            }
            else
                throw new Exception("AddSamples : Buffer out of bounds");

            return 1;
        }

        public void SetupBackend(Mp3Frame frame)
        {
            //first cleanup the the backend. the old MP3 info could be different
            StopPlaying();
            //setup the output stream, using the provider & mp3Frame
            waveOut = new NAudio.Wave.DirectSoundOut();

            SetWaveFormat(frame);

            decompressor = new AcmMp3FrameDecompressor(waveFormat);
            out_buffer = new BufferedWaveProvider(decompressor.OutputFormat);
            volumeHandler = new VolumeWaveProvider16(out_buffer);
            //1.0 = full volume, 0.0 = silence
            volumeHandler.Volume = volume;
            waveOut.Init(volumeHandler);
            waveOut.Play();
        }
        public int AddNextFrame(byte[] frame)
        {
            if (frame == null)
                throw new ArgumentNullException();

            //make a stream out of the array
            System.IO.Stream stream = new System.IO.MemoryStream(frame);

            //so we can then read the array into the frame variable
            Mp3Frame tempframe = Mp3Frame.LoadFromStream(stream);

            //and then pass it to the actual function
            if (tempframe != null)
                return AddNextFrame(tempframe);
            else 
                return 0;
        }
        public int AddNextFrame(Mp3Frame frame)
        {
            if (frame == null)
                throw new ArgumentNullException("given frame = null");

            //when receiving data...
            if (out_buffer == null)
            {
                SetupBackend(frame);
            }
            byte[] buffer = new byte[16384 * 50];
            int decompressed = 0;

            if (frame != null)
            {
                decompressed = decompressor.DecompressFrame(frame, buffer, 0);

                if (out_buffer.BufferedBytes + decompressed < out_buffer.BufferLength)
                {
                    out_buffer.AddSamples(buffer, 0, decompressed);//AddSamples(buffer,decompressed);//out_buffer.AddSamples(buffer, 0, decompressed);
                }
            }
            return decompressed;
        }

        /// <summary>
        /// let the thread sleep while the buffer is full enough.
        /// </summary>
        /// <returns>the amout of seconds the app waited</returns>
        public double WaitForMoreData()
        {
            TimeSpan sleepTime = TimeSpan.FromSeconds(0.01);
            if (out_buffer.BufferedDuration >= TimeSpan.FromSeconds(3))
            {
                sleepTime = TimeSpan.FromSeconds(out_buffer.BufferedDuration.TotalSeconds / (1 * 8));
            }
            else if (out_buffer.BufferedDuration > TimeSpan.FromSeconds(2))
            {
                sleepTime = TimeSpan.FromSeconds(out_buffer.BufferedDuration.TotalSeconds / (1*500));
            }

            System.Threading.Thread.Sleep(sleepTime);

            BufferLenght = out_buffer.BufferedDuration.TotalSeconds;
            return Convert.ToDouble(sleepTime.TotalSeconds);
        }

    }
}
