using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MifareOneTool.Core.Services
{
    public class AppSettings
    {
        public bool AutoABN { get; set; } = true;
        public bool WriteCheck { get; set; } = true;
        public bool AutoCheck { get; set; } = false;
        public bool ShowUID { get; set; } = false;
        public string Language { get; set; } = "";
        public string LastKeyFile { get; set; } = "";
        public string LastDumpFile { get; set; } = "";
        public bool CLIMode { get; set; } = false;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        private static string SettingsPath =>
            Path.Combine(AppContext.BaseDirectory, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json, _jsonOpts) ?? new AppSettings();
                }
            }
            catch { /* fall through to default */ }
            return new AppSettings();
        }

        public void Save()
        {
            string json = JsonSerializer.Serialize(this, _jsonOpts);
            File.WriteAllText(SettingsPath, json);
        }
    }
}
