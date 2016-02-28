using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Ookii.Dialogs.Wpf;
using System.Diagnostics;
using AudioStreaming.Utils;

namespace AudioStreaming.Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //----------------------
        //subclasses
        //----------------------
        /// <summary>
        /// class to contain the device information
        /// </summary>
        public class Device
        {
            public string Device_name { get; set; }
            public int Channels { get; set; }
        }


        //----------------------
        //variables
        //----------------------
        public IList<Device> Devices { get; set; }
        audioServer Server;
        DebugListener debug;
        //functions
        public MainWindow()
        {
            InitializeComponent();

            Server = new audioServer();
            grdMain.DataContext = Server;
            listDevices.DataContext = this;
            GetDevices();

            if (GetDevicesCount() > 0 && listDevices.SelectedIndex < 0)
            {
                listDevices.SelectedIndex = 0;
            }

            Server.mp3Path = System.IO.Directory.GetCurrentDirectory(); //@"H:\stuff\MP3's\"  
            debug = new DebugListener(txtDebug);
            Debug.Listeners.Add(debug);  
          
        }

        private void btStart_Click(object sender, RoutedEventArgs e)
        {
            if (GetDevicesCount() > 0)
            {
                Server.StartServer(listDevices.SelectedIndex);

            }
        }

        private void btStop_Click(object sender, RoutedEventArgs e)
        { 
            Server.StopServer();
        }

        private void GetMp3Path(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Please select a folder.";
            dialog.UseDescriptionForTitle = true; // This applies to the Vista style dialog only, not the old dialog.
            dialog.ShowNewFolderButton = true;
            dialog.SelectedPath = txtMp3Path.Text;

            if ((bool)dialog.ShowDialog() == true)
            {
                if (dialog.SelectedPath != null && dialog.SelectedPath.Length > 0)
                    txtMp3Path.Text = dialog.SelectedPath;
            }
            
        }

        private void CloseApp(object sender, EventArgs e)
        {
            Server.KillThread();
        }

        /// <summary>
        /// Gets all audio devices installed on the device and store them in 'Devices' which is data linked to the GUI
        /// </summary>
        private void GetDevices()
        {
            List<NAudio.Wave.WaveInCapabilities> devices = new List<NAudio.Wave.WaveInCapabilities>();

            //gets the input (wavein) devices and adds them to the list
            for (short i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++)
            {
                devices.Add(NAudio.Wave.WaveIn.GetCapabilities(i));
            }

            //claer the listview module
            Devices = new List<Device>();

            //each device gets inserted into the devices list, which is linked to the listdevices listview module
            foreach (var device in devices)
            {
                ListViewItem item = new ListViewItem();
                item.Content = device.ProductName;
                Devices.Add(new Device() { Device_name = device.ProductName, Channels = device.Channels });
            }
        }

        public int GetDevicesCount()
        {
            return Devices.Count;
        }
    }
}
