using Newtonsoft.Json;
using System;

namespace UntisIccImporter.Gui.Settings
{
    public class UserSettings
    {
        [JsonProperty]
        public string GpnFile { get; set; }

        [JsonProperty]
        public bool IsSubstitutionSyncEnabled { get; set; }

        [JsonProperty]
        public bool IsDayTextsSyncEnabled { get; set; }

        [JsonProperty]
        public bool IsAbsencesSyncEnabled { get; set; }

        [JsonProperty]
        public bool IsExamsSyncEnabled { get; set; }

        [JsonProperty]
        public bool IsTimetableSyncEnabled { get; set; }

        [JsonProperty]
        public bool IsSupervisionSyncEnabled { get; set; }

        [JsonProperty]
        public bool IsRoomsSyncEnabled { get; set; }

        [JsonProperty]
        public DateTime? ExamStart { get; set; }

        [JsonProperty]
        public DateTime? ExamEnd { get; set; }
    }
}
