using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using TagLib;

namespace Wave_Player
{
    public partial class MainWindow : Window
    {
        private List<string> _trackPaths = new();
        private DispatcherTimer _timer;
        private bool _isDraggingProgress = false;
        private Random _random = new Random();
        private bool _isShuffleEnabled = false;
        private bool _isRepeatEnabled = false;
        private SettingsC _settings;
        private FileSystemWatcher _fileWatcher;
        private readonly string _saveFilePath = "playlist.json";
        private readonly string _lastTrackFilePath = "lastTrack.json";
        private TimeSpan _pausedPosition;

        public MainWindow()
        {
            MediaPlayer = new MediaElement();
            InitializeComponent();
            LoadSettings();
            InitializeFileWatcher();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += UpdateProgress;
            LoadLastTrack();
        }

        private void LoadSettings()
        {
            _settings = SettingsC.Load();
            MediaPlayer.Volume = _settings.DefaultVolume;
            VolumeSlider.Value = _settings.DefaultVolume;

            LoadMusicFiles();
        }

        private void InitializeFileWatcher()
        {
            if (Directory.Exists(_settings.DefaultMusicFolder))
            {
                _fileWatcher = new FileSystemWatcher(_settings.DefaultMusicFolder, "*.mp3");
                _fileWatcher.Created += OnFileChanged;
                _fileWatcher.Deleted += OnFileChanged;
                _fileWatcher.Renamed += OnFileChanged;
                _fileWatcher.EnableRaisingEvents = true;
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LoadMusicFiles();
            });
        }

