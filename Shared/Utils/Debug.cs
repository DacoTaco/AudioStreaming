using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AudioStreaming.Utils
{
    public class DebugListener : TraceListener
    {
        private TextBox tBox;

        public DebugListener(TextBox box)
        {
            this.tBox = box;
        }

        public override void Write(string msg)
        { 
            //allows tBox to be updated from different thread
            tBox.Dispatcher.Invoke(new Action(() => {/*tBox.Text += msg;*/tBox.AppendText(msg); }), DispatcherPriority.ContextIdle);
        }

        public override void WriteLine(string msg)
        {
            Write(msg + "\r\n");
        }
    }
}
