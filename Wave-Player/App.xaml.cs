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
        }

        private void SaveSettings()
        {
            Settings.Default.Save();
        }
    }
}
