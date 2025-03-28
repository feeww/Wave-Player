using System.Windows;
using System.Windows.Media;
using Wave_Player.classes;

namespace Wave_Player
{
    public partial class App : Application
    {
        private SettingsC _settings;
        protected override void OnStartup(StartupEventArgs e)
        {
            SettingsC settings = SettingsC.Load();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            SaveSettings();
        }

        private void LoadSettings()
        {
            _settings = SettingsC.Load();
        }

        private void SaveSettings()
        {
            _settings?.Save();
        }

        public void ApplyThemeColors(string primaryColor, string secondaryColor)
        {
            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            gradientBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(primaryColor), 0));
            gradientBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(secondaryColor), 1));

            Resources["ThemeGradient"] = gradientBrush;
        }

    }
}
