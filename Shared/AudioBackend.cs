using NAudio.Wave;

namespace AudioStreaming
{
    public class AudioBackend : NotifyPropertyChange
    {

        ///VERSION OF BACKEND
        ///THIS IS FOR BOTH CLIENT AND SERVER
        public short _VERSION
        {
            get { return 0x0002; }
        }

        //variables
        protected WaveFormat waveFormat = null;
        protected BufferedWaveProvider out_buffer = null;


        private double bufferSize = 0;
        public double BufferLenght
        {
            get
            {
                return bufferSize;
            }
            set
            {
                double dv = value;
                if (dv > 5)
                    dv = 5;

                if (dv != 0)
                {
                    bufferSize = (dv / 5);
                    bufferSize *= 100;
                }
                else
                    bufferSize = 0;
                OnPropertyChanged("BufferLenght");
            }
        }




        //----------------------
        //functions
        //----------------------

        public AudioBackend()
        {

        }

        protected void KillAll()
        {
            if (waveFormat != null)
                waveFormat = null;

            if (out_buffer != null)
            {
                out_buffer.ClearBuffer();
                out_buffer = null;
                BufferLenght = 0;
            }
           
        }

        //WaveFormat functions. to be called when we are in playing from buffer so we know what format the buffer is
        public void SetWaveFormat(int samplerate, int channels)
        {
            if (samplerate == 0 || channels == 0)
                return;

            waveFormat = new WaveFormat(samplerate, channels);
        }

        public void SetWaveFormat(Mp3Frame frame)
        {
            if (frame == null)
                return;

            waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                        frame.FrameLength, frame.BitRate);

            return;
        }

        public bool IsWaveformatEqual(Mp3Frame frame)
        {
            //TODO : fix function. somehow it doesn't work 100% ...
            //hence return false so it always re-inits backend
            return false;

            Mp3WaveFormat new_mp3Format = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                        frame.FrameLength, frame.BitRate);

            if (waveFormat.GetType() != new_mp3Format.GetType())
                return false;
            
            Mp3WaveFormat CurrentFormat = (Mp3WaveFormat)waveFormat;
            if(
                CurrentFormat.SampleRate == new_mp3Format.SampleRate &&
                CurrentFormat.Channels == new_mp3Format.Channels &&
                CurrentFormat.BitsPerSample == new_mp3Format.BitsPerSample &&
                CurrentFormat.AverageBytesPerSecond == new_mp3Format.AverageBytesPerSecond &&
                CurrentFormat.blockSize == new_mp3Format.blockSize &&
                CurrentFormat.ExtraSize == new_mp3Format.ExtraSize)
            {

                return true;
            }
            else
            {
                return false;
            }
        }

        public int GetWaveSamples()
        {
            if (waveFormat == null)
                return 0;
            else
                return waveFormat.SampleRate;
        }
        public int GetWaveChannels()
        {
            if (waveFormat == null)
                return 0;
            else
                return waveFormat.Channels;
        }

        //check if the backend, both in playing or recording mode is valid
        protected virtual bool IsBackendValid()
        {
            if (out_buffer == null || waveFormat == null)
                return false;
            return true;
        }

    }
}
