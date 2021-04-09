using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace UntisIccImporter.Gui.Settings
{
    public class SettingsManager : ISettingsManager
    {
        public const string JsonFileName = "settings.json";
        public const string ApplicationName = "UntisIccImporter";
        public const string ApplicationVendor = "SchulIT";

        public AppSettings AppSettings { get; private set; }

        public UserSettings UserSettings { get; private set; }

        public void LoadSettings()
        {
            LoadSettings<AppSettings>(GetAppSettingsPath(), settings => AppSettings = settings);
            LoadSettings<UserSettings>(GetUserSettingsPath(), settings => UserSettings = settings);
        }

        private void LoadSettings<T>(string path, Action<T> callback)
            where T : new()
        {
            var settings = new T();

            if (File.Exists(path))
            {
                using (var stream = new FileStream(path, FileMode.Open))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var json = reader.ReadToEnd();
                        JsonConvert.PopulateObject(json, settings);
                    }
                }
            }
            
            callback(settings);
        }

        public Task SaveAppSettingsAsync() => SaveSettings(GetAppSettingsPath(), AppSettings);

        public Task SaveUserSettingsAsync() => SaveSettings(GetUserSettingsPath(), UserSettings);

        private async Task SaveSettings<T>(string path, T settings)
        {
            await Task.Run(() =>
            {
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            });

            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            await File.WriteAllTextAsync(path, json, Encoding.UTF8);
        }

        protected virtual string GetAppSettingsPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                ApplicationVendor,
                ApplicationName,
                JsonFileName
            );
        }
        protected virtual string GetUserSettingsPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ApplicationVendor,
                ApplicationName,
                JsonFileName
            );
        }
    }
}
