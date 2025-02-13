using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
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

        public MainWindow()
        {
            InitializeComponent();
            LoadPlaylist();
            MediaPlayer.Volume = VolumeSlider.Value;

            // Таймер для оновлення прогресбару
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += UpdateProgress;
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "MP3 Files (*.mp3)|*.mp3"
            };

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

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (MediaPlayer.Source == null && TrackList.SelectedIndex >= 0)
            {
                MediaPlayer.Source = new Uri(_trackPaths[TrackList.SelectedIndex]);
                MediaPlayer.Play();
                _timer.Start();
            }
            else
            {
                MediaPlayer.Play();
                _timer.Start();
            }

            PlayButton.Visibility = Visibility.Collapsed;
            PauseButton.Visibility = Visibility.Visible;
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer.Pause();
            _timer.Stop();

            PlayButton.Visibility = Visibility.Visible;
            PauseButton.Visibility = Visibility.Collapsed;
        }


        private void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            if (_trackPaths.Count > 1)
            {
                int randomIndex = _random.Next(0, _trackPaths.Count);
                TrackList.SelectedIndex = randomIndex;
                Play_Click(null, null);
            }
        }

        private bool _previousClickedOnce = false;

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (TrackList.SelectedIndex >= 0)
            {
                if (MediaPlayer.Position.TotalSeconds >= 5 && !_previousClickedOnce)
                {
                    // Якщо трек грав більше 5 секунд - спочатку обнуляємо його
                    MediaPlayer.Position = TimeSpan.Zero;
                    _previousClickedOnce = true;
                }
                else if (TrackList.SelectedIndex > 0)
                {
                    // Якщо натиснули вдруге або трек був менше 5 секунд – переходимо до попереднього
                    TrackList.SelectedIndex--;
                    Play_Click(null, null);
                    _previousClickedOnce = false;
                }
                else
                {
                    // Якщо це перший трек у списку - просто перемотуємо його
                    MediaPlayer.Position = TimeSpan.Zero;
                }
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (TrackList.SelectedIndex < _trackPaths.Count - 1)
            {
                TrackList.SelectedIndex++;
                Play_Click(null, null);
            }
        }

        private void TrackList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TrackList.SelectedIndex >= 0)
            {
                MediaPlayer.Source = new Uri(_trackPaths[TrackList.SelectedIndex]);
                MediaPlayer.Play();
                _timer.Start();
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MediaPlayer != null)
            {
                MediaPlayer.Volume = VolumeSlider.Value;
            }
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isDraggingProgress)
            {
                MediaPlayer.Position = TimeSpan.FromSeconds(ProgressBar.Value);
            }
        }

        private void ProgressBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            double position = e.GetPosition(ProgressBar).X / ProgressBar.ActualWidth;
            MediaPlayer.Position = TimeSpan.FromSeconds(ProgressBar.Maximum * position);
        }

        private void DeleteTrack_Click(object sender, RoutedEventArgs e)
        {
            if (TrackList.SelectedIndex != -1)
            {
                int index = TrackList.SelectedIndex;
                _trackPaths.RemoveAt(index);
                TrackList.Items.RemoveAt(index);

                // Якщо після видалення список порожній - зупиняємо плеєр
                if (TrackList.Items.Count == 0)
                {
                    MediaPlayer.Stop();
                }
            }
        }


        private void SavePlaylist()
        {
            File.WriteAllText(_saveFilePath, JsonSerializer.Serialize(_trackPaths));
        }

        private void LoadPlaylist()
        {
            if (TrackList == null) return; // Запобігає NullReferenceException

            if (File.Exists(_saveFilePath))
            {
                _trackPaths = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_saveFilePath)) ?? new List<string>();
                foreach (string path in _trackPaths)
                {
                    if (File.Exists(path)) // Перевіряємо, чи файл існує
                        TrackList.Items.Add(System.IO.Path.GetFileName(path));
                }
            }
        }

        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            PauseButton.Visibility = Visibility.Collapsed;
            PlayButton.Visibility = Visibility.Visible;
            // Якщо це не останній трек у списку – переключаємось на наступний
            if (TrackList.SelectedIndex < _trackPaths.Count - 1)
            {
                TrackList.SelectedIndex++;
                Play_Click(null, null);
            }
            else
            {
                // Якщо це останній трек – можна або зупинити, або почати спочатку
                TrackList.SelectedIndex = 0;
                Play_Click(null, null);
            }
        }

        private void UpdateProgress(object sender, EventArgs e)
        {
            if (!_isDraggingProgress && MediaPlayer.NaturalDuration.HasTimeSpan)
            {
                ProgressBar.Maximum = MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                ProgressBar.Value = MediaPlayer.Position.TotalSeconds;

                CurrentTime.Text = TimeSpan.FromSeconds(ProgressBar.Value).ToString(@"m\:ss");
                TotalTime.Text = MediaPlayer.NaturalDuration.TimeSpan.ToString(@"m\:ss");
            }
        }

    }
}
