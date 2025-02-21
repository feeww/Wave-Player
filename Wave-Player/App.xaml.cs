using System.Windows;
using System.Windows.Media;

namespace Wave_Player
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LoadSettings();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            SaveSettings();
        }

        private void LoadSettings()
        {
        }

        private void SaveSettings()
        {
            Settings.Default.Save();
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
