using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using TagLib;
using Wave_Player.classes;

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
        private MediaElement AlbumCoverMedia;
        private HashSet<int> _multiRepeatTracks = new HashSet<int>();
        private bool _isMultiRepeatEnabled = false;
        private readonly string _multiRepeatFilePath = "multiRepeat.json";


        public MainWindow()
        {
            MediaPlayer = new MediaElement();
            InitializeComponent();
            LoadSettings();

            NotificationSystem.Initialize(this);
            NotificationSystem.Show($"Welcome back to Wave!", NotificationType.Success, 6000);

            UpdateThemeColors(_settings.Theme.PrimaryColor, _settings.Theme.SecondaryColor);

            InitializeFileWatcher();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += UpdateProgress;
            LoadLastTrack();
            LoadMultiRepeatState();
        }

        private void LoadSettings()
        {
            _settings = SettingsC.Load();
            MediaPlayer.Volume = _settings.DefaultVolume;
            VolumeSlider.Value = _settings.DefaultVolume;
            _isShuffleEnabled = _settings.AutoShuffle;

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

            Dispatcher.Invoke(() => UpdateTrackListAppearance(), DispatcherPriority.Loaded);
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
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            SaveMultiRepeatState();
            Close();
        }

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
            Button playButton = (Button)sender;

            PlayButton.Background = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString(_settings.Theme.PrimaryColor),
            (Color)ColorConverter.ConvertFromString(_settings.Theme.SecondaryColor),
            new Point(0, 0),
            new Point(1, 1));

            if (TrackList.SelectedIndex >= 0)
            {
                string selectedTrack = _trackPaths[TrackList.SelectedIndex];

                if (MediaPlayer.Source == null || !MediaPlayer.Source.OriginalString.Equals(selectedTrack, StringComparison.OrdinalIgnoreCase))
                {
                    MediaPlayer.Source = new Uri(selectedTrack);
                    MediaPlayer.Position = TimeSpan.Zero;
                    UpdateTrackInfo(Path.GetFileNameWithoutExtension(selectedTrack), selectedTrack);
                }
                else
                {
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
                _pausedPosition = MediaPlayer.Position;
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

            if (_isShuffleEnabled && !_isRepeatEnabled)
            {
                shuffleButton.Foreground = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString(_settings.Theme.PrimaryColor),
                    (Color)ColorConverter.ConvertFromString(_settings.Theme.SecondaryColor),
                    new Point(0, 0),
                    new Point(1, 1));
            }
            else
            {
                shuffleButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            }
        }

        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            Button repeatButton = (Button)sender;
            _isRepeatEnabled = !_isRepeatEnabled;

            if (_isRepeatEnabled && !_isShuffleEnabled)
            {
                repeatButton.Foreground = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString(_settings.Theme.PrimaryColor),
                    (Color)ColorConverter.ConvertFromString(_settings.Theme.SecondaryColor),
                    new Point(0, 0),
                    new Point(1, 1));
            }
            else
            {
                repeatButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
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
                VolumeSlider.Value = _settings.DefaultVolume;
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
            else if (_isMultiRepeatEnabled && _multiRepeatTracks.Count > 0)
            {
                List<int> repeatTracks = _multiRepeatTracks.ToList();

                int currentMultiRepeatIndex = repeatTracks.IndexOf(TrackList.SelectedIndex);

                if (currentMultiRepeatIndex != -1)
                {
                    int nextIndex = (currentMultiRepeatIndex + 1) % repeatTracks.Count;
                    TrackList.SelectedIndex = repeatTracks[nextIndex];
                }
                else
                {
                    TrackList.SelectedIndex = repeatTracks[0];
                }

                Play_Click(null, null);
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
                    AlbumCoverImage.ImageSource = image;
                }
            }
            else
            {
                AlbumCoverImage.ImageSource = new BitmapImage(new Uri("assets/dance-smile.gif", UriKind.Relative));
            }
        }
        public void UpdateAlbumCover(string imagePath)
        {
            if (imagePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            {
                //AlbumCoverImage.Visibility = Visibility.Collapsed;
                AlbumCoverMedia.Visibility = Visibility.Visible;
                AlbumCoverMedia.Source = new Uri(imagePath);
                AlbumCoverMedia.Play();
            }
            else
            {
                AlbumCoverMedia.Visibility = Visibility.Collapsed;
                //AlbumCoverImage.Visibility = Visibility.Visible; 
                AlbumCoverImage.ImageSource = new BitmapImage(new Uri(imagePath));
            }
        }



        private void ResetAnimation(object sender, EventArgs e)
        {
            //CurrentTrackNameTransform.X = -150;
        }

        public void UpdateThemeColors(string primaryColor, string secondaryColor)
        {
            var playButtonStyle = FindResource("PlayPauseButton") as Style;
            if (!IsValidColor(primaryColor) || !IsValidColor(secondaryColor))
            {
                throw new ArgumentException("Invalid color value.");
            }
            if (playButtonStyle != null)
            {
                var newStyle = new Style(typeof(Button), playButtonStyle);
                var gradientBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1)
                };
                gradientBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(primaryColor), 0));
                gradientBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(secondaryColor), 1));

                newStyle.Setters.Add(new Setter(Button.BackgroundProperty, gradientBrush));
                Resources["PlayPauseButton"] = newStyle;
            }

            if (_isShuffleEnabled)
            {
                ShuffleButton.Foreground = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString(primaryColor),
                    (Color)ColorConverter.ConvertFromString(secondaryColor),
                    new Point(0, 0),
                    new Point(1, 1));
            }

            if (_isRepeatEnabled)
            {
                RepeatButton.Foreground = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString(primaryColor),
                    (Color)ColorConverter.ConvertFromString(secondaryColor),
                    new Point(0, 0),
                    new Point(1, 1));
            }

            Application.Current.Resources["PrimaryColor"] = (Color)ColorConverter.ConvertFromString(primaryColor);
            Application.Current.Resources["SecondaryColor"] = (Color)ColorConverter.ConvertFromString(secondaryColor);

            ProgressBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(primaryColor));
            VolumeSlider.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(primaryColor));
        }
        private bool IsValidColor(string color)
        {
            try
            {
                ColorConverter.ConvertFromString(color);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void MultiRepeat_Click(object sender, RoutedEventArgs e)
        {
            _isMultiRepeatEnabled = !_isMultiRepeatEnabled;

            Button multiRepeatButton = (Button)sender;
            if (_isMultiRepeatEnabled)
            {
                multiRepeatButton.Foreground = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString(_settings.Theme.PrimaryColor),
                    (Color)ColorConverter.ConvertFromString(_settings.Theme.SecondaryColor),
                    new Point(0, 0),
                    new Point(1, 1));
            }
            else
            {
                multiRepeatButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
                _multiRepeatTracks.Clear();
                UpdateTrackListAppearance();
            }
        }

        private void ToggleMultiRepeat(int trackIndex)
        {
            if (_isMultiRepeatEnabled)
            {
                if (_multiRepeatTracks.Contains(trackIndex))
                {
                    _multiRepeatTracks.Remove(trackIndex);
                }
                else
                {
                    _multiRepeatTracks.Add(trackIndex);
                }
                UpdateTrackListAppearance();
            }
        }

        private void UpdateTrackListAppearance()
        {
            for (int i = 0; i < TrackList.Items.Count; i++)
            {
                ListBoxItem item = (ListBoxItem)TrackList.ItemContainerGenerator.ContainerFromIndex(i);
                if (item != null)
                {
                    if (_multiRepeatTracks.Contains(i))
                    {
                        item.Foreground = new LinearGradientBrush(
                            (Color)ColorConverter.ConvertFromString(_settings.Theme.PrimaryColor),
                            (Color)ColorConverter.ConvertFromString(_settings.Theme.SecondaryColor),
                            new Point(0, 0),
                            new Point(1, 1));
                    }
                    else
                    {
                        item.Foreground = new SolidColorBrush(Colors.White);
                    }
                }
            }
        }

        private void TrackList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_isMultiRepeatEnabled && TrackList.SelectedIndex != -1)
            {
                ToggleMultiRepeat(TrackList.SelectedIndex);
                e.Handled = true; 
            }
            else
            {
                TrackList_SelectionChanged(sender, null);
            }
        }

        private void SaveMultiRepeatState()
        {
            if (_isMultiRepeatEnabled)
            {
                System.IO.File.WriteAllText(_multiRepeatFilePath, JsonSerializer.Serialize(_multiRepeatTracks));
            }
        }

        private void LoadMultiRepeatState()
        {
            if (System.IO.File.Exists(_multiRepeatFilePath))
            {
                try
                {
                    _multiRepeatTracks = JsonSerializer.Deserialize<HashSet<int>>(System.IO.File.ReadAllText(_multiRepeatFilePath)) ?? new HashSet<int>();

                    _isMultiRepeatEnabled = false;
                    MultiRepeatButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));

                    if (_multiRepeatTracks.Count > 0)
                    {
                        UpdateTrackListAppearance();
                    }
                }
                catch (Exception)
                {
                    _multiRepeatTracks = new HashSet<int>();
                }
            }
        }

    }
}


