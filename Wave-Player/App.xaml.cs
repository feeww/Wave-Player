using System.Windows;

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
            // Load settings if needed, this is optional if settings are loaded in the MainWindow or SettingsWindow
        }

        private void SaveSettings()
        {
            Settings.Default.Save();
        }
    }
}