        private void LoadMusicFiles()
        {
            _trackPaths.Clear();
            TrackList.Items.Clear();

            if (Directory.Exists(_settings.DefaultMusicFolder))
            {
                string[] mp3Files = Directory.GetFiles(_settings.DefaultMusicFolder, "*.mp3");
                foreach (string file in mp3Files)
                {
                    _trackPaths.Add(file);
                    TrackList.Items.Add(Path.GetFileName(file));
                }
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new() { Multiselect = true, Filter = "MP3 Files (*.mp3)|*.mp3" };
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string file in openFileDialog.FileNames)
                {
                    if (!_trackPaths.Contains(file))
                    {
                        System.IO.File.Copy(file, Path.Combine(_settings.DefaultMusicFolder, Path.GetFileName(file)), true);
                    }
                }
            }
        }

        private void DeleteTrack_Click(object sender, RoutedEventArgs e)
        {
            if (TrackList.SelectedIndex != -1)
            {
                int index = TrackList.SelectedIndex;
                string filePath = _trackPaths[index];
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (TrackList.SelectedIndex >= 0)
            {
                string selectedTrack = _trackPaths[TrackList.SelectedIndex];

                // Якщо трек ще не завантажений або змінився — завантажуємо його заново
                if (MediaPlayer.Source == null || !MediaPlayer.Source.OriginalString.Equals(selectedTrack, StringComparison.OrdinalIgnoreCase))
                {
                    MediaPlayer.Source = new Uri(selectedTrack);
                    MediaPlayer.Position = TimeSpan.Zero; // Новий трек починається з початку
                    UpdateTrackInfo(Path.GetFileNameWithoutExtension(selectedTrack), selectedTrack);
                }
                else
                {
                    // Якщо трек вже завантажений, відновлюємо позицію після паузи
                    MediaPlayer.Position = _pausedPosition;
                }

                MediaPlayer.Play();
                _timer.Start();

                PlayButton.Visibility = Visibility.Collapsed;
                PauseButton.Visibility = Visibility.Visible;

                SaveLastTrack();
            }
        }



        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (MediaPlayer.Source != null)
            {
                _pausedPosition = MediaPlayer.Position; // Зберігаємо позицію треку
                MediaPlayer.Pause();
                _timer.Stop();

                PlayButton.Visibility = Visibility.Visible;
                PauseButton.Visibility = Visibility.Collapsed;
            }
        }


        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (TrackList.SelectedIndex >= 0)
            {
                if (MediaPlayer.Position.TotalSeconds >= 5)
                {
                    MediaPlayer.Position = TimeSpan.Zero;
                }
                else if (TrackList.SelectedIndex > 0)
                {
                    TrackList.SelectedIndex--;
                }
                Play_Click(null, null);
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_trackPaths.Count > 1)
            {
                if (_isShuffleEnabled)
                {
                    int newIndex;
                    do
                    {
                        newIndex = _random.Next(_trackPaths.Count);
                    } while (newIndex == TrackList.SelectedIndex);
                    TrackList.SelectedIndex = newIndex;
                }
                else if (TrackList.SelectedIndex < _trackPaths.Count - 1)
                {
                    TrackList.SelectedIndex++;
                }
                Play_Click(null, null);
            }
        }

        private void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            Button shuffleButton = (Button)sender;

            _isShuffleEnabled = !_isShuffleEnabled;

            if (_isShuffleEnabled && IsRepeatEnabled == false)
            {
                shuffleButton.Foreground = new LinearGradientBrush(
                    System.Windows.Media.Color.FromRgb(0, 209, 255),
                    System.Windows.Media.Color.FromRgb(0, 128, 255),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(1, 1));
            }
            else
            {
                shuffleButton.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#B3B3B3"));
            }
        }

        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            Button repeatButton = (Button)sender;

            _isRepeatEnabled = !_isRepeatEnabled;

            if (_isRepeatEnabled && IsShuffleEnabled == false)
            {
                repeatButton.Foreground = new System.Windows.Media.LinearGradientBrush(
                    System.Windows.Media.Color.FromRgb(0, 209, 255),
                    System.Windows.Media.Color.FromRgb(0, 128, 255),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(1, 1));
            }
            else
            {
                repeatButton.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#B3B3B3"));
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();

            if (settingsWindow.DialogResult == true)
            {
                MediaPlayer.Volume = _settings.DefaultVolume;
                VolumeSlider.Value = _settings.DefaultVolume; // Ensure VolumeSlider is updated after settings are applied
            }
        }

        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            _pausedPosition = TimeSpan.Zero; 

            if (_isRepeatEnabled)
            {
                MediaPlayer.Position = TimeSpan.Zero;
                MediaPlayer.Play();
            }
            else if (_isShuffleEnabled && _trackPaths.Count > 1)
            {
                int newIndex;
                do
                {
                    newIndex = _random.Next(_trackPaths.Count);
                } while (newIndex == TrackList.SelectedIndex);

                TrackList.SelectedIndex = newIndex;
                Play_Click(null, null);
            }
            else if (TrackList.SelectedIndex < _trackPaths.Count - 1)
            {
                TrackList.SelectedIndex++;
                Play_Click(null, null);
            }
            else
            {
                SaveLastTrack();
            }
        }

        private void UpdateProgress(object sender, EventArgs e)
        {
            if (MediaPlayer.NaturalDuration.HasTimeSpan && !_isDraggingProgress)
            {
                ProgressBar.Maximum = MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                ProgressBar.Value = MediaPlayer.Position.TotalSeconds;
                CurrentTime.Text = MediaPlayer.Position.ToString(@"m\:ss");
                TotalTime.Text = MediaPlayer.NaturalDuration.TimeSpan.ToString(@"m\:ss");
            }
        }

        private void TrackList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TrackList.SelectedIndex != -1)
            {
                Play_Click(null, null);
            }
        }

        private void ProgressBar_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingProgress = true;
            UpdateProgressBarValue(e.GetPosition(ProgressBar).X);
        }

        private void ProgressBar_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            UpdateProgressBarValue(e.GetPosition(ProgressBar).X);
            _isDraggingProgress = false;
        }

        private void UpdateProgressBarValue(double mouseX)
        {
            double ratio = mouseX / ProgressBar.ActualWidth;
            double newValue = ratio * ProgressBar.Maximum;
            ProgressBar.Value = newValue;
            MediaPlayer.Position = TimeSpan.FromSeconds(newValue);
        }

        private void VolumeSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            UpdateVolumeSliderValue(e.GetPosition(VolumeSlider).X);
        }

        private void VolumeSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            UpdateVolumeSliderValue(e.GetPosition(VolumeSlider).X);
        }

        private void UpdateVolumeSliderValue(double mouseX)
        {
            double ratio = mouseX / VolumeSlider.ActualWidth;
            double newValue = ratio * VolumeSlider.Maximum;
            VolumeSlider.Value = newValue;
            MediaPlayer.Volume = newValue;
        }

       
        

        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            double previousVolume = 0.2;

            if (MediaPlayer.Volume > 0)
            {
                previousVolume = MediaPlayer.Volume;
                MediaPlayer.Volume = 0;             
                VolumeSlider.Value = 0;               
                MuteButton.Content = "🔇";             
            }
            else
            {
                MediaPlayer.Volume = previousVolume;  
                VolumeSlider.Value = previousVolume;
                MuteButton.Content = "🔊";             
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MediaPlayer != null)
            {
                MediaPlayer.Volume = VolumeSlider.Value;
            }
        }

        private void LoadPlaylist()
        {
            if (System.IO.File.Exists(_saveFilePath))
            {
                try
                {
                    _trackPaths = JsonSerializer.Deserialize<List<string>>(System.IO.File.ReadAllText(_saveFilePath)) ?? new List<string>();
                    foreach (var path in _trackPaths)
                    {
                        TrackList.Items.Add(Path.GetFileName(path));
                    }
                }
                catch (Exception)
                {
                    _trackPaths = new List<string>();
                }
            }
        }

        private void SavePlaylist()
        {
            System.IO.File.WriteAllText(_saveFilePath, JsonSerializer.Serialize(_trackPaths));
        }

        private void LoadLastTrack()
        {
            if (_settings.RememberLastTrack && System.IO.File.Exists(_lastTrackFilePath))
            {
                string lastTrack = System.IO.File.ReadAllText(_lastTrackFilePath);
                if (_trackPaths.Contains(lastTrack))
                {
                    TrackList.SelectedIndex = _trackPaths.IndexOf(lastTrack);
                    Play_Click(null, null);
                }
            }
        }

        private void SaveLastTrack()
        {
            if (_settings.RememberLastTrack && TrackList.SelectedIndex >= 0)
            {
                string currentTrack = _trackPaths[TrackList.SelectedIndex];
                System.IO.File.WriteAllText(_lastTrackFilePath, currentTrack);
            }
        }

        private void UpdateTrackInfo(string trackName, string trackPath)
        {
            CurrentTrackName.Text = trackName;

            // Extract album art from the MP3 file
            var file = TagLib.File.Create(trackPath);
            if (file.Tag.Pictures.Length > 0)
            {
                var bin = (byte[])(file.Tag.Pictures[0].Data.Data);
                using (var stream = new MemoryStream(bin))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = stream;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    AlbumCover.ImageSource = image;
                }
            }
            else
            {
                // Set a default image if no album art is found
                AlbumCover.ImageSource = new BitmapImage(new Uri("assets/wave.png", UriKind.Relative));
            }
        }

        private void ResetAnimation(object sender, EventArgs e)
        {
            //CurrentTrackNameTransform.X = -150;
        }



    }
}


