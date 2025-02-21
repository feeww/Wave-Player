using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using Wave_Player.classes;
using Button = System.Windows.Controls.Button;

namespace Wave_Player
{
    public partial class SettingsWindow : Window
    {
        private SettingsC _settings;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();

            UpdateColorPickerBackground(PrimaryColorPicker, _settings.Theme.PrimaryColor);
            UpdateColorPickerBackground(SecondaryColorPicker, _settings.Theme.SecondaryColor);
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

        private void ColorPicker_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var dialog = new System.Windows.Forms.ColorDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var colorHex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";

                if (button == PrimaryColorPicker)
                {
                    _settings.Theme.PrimaryColor = colorHex;
                    PrimaryColorTextBox.Text = colorHex;
                }
                else
                {
                    _settings.Theme.SecondaryColor = colorHex;
                    SecondaryColorTextBox.Text = colorHex;
                }

                UpdateColorPickerBackground(button, colorHex);
                ApplyThemeColors();
            }
        }

        private void UpdateColorPickerBackground(Button button, string colorHex)
        {
            button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
        }

        private void ApplyThemeColors()
        {
            var app = System.Windows.Application.Current;

            // Create gradient brush with selected colors
            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            gradientBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(_settings.Theme.PrimaryColor), 0));
            gradientBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(_settings.Theme.SecondaryColor), 1));

            // Update resources
            app.Resources["ThemeGradient"] = gradientBrush;
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