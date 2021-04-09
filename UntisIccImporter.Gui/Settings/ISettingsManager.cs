using System.Threading.Tasks;

namespace UntisIccImporter.Gui.Settings
{
    public interface ISettingsManager
    {
        public AppSettings AppSettings { get; }

        public UserSettings UserSettings { get; }

        public void LoadSettings();

        public Task SaveAppSettingsAsync();

        public Task SaveUserSettingsAsync();
    }
}
