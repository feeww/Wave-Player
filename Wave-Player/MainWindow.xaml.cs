﻿using System;
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
using System.Runtime.InteropServices;
using System.Windows.Shell;
using System.Drawing;
using Wave_Player.classes.Managers;

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
        private readonly NotificationSystem notificationSystem;
        public MainWindow()
        {
            MediaPlayer = new MediaElement();
            InitializeComponent();
            LoadSettings();

            NotificationSystem.Initialize(this);
            NotificationSystem.Show($"Welcome back to Wave!", NotificationType.Success, 6000);

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
                _fileWatcher = new FileSystemWatcher(_settings.DefaultMusicFolder, "*.mp3;*.wav;*.flac;*.m4a");
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
                string[] supportedExtensions = { "*.mp3", "*.wav", "*.flac", "*.m4a" };

                foreach (string extension in supportedExtensions)
                {
                    string[] musicFiles = Directory.GetFiles(_settings.DefaultMusicFolder, extension);
                    foreach (string file in musicFiles)
                    {
                        _trackPaths.Add(file);
                        TrackList.Items.Add(Path.GetFileName(file));
                    }
                }
            }

            Dispatcher.Invoke(UpdateTrackListAppearance, DispatcherPriority.Loaded);
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
        private void Close_Click(object sender, RoutedEventArgs e){ SaveMultiRepeatState(); Close();}

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Multiselect = true,
                Filter = "Audio Files|*.mp3;*.wav;*.flac;*.m4a|MP3 Files (*.mp3)|*.mp3|WAV Files (*.wav)|*.wav|FLAC Files (*.flac)|*.flac|M4A Files (*.m4a)|*.m4a"
            };

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
                UpdateTaskbarThumbButton();
            }
            if (TrackList.SelectedIndex != -1)
            {
                _playHistory.Add(TrackList.SelectedIndex);

                if (_playHistory.Count > MAX_PLAY_HISTORY)
                {
                    _playHistory.RemoveAt(0);
                }
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
            UpdateTaskbarThumbButton();
        }


        private List<int> _playHistory = new List<int>();
        private const int MAX_PLAY_HISTORY = 10;

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (_trackPaths.Count == 0) return;

            if (MediaPlayer.Position.TotalSeconds >= 5)
            {
                MediaPlayer.Position = TimeSpan.Zero;
                return;
            }

            if (!_isShuffleEnabled)
            {
                if (TrackList.SelectedIndex > 0)
                {
                    TrackList.SelectedIndex--;
                }
                else
                {
                    TrackList.SelectedIndex = _trackPaths.Count - 1;
                }
            }
            else
            {
                List<int> playedIndexes = new List<int> { TrackList.SelectedIndex };

                List<int> availableIndexes = Enumerable.Range(0, _trackPaths.Count)
                    .Where(i => !playedIndexes.Contains(i))
                    .ToList();

                if (availableIndexes.Count == 0)
                {
                    availableIndexes = Enumerable.Range(0, _trackPaths.Count)
                        .Where(i => i != TrackList.SelectedIndex)
                        .ToList();
                }

                int newIndex = availableIndexes[_random.Next(availableIndexes.Count)];
                TrackList.SelectedIndex = newIndex;
            }

            Play_Click(null, null);
        }


        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_trackPaths.Count > 1)
            {
                if (_isShuffleEnabled)
                {
                    int newIndex;
                    List<int> availableTracks = Enumerable.Range(0, _trackPaths.Count)
                        .Where(i => i != TrackList.SelectedIndex &&
                                    !_playHistory.Contains(i))
                        .ToList();

                    if (availableTracks.Count > 0)
                    {
                        newIndex = availableTracks[_random.Next(availableTracks.Count)];
                    }
                    else
                    {
                        do
                        {
                            newIndex = _random.Next(_trackPaths.Count);
                        } while (newIndex == TrackList.SelectedIndex);
                    }

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
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E04B2E"),
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF5E3A"),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(1, 1));
            }
            else
            {
                shuffleButton.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0"));
            }
        }

        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            Button repeatButton = (Button)sender;
            _isRepeatEnabled = !_isRepeatEnabled;

            if (_isRepeatEnabled && !_isShuffleEnabled)
            {
                repeatButton.Foreground = new LinearGradientBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E04B2E"),
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("FF5E3A"),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(1, 1));
            }
            else
            {
                repeatButton.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0"));
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

        private double _previousVolume = 0.2; 
        private bool _isMuted = false;

        private void VolumeSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            UpdateVolumeSliderValue(e.GetPosition(VolumeSlider).X);
        }

        private void VolumeSlider_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateVolumeSliderValue(e.GetPosition(VolumeSlider).X);
            }
        }

        private void VolumeSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            UpdateVolumeSliderValue(e.GetPosition(VolumeSlider).X);
        }

        private void UpdateVolumeSliderValue(double mouseX)
        {
            double ratio = Math.Max(0, Math.Min(1, mouseX / VolumeSlider.ActualWidth));
            double newValue = ratio * VolumeSlider.Maximum;

            VolumeSlider.Value = newValue;
            MediaPlayer.Volume = newValue;

            _isMuted = newValue == 0;
            UpdateMuteButtonAppearance();
        }

        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            if (!_isMuted)
            {
                _previousVolume = MediaPlayer.Volume;
                MediaPlayer.Volume = 0;
                VolumeSlider.Value = 0;
                _isMuted = true;
            }
            else
            {
                MediaPlayer.Volume = _previousVolume > 0 ? _previousVolume : 0.2;
                VolumeSlider.Value = _previousVolume > 0 ? _previousVolume : 0.2;
                _isMuted = false;
            }

            UpdateMuteButtonAppearance();
        }

        private void UpdateMuteButtonAppearance(){ MuteButton.Content = _isMuted ? "🔇" : "🔊"; }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MediaPlayer != null)
            {
                MediaPlayer.Volume = VolumeSlider.Value;

                _isMuted = VolumeSlider.Value == 0;
                UpdateMuteButtonAppearance();

                if (VolumeSlider.Value > 0)
                {
                    _previousVolume = VolumeSlider.Value;
                }
            }
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

        private void MultiRepeat_Click(object sender, RoutedEventArgs e)
        {
            _isMultiRepeatEnabled = !_isMultiRepeatEnabled;

            Button multiRepeatButton = (Button)sender;
            if (_isMultiRepeatEnabled)
            {
                multiRepeatButton.Foreground = new LinearGradientBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("FF5E3A"),
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("FF5E3A"),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(1, 1));
            }
            else
            {
                multiRepeatButton.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0"));
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
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("FF5E3A"),
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("FF5E3A"),
                            new System.Windows.Point(0, 0),
                            new System.Windows.Point(1, 1));
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
                    MultiRepeatButton.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0"));

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

        private void ThumbPrevious_Click(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Previous_Click(sender, null);
            });
        }

        private void ThumbPlayPause_Click(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (TrackList.SelectedIndex >= 0 && MediaPlayer.Source != null)
                {
                    if (PlayButton.Visibility == Visibility.Visible)
                    {
                        Play_Click(PlayButton, null);
                    }
                    else
                    {
                        Pause_Click(PauseButton, null);
                    }
                }
            });
        }

        private void ThumbNext_Click(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Next_Click(sender, null);
            });
        }

        private void UpdateTaskbarThumbButton()
        {
            Dispatcher.Invoke(() =>
            {
                var taskbarItemInfo = this.TaskbarItemInfo;

                if (taskbarItemInfo != null && taskbarItemInfo.ThumbButtonInfos.Count >= 2)
                {
                    var playPauseThumb = taskbarItemInfo.ThumbButtonInfos[1];

                    if (PlayButton.Visibility == Visibility.Visible)
                    {
                        playPauseThumb.ImageSource = (BitmapImage)Resources["TaskBarButtonPlay"];
                    }
                    else
                    {
                        playPauseThumb.ImageSource = (BitmapImage)Resources["TaskBarButtonPause"];
                    }
                }
            });
        }

    }
}


