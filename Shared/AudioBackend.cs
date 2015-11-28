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

        public bool CompareWaveformat(Mp3Frame frame)
        {
            var temp = (Mp3WaveFormat)waveFormat;
            if(frame.SampleRate == waveFormat.SampleRate &&
                (frame.ChannelMode == ChannelMode.Mono ? 1 : 2) == waveFormat.Channels &&
                frame.FrameLength == ((Mp3WaveFormat)temp).blockSize)
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
