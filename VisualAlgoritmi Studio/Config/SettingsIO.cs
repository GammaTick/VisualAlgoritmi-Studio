using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace VisualAlgoritmi_Studio.Config
{
    internal static class SettingsIO
    {
        private static readonly string SettingsPath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VisualAlgoritmi Studio",
                "settings.json");

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                WriteIndented = true
            };

        public static Settings Load()
        {
            if (!File.Exists(SettingsPath))
            {
                return new Settings();
            }

            try
            {
                string json = File.ReadAllText(SettingsPath);

                return JsonSerializer.Deserialize<Settings>(json)
                       ?? new Settings();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Failed to load settings: {ex.Message}");
                return new Settings();
            }
        }

        public static void Save(Settings settings)
        {
            string directory = Path.GetDirectoryName(SettingsPath)!;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(settings, JsonOptions);

            File.WriteAllText(SettingsPath, json);
        }
    }
}
