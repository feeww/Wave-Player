using System;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace Wave_Player.classes
{
    public class SettingsC
    {
        public double DefaultVolume { get; set; } = 0.5;
        public double CrossfadeDuration { get; set; } = 2;
        public bool AutoPlayEnabled { get; set; } = false;
        public bool RememberLastTrack { get; set; } = true;
        public bool AutoShuffle { get; set; } = false;
        public bool ShowNotifications { get; set; } = true;
        public string DefaultMusicFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        public ThemeSettings Theme { get; set; } = new ThemeSettings();
        public string AlbumCoverImagePath { get; set; }


        private static readonly string SettingsFilePath = "settings.json";

        public static SettingsC Load()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<SettingsC>(json) ?? new SettingsC();

                    if (!IsValidColor(settings.Theme.PrimaryColor))
                    {
                        settings.Theme.PrimaryColor = "#00D1FF";
                    }
                    if (!IsValidColor(settings.Theme.SecondaryColor))
                    {
                        settings.Theme.SecondaryColor = "#0080FF";
                    }

                    return settings;
                }
                catch (Exception)
                {
                    return new SettingsC();
                }
            }
            return new SettingsC();
        }

        private static bool IsValidColor(string color)
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


        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}
