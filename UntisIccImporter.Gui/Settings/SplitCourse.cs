using Newtonsoft.Json;

namespace UntisIccImporter.Gui.Settings
{
    public class SplitCourse
    {
        [JsonProperty]
        public string Subject { get; set; }

        [JsonProperty]
        public string Grade { get; set; }
    }
}
