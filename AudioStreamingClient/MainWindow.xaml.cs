using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Collections;
using AudioStreaming.Utils;

namespace AudioStreaming.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public static RoutedCommand RoutePause = new RoutedCommand();
        private audioClient Client = new audioClient();
        DebugListener debug;

        

        //functions
        public MainWindow()
        {
            //setup UI
            InitializeComponent();
            CommandBinding pause = new CommandBinding(RoutePause, cmdPausePlayer);
            this.CommandBindings.Add(pause);
            KeyGesture keyPause = new KeyGesture(Key.Space,ModifierKeys.None);
            btConnect.Focus();

            //enable debugging output
            debug = new DebugListener(txtDebug);
            Debug.Listeners.Add(debug);


            //assinging datacontext to all the stuff...
            this.DataContext = Client;
            txbHostname.DataContext = Client.settings;
            cbMp3Mode.DataContext = Client.settings;
            cbCompress.DataContext = Client.settings;
            btnNext.DataContext = Client.settings;
            btnPrev.DataContext = Client.settings;

            //for the disabling of controls
            stConnections.DataContext = Client;


        }

        private void cmdPausePlayer(object sender, ExecutedRoutedEventArgs e)
        {
            if(cbMp3Mode.IsChecked == true && Client.ThreadAlive)
                Client.Paused = true;
        }
        private void cmdConnect(object sender, ExecutedRoutedEventArgs e)
        {
            Connect();
        }
        private void Connect()
        {
            //isChecked is a 3 state bool. you can't pass them on to a regular bool.
            //hence we check if its true or not. basically combining 2 states into false
            Client.StartConnection();//txbHostname.Text, (cbCompress.IsChecked == true) ? true : false, (cbMp3Mode.IsChecked == true) ? true : false);
        }
        private void btConnect_Click(object sender, RoutedEventArgs e)
        {
            Connect();
        }
        private void Disconnect(object sender, RoutedEventArgs e)
        {
            Client.StopConnection();
        }

        private void CloseApp(object sender, EventArgs e)
        {
            Client.StopConnection();
        }

        private void ChangeUISize(object sender, RoutedEventArgs e)
        {
            if( sender.GetType() != expDebug.GetType() && ( ((Expander)sender).Name != expDebug.Name))
                return;
            if (expDebug.IsExpanded)
                mainWindow.Height += 200;
            else
                mainWindow.Height -= 200;
        }

        private void PlayerControl(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                switch (((Button)sender).Name)
                {
                    case "btnPrev":
                        Client.RequestPrev();
                        break;
                    case "btnNext":
                        Client.RequestNext();
                        break;
                    default:
                        break;
                }
            }
            else
                return;
        }
    }
}
