using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Wave_Player
{
    public partial class SettingsWindow : Window
    {
        private SettingsC _settings;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            _settings = SettingsC.Load();
            DefaultVolumeSlider.Value = _settings.DefaultVolume;
            CrossfadeSlider.Value = _settings.CrossfadeDuration;
            AutoPlayCheckBox.IsChecked = _settings.AutoPlayEnabled;
            RememberTrackCheckBox.IsChecked = _settings.RememberLastTrack;
            NotificationsCheckBox.IsChecked = _settings.ShowNotifications;
            MusicFolderTextBox.Text = _settings.DefaultMusicFolder;
        }

        private void SaveSettings()
        {
            _settings.DefaultVolume = DefaultVolumeSlider.Value;
            _settings.CrossfadeDuration = CrossfadeSlider.Value;
            _settings.AutoPlayEnabled = AutoPlayCheckBox.IsChecked ?? false;
            _settings.RememberLastTrack = RememberTrackCheckBox.IsChecked ?? false;
            _settings.ShowNotifications = NotificationsCheckBox.IsChecked ?? false;
            _settings.DefaultMusicFolder = MusicFolderTextBox.Text;
            _settings.Save();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select Default Music Folder"
            };

            if (!string.IsNullOrEmpty(MusicFolderTextBox.Text))
            {
                dialog.InitialDirectory = MusicFolderTextBox.Text;
            }

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                MusicFolderTextBox.Text = dialog.FileName;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
    }
}