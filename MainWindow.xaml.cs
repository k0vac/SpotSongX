using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
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

namespace SongSpotX
{


    [Serializable]
    public class Config {
        int revision = 5;
        public string savePath;
        public int pollRate;
        public Config() {
            savePath = "Songspot.txt";
            pollRate = 1000;
        }

        public void changeSave(string sp) {
            savePath = sp;
        }

        public void changePoll(int rate) {
            pollRate = rate;
        }
        public int revisionVersion() {
            return revision;
        }
    }
    public partial class MainWindow : Window
    {
        Config conf;
        Process proc;
        FileStream confStream;
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        string lastSong;

        public MainWindow()
        {
            lastSong = "";
            ConfigCheck();
            InitializeComponent();
        }

        private void ConfigCheck()
        {
            if (File.Exists("conf.kcp"))
            {
                confStream = new FileStream("conf.kcp", FileMode.Open);
                try { 
                    conf = (Config)binaryFormatter.Deserialize(confStream);
                    Debug.WriteLine(conf.pollRate);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error Reading Config File, your settings have been reset. Error has been written to a file.\n");
                    ErrorWrite(e);
                    conf = new Config();
                    confStream.Close();
                    confStream = new FileStream("conf.kcp", FileMode.Create);
                    binaryFormatter.Serialize(confStream, conf);
                    confStream.Close();
                }
                if (conf.revisionVersion() != new Config().revisionVersion())
                {
                    MessageBox.Show("Outdated Config Revision. Deleting old configuration.");
                    confStream.Close();
                    File.Delete("conf.kcp");
                    ConfigCheck();
                }
                confStream.Close();
            }
            else
            {
                conf = new Config();
                confStream = new FileStream("conf.kcp", FileMode.Create);
                binaryFormatter.Serialize(confStream, conf);
                confStream.Close();
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Save Path is " + conf.savePath);
            Debug.WriteLine("Poll Rate: " + conf.pollRate);

            switch (conf.pollRate)
            {
                case 500:
                    comboBox.SelectedIndex = 0;
                    break;
                case 1000:
                    comboBox.SelectedIndex = 1;
                    break;
                case 2000:
                    comboBox.SelectedIndex = 2;
                    break;
            }

            Process[] procs = Process.GetProcessesByName("Spotify");

            foreach (Process proces in procs)
                if (proces.MainWindowTitle != "")
                    proc = proces;

            await Updater();
        }

        private async Task Updater()
        {

            while (true)
            {
                await Task.Delay(conf.pollRate);

                try
                {
                    proc = Process.GetProcessById(proc.Id);
                    spotifyIDActiveLabel.Content = proc.Id;
                    directoryActiveLabel.Content = conf.savePath;

                    if (lastSong != proc.MainWindowTitle)
                    {
                        if (proc.MainWindowTitle != "Spotify Free")
                        {
                            songPlayingActiveLabel.Content = proc.MainWindowTitle;
                            File.WriteAllText(conf.savePath, proc.MainWindowTitle);
                            lastSong = proc.MainWindowTitle;
                        }
                        else {
                            songPlayingActiveLabel.Content = "No Song Playing.";
                            lastSong = proc.MainWindowTitle;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message.ToString());
                    spotifyIDActiveLabel.Content = "Searching for process...";
                    await SpotifySearcher();
                    return;
                }

                
            }
        }

        private async Task SpotifySearcher() {
            while (true)
            {
                await Task.Delay(1000);
                Process[] procs = Process.GetProcessesByName("Spotify");
                if (procs.Length > 0)
                    foreach (Process proces in procs)
                        if (proces.MainWindowTitle != "")
                        {
                            proc = proces;
                            await Updater();
                            return;
                        }
            }
        }

        private void Ellipse_MouseUp(object sender, MouseButtonEventArgs e)
        {
            confStream = new FileStream("conf.kcp", FileMode.Create);
            binaryFormatter.Serialize(confStream, conf);
            confStream.Close();

            Debug.WriteLine("Polling rate saved: " + conf.pollRate);
            this.Close();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Unloading...");
            
        }

        private void ErrorWrite(Exception e) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("Error at " + DateTime.Now.ToString("g"));
            sb.AppendLine("\n" + e.Message);
            File.AppendAllText("logs\\err.txt",sb.ToString());
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if(comboBox.IsLoaded)
            switch (comboBox.SelectedIndex) {
                case 0:
                    conf.changePoll(500);
                    break;
                case 1:
                    conf.changePoll(1000);
                    break;
                case 2:
                    conf.changePoll(2000);
                    break;
            }
        }

        private void changeDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sv = new SaveFileDialog();
            sv.Filter = "Text Files (*.txt)|*.txt";
            sv.ShowDialog();

            if (sv.FileName != "") {
                conf.changeSave(sv.FileName);
            }
        }

        private void Rectangle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            
        }

        private void textBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://twitter.com/_vkovac"));
        }
    }
}
