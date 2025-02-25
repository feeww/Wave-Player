using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using Wave_Player.classes;
using Button = System.Windows.Controls.Button;
using System.Windows.Controls;
using Xceed.Wpf.Toolkit;

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


            PrimaryColorTextBox.Text = _settings.Theme.PrimaryColor;
            SecondaryColorTextBox.Text = _settings.Theme.SecondaryColor;

            ApplyThemeColors();
        }

        private void LoadSettings()
        {
            _settings = SettingsC.Load();
            DefaultVolumeSlider.Value = _settings.DefaultVolume;
            CrossfadeSlider.Value = _settings.CrossfadeDuration;
            AutoPlayCheckBox.IsChecked = _settings.AutoPlayEnabled;
            RememberTrackCheckBox.IsChecked = _settings.RememberLastTrack;
            AutoShuffleCheckBox.IsChecked = _settings.AutoShuffle;
            NotificationsCheckBox.IsChecked = _settings.ShowNotifications;
            MusicFolderTextBox.Text = _settings.DefaultMusicFolder;
        }

        private void SaveSettings()
        {
            _settings.DefaultVolume = DefaultVolumeSlider.Value;
            _settings.CrossfadeDuration = CrossfadeSlider.Value;
            _settings.AutoPlayEnabled = AutoPlayCheckBox.IsChecked ?? false;
            _settings.RememberLastTrack = RememberTrackCheckBox.IsChecked ?? false;
            _settings.AutoShuffle = AutoShuffleCheckBox.IsChecked ?? false;
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
            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            gradientBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(_settings.Theme.PrimaryColor), 0));
            gradientBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(_settings.Theme.SecondaryColor), 1));

            var modernSliderStyle = FindResource("ModernSlider") as Style;
            if (modernSliderStyle != null)
            {
                var newStyle = new Style(typeof(Slider), modernSliderStyle);
                foreach (var setter in modernSliderStyle.Setters)
                {
                    if (setter is Setter setterObj && setterObj.Property == Slider.ForegroundProperty)
                    {
                        newStyle.Setters.Add(new Setter(Slider.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(_settings.Theme.PrimaryColor))));
                    }
                    else
                    {
                        newStyle.Setters.Add(setter);
                    }
                }
                foreach (var slider in FindVisualChildren<Slider>(this))
                {
                    slider.Style = newStyle;
                }
            }

            System.Windows.Application.Current.Resources["ThemeGradient"] = gradientBrush;
            System.Windows.Application.Current.Resources["ThemePrimaryColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_settings.Theme.PrimaryColor));
            System.Windows.Application.Current.Resources["ThemeSecondaryColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_settings.Theme.SecondaryColor));
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
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
            string primaryColor = PrimaryColorTextBox.Text;
            string secondaryColor = SecondaryColorTextBox.Text;

            if (!string.IsNullOrEmpty(primaryColor) && !IsValidColor(primaryColor))
            {
                NotificationSystem.Show("Please select a valid primary color.", NotificationType.Error);
                return;
            }

            if (!string.IsNullOrEmpty(secondaryColor) && !IsValidColor(secondaryColor))
            {
                NotificationSystem.Show("Please select a valid secondary color.", NotificationType.Error);
                return;
            }

            if (!string.IsNullOrEmpty(primaryColor))
            {
                _settings.Theme.PrimaryColor = primaryColor;
            }

            if (!string.IsNullOrEmpty(secondaryColor))
            {
                _settings.Theme.SecondaryColor = secondaryColor;
            }

            if (NotificationsCheckBox.IsChecked == true)
                {NotificationSystem.Show("Settings applied successfully!", NotificationType.Success);}
            
            SaveSettings();

            var mainWindow = (MainWindow)Owner;
            mainWindow.UpdateThemeColors(_settings.Theme.PrimaryColor, _settings.Theme.SecondaryColor);

            DialogResult = true;
            Close();
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