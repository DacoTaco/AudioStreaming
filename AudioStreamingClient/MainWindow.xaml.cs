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

namespace AudioStreaming.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private audioClient Client = new audioClient();

        //functions
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = Client;//this;
            slVolume.Value = 90;

        }
        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            cbCompress.IsEnabled = (cbMp3Mode.IsChecked == true)?false:true;

        }
        private void btConnect_Click(object sender, RoutedEventArgs e)
        {
            //isChecked is a 3 state bool. you can't pass them on to a regular bool.
            //hence we check if its true or not. basically combining 2 states into false
            Client.StartConnection(txbHostname.Text, (cbCompress.IsChecked == true) ? true : false, (cbMp3Mode.IsChecked == true) ? true : false);
        }
        private void Disconnect(object sender, RoutedEventArgs e)
        {
            Client.KillThread();
        }
    }
}
