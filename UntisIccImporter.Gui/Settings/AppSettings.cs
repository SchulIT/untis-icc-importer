using Newtonsoft.Json;
using System.Collections.Generic;

namespace UntisIccImporter.Gui.Settings
{
    public class AppSettings
    {
        [JsonProperty]
        public string IccEndpoint { get; set; }

        [JsonProperty]
        public string IccToken { get; set; }

        [JsonProperty]
        public int? NumberAutoSelectedDays { get; set; } = 7;

        [JsonProperty]
        public bool AlwaysIncludeStudents { get; set; } = true;

        [JsonProperty]
        public string ExcludeRegExp { get; set; } = null;

        [JsonProperty]
        public List<SubjectOverride> SubjectOverrides { get; } = new List<SubjectOverride>();

        [JsonProperty]
        public List<SplitCourse> SplitCourses { get; } = new List<SplitCourse>();
    }
}
