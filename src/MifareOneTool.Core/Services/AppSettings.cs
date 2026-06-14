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

        public static AppSettings Load() => LoadFrom(SettingsPath);

        public static AppSettings LoadFrom(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<AppSettings>(json, _jsonOpts) ?? new AppSettings();
                }
            }
            catch { /* fall through to default */ }
            return new AppSettings();
        }

        public void Save() => SaveTo(SettingsPath);

        public void SaveTo(string path)
        {
            string json = JsonSerializer.Serialize(this, _jsonOpts);
            File.WriteAllText(path, json);
        }
    }
}
