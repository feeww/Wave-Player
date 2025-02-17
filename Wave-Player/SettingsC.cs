using System;
using System.IO;
using System.Text.Json;

namespace Wave_Player
{
    public class SettingsC
    {
        public double DefaultVolume { get; set; } = 0.5;
        public double CrossfadeDuration { get; set; } = 2;
        public bool AutoPlayEnabled { get; set; } = false;
        public bool RememberLastTrack { get; set; } = true;
        public bool ShowNotifications { get; set; } = true;
        public string DefaultMusicFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

        private static readonly string SettingsFilePath = "settings.json";

        public static SettingsC Load()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<SettingsC>(json) ?? new SettingsC();
                }
                catch (Exception)
                {
                    return new SettingsC();
                }
            }
            return new SettingsC();
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                // Handle exceptions if needed
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}
