using System;
using NAudio.Wave;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace AudioStreaming
{
    public class AudioPlayer : AudioBackend
    {
        //variables
        //-------------------
        private NAudio.Wave.DirectSoundOut waveOut = null;
        private AcmMp3FrameDecompressor decompressor = null;
        protected NAudio.Wave.VolumeWaveProvider16 volumeHandler = null;

            //the handler for the thread monitor
        public readonly object thread_monitor = new object();

        public bool bFileEnding = false;

        /// <summary>
        /// Get or Set the Volume of the player. 1 = 100% , 0 = 0%
        /// </summary>
        private float volume = 1;
        public float Volume
        {
            get
            {
                if (volumeHandler == null)
                    return volume*100;
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

        public bool Paused 
        {
            get
            {
                if (waveOut == null)
                    return false;

                return (waveOut.PlaybackState == PlaybackState.Paused);
            }
            set
            {
                if (waveOut != null)
                {
                    if (value == true)
                    {
                        waveOut.Pause();
                    }
                    else
                    {
                        waveOut.Play();
                    }
                }
                OnPropertyChanged("Paused");
            }
        }


        public delegate void BackendHandler(object sender, bool State);
        public event BackendHandler backendHandler;

        //functions
        //--------------------------------

        protected override bool IsBackendValid()
        {
            if (out_buffer == null || ( waveOut == null && decompressor == null) )
                return false;
            return true;
        }
        public bool IsInit()
        {
            return IsBackendValid();
        }

        public bool IsPlaying()
        {
            if (waveOut == null)
                return false;
            return (waveOut.PlaybackState == PlaybackState.Playing);
        }

        public void PausePlayer(bool pause)
        {
            Paused = pause;
        }
        //The StopPlaying/Recording functions which will call KillAll which kills both recording and playing streams.
        //public void StopPlaying(object sender, EventArgs e)
        public void StopPlayer()
        {
            //signal event we are shutting down
            if (backendHandler != null)
                backendHandler(this, false);

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
        public void StartPlaying()
        {
            if (waveFormat == null)
                return;

            //setup the output stream, using the provider
            waveOut = new NAudio.Wave.DirectSoundOut();

            //setup the 'provider'. from what i understood this is what takes the input wave and converts it into what directsound understands. which afaik is PCM
            out_buffer = new BufferedWaveProvider(waveFormat);
            volumeHandler = new VolumeWaveProvider16(out_buffer);
            volumeHandler.Volume = volume;

            waveOut.Init(volumeHandler);

            //signal event we are set up
            if (backendHandler != null)
                backendHandler(this, true);

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
            StopPlayer();
            bFileEnding = false;
            //setup the output stream, using the provider & mp3Frame
            waveOut = new NAudio.Wave.DirectSoundOut();

            SetWaveFormat(frame);

            decompressor = new AcmMp3FrameDecompressor(waveFormat);
            out_buffer = new BufferedWaveProvider(decompressor.OutputFormat);
            volumeHandler = new VolumeWaveProvider16(out_buffer);
            //1.0 = full volume, 0.0 = silence
            volumeHandler.Volume = volume;
            waveOut.Init(volumeHandler);

            //signal event we are set up
            if (backendHandler != null)
                backendHandler(this, true);

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
        /// <returns>the amout of seconds left in buffer</returns>
        public double WaitForMoreData()
        {
            TimeSpan sleepTime = TimeSpan.FromSeconds(0.01);
            //prevent devide by 0
            if (out_buffer != null && out_buffer.BufferDuration.TotalSeconds >= 1)
            {
                double devider = 5;
                for (double i = out_buffer.BufferedDuration.TotalSeconds; i < 3 && i > 0; i--)
                {
                    //basically, this'll make the devider bigger so we sleep less when we have to little data
                    // +3s : devide by 5
                    // 2s : devide by 500
                    // 1s : devide by 50 000
                    // 0s : devide by 5 000 000
                    devider *= 100;
                }
                sleepTime = TimeSpan.FromSeconds(out_buffer.BufferedDuration.TotalSeconds / (1 * devider));
            }

            //we moved away from Thread.Sleep because it couldn't be interrupted for commands or interraction. 
            //this makes the thread sleep but its possible to get it back in action by a external pulse.
            lock (thread_monitor)
            {
                Monitor.Wait(thread_monitor, sleepTime);
            }

            BufferLenght = out_buffer==null?0:out_buffer.BufferedDuration.TotalSeconds;

            //return bufferlenght in seconds, this is good if we wanna catch how much data is left for like when we wanna reinit or close after current song
            return Convert.ToDouble(out_buffer == null ? 0 : out_buffer.BufferedDuration.TotalSeconds);
        }
        public double GetBufferLenght()
        {
            BufferLenght = out_buffer==null?0:out_buffer.BufferedDuration.TotalSeconds;

            //return bufferlenght in seconds, this is good if we wanna catch how much data is left for like when we wanna reinit or close after current song
            return Convert.ToDouble(out_buffer == null ? 0 : out_buffer.BufferedDuration.TotalSeconds);
        }

    }
}
