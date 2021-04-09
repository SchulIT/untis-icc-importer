using Newtonsoft.Json;

namespace UntisIccImporter.Gui.Settings
{
    public class SubjectOverride
    {
        [JsonProperty]
        public string UntisSubject { get; set; }

        [JsonProperty]
        public string NewSubject { get; set; }
    }
}
