﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Ookii.Dialogs.Wpf;

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

        //functions
        public MainWindow()
        {
            InitializeComponent();

            Server = new audioServer();
            this.DataContext = Server;

            if (Server.GetDevicesCount() > 0 && listDevices.SelectedIndex < 0)
            {
                listDevices.SelectedIndex = 0;
            }

            txtMp3Path.Text = System.IO.Directory.GetCurrentDirectory(); //@"H:\stuff\MP3's\"          
        }

        private void btStart_Click(object sender, RoutedEventArgs e)
        {
            if (Server.GetDevicesCount() > 0)
            {
                Server.StartServer(listDevices.SelectedIndex,txtMp3Path.Text);

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
    }
}