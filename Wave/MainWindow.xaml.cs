using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Mp3Player
{
    public partial class MainWindow : Window
    {
        private List<string> _trackPaths = new();
        private readonly string _saveFilePath = "playlist.json";
        private DispatcherTimer _timer;
        private bool _isDraggingProgress = false;
        private Random _random = new Random();
        private bool _isShuffleEnabled = false;
        private bool _isRepeatEnabled = false;

        public MainWindow()
        {
            MediaPlayer = new MediaElement();
            InitializeComponent();
            LoadPlaylist();
            MediaPlayer.Volume = VolumeSlider.Value;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += UpdateProgress;
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
                        _trackPaths.Add(file);
                        TrackList.Items.Add(System.IO.Path.GetFileName(file));
                    }
                }
                SavePlaylist();
            }
        }

        private void DeleteTrack_Click(object sender, RoutedEventArgs e)
        {
            if (TrackList.SelectedIndex != -1)
            {
                int index = TrackList.SelectedIndex;
                _trackPaths.RemoveAt(index);
                TrackList.Items.RemoveAt(index);
                SavePlaylist();
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (TrackList.SelectedIndex >= 0)
            {
                string selectedTrack = _trackPaths[TrackList.SelectedIndex];

                if (MediaPlayer.Source == null || MediaPlayer.Source.ToString() != selectedTrack)
                {
                    MediaPlayer.Source = new Uri(selectedTrack);
                }

                MediaPlayer.Play();
                _timer.Start();

                PlayButton.Visibility = Visibility.Collapsed;
                PauseButton.Visibility = Visibility.Visible;
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer.Pause();
            _timer.Stop();

            PlayButton.Visibility = Visibility.Visible;
            PauseButton.Visibility = Visibility.Collapsed;
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

            // Змінюємо стан шафлу
            _isShuffleEnabled = !_isShuffleEnabled;

            // Оновлюємо стиль кнопки
            if (_isShuffleEnabled)
            {
                shuffleButton.Background = new System.Windows.Media.LinearGradientBrush(
                    System.Windows.Media.Color.FromRgb(0, 209, 255),
                    System.Windows.Media.Color.FromRgb(0, 128, 255),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(1, 1));
            }
            else
            {
                shuffleButton.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#22224F"));
            }

        }

        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            Button repeatButton = (Button)sender;

            _isRepeatEnabled = !_isRepeatEnabled;

            if (_isRepeatEnabled)
            {
                repeatButton.Background = new System.Windows.Media.LinearGradientBrush(
                    System.Windows.Media.Color.FromRgb(0, 209, 255),
                    System.Windows.Media.Color.FromRgb(0, 128, 255),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(1, 1));
            }
            else
            {
                repeatButton.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#22224F"));
            }
        }

        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
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
        }

        private void ProgressBar_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            MediaPlayer.Position = TimeSpan.FromSeconds(ProgressBar.Value);
            _isDraggingProgress = false;
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
            if (File.Exists(_saveFilePath))
            {
                try
                {
                    _trackPaths = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_saveFilePath)) ?? new List<string>();
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
            File.WriteAllText(_saveFilePath, JsonSerializer.Serialize(_trackPaths));
        }
    }
}