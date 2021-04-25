using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json.Linq;
using SchulIT.IccImport.Response;
using SchulIT.UntisExport;
using SchulIT.UntisExport.Model;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UntisIccImporter.Gui.Import;
using UntisIccImporter.Gui.Settings;

namespace UntisIccImporter.Gui.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private string gpnFile;

        public string GpnFile
        {
            get { return gpnFile; }
            set
            {
                Set(() => GpnFile, ref gpnFile, value);
                LoadCommand?.RaiseCanExecuteChanged();
            }
        }

        #region Element selection

        private bool isSubstitutionSyncEnabled;

        public bool IsSubstitutionSyncEnabled
        {
            get { return isSubstitutionSyncEnabled; }
            set
            {
                Set(() => IsSubstitutionSyncEnabled, ref isSubstitutionSyncEnabled, value);
                if(!IsSubstitutionSyncEnabled)
                {
                    IsDayTextsSyncEnabled = false;
                    IsAbsencesSyncEnabled = false;
                }
                SaveSettings();
            }
        }

        private bool isDayTextsSyncEnabled;

        public bool IsDayTextsSyncEnabled
        {
            get { return isDayTextsSyncEnabled; }
            set
            {
                Set(() => IsDayTextsSyncEnabled, ref isDayTextsSyncEnabled, value);
                SaveSettings();
            }
        }

        private bool isAbsencesSyncEnabled;

        public bool IsAbsencesSyncEnabled
        {
            get { return isAbsencesSyncEnabled; }
            set
            {
                Set(() => IsAbsencesSyncEnabled, ref isAbsencesSyncEnabled, value);
                SaveSettings();
            }
        }

        private bool isExamsSyncEnabled;

        public bool IsExamsSyncEnabled
        {
            get { return isExamsSyncEnabled; }
            set
            {
                Set(() => IsExamsSyncEnabled, ref isExamsSyncEnabled, value);
                SaveSettings();
            }
        }

        private bool isTimetableSyncEnabled;

        public bool IsTimetableSyncEnabled
        {
            get { return isTimetableSyncEnabled; }
            set
            {
                Set(() => IsTimetableSyncEnabled, ref isTimetableSyncEnabled, value);

                SaveSettings();
            }
        }

        private bool isSupervisionsSyncEnabled;

        public bool IsSupervisionSyncEnabled
        {
            get { return isSupervisionsSyncEnabled; }
            set
            {
                Set(() => IsSupervisionSyncEnabled, ref isSupervisionsSyncEnabled, value);
                SaveSettings();
            }
        }

        private bool isRoomsSyncEnabled;

        public bool IsRoomsSyncEnabled
        {
            get { return isRoomsSyncEnabled; }
            set
            {
                Set(() => IsRoomsSyncEnabled, ref isRoomsSyncEnabled, value);
                SaveSettings();
            }
        }

        #endregion

        #region Date selection

        private DateTime? substitutionStart;

        public DateTime? SubstitutionStart
        {
            get { return substitutionStart; }
            set
            {
                Set(() => SubstitutionStart, ref substitutionStart, value);
            }
        }

        private DateTime? substitutionEnd;

        public DateTime? SubstitutionEnd
        {
            get { return substitutionEnd; }
            set
            {
                Set(() => SubstitutionEnd, ref substitutionEnd, value);
            }
        }

        private DateTime? examStart;

        public DateTime? ExamStart
        {
            get { return examStart; }
            set
            {
                Set(() => ExamStart, ref examStart, value);
                SaveSettings();
            }
        }

        private DateTime? examEnd;

        public DateTime? ExamEnd
        {
            get { return examEnd; }
            set
            {
                Set(() => ExamEnd, ref examEnd, value);
                SaveSettings();
            }
        }

        #endregion

        #region State

        private bool isBusy;

        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                Set(() => IsBusy, ref isBusy, value);
                LoadCommand?.RaiseCanExecuteChanged();
                UploadCommand?.RaiseCanExecuteChanged();
            }
        }

        private string busyText;

        public string BusyText
        {
            get { return busyText; }
            set { Set(() => BusyText, ref busyText, value); }
        }

        public ObservableCollection<Period> Periods { get; } = new ObservableCollection<Period>();

        public ObservableCollection<Period> SelectedPeriods { get; } = new ObservableCollection<Period>();

        private UntisExportResult result;

        public UntisExportResult Result
        {
            get { return result; }
            set
            {
                Set(() => Result, ref result, value);
                UploadCommand?.RaiseCanExecuteChanged();
            }
        }

        private DateTime? gpnLoadDateTime;

        public DateTime? GpnLoadDateTime
        {
            get { return gpnLoadDateTime; }
            set { Set(() => GpnLoadDateTime, ref gpnLoadDateTime, value); }
        }

        public ObservableCollection<ImportOutput> Output { get; } = new ObservableCollection<ImportOutput>();

        #endregion

        #region Commands

        public RelayCommand LoadCommand { get; private set; }

        public RelayCommand<bool> UploadCommand { get; private set; }

        #endregion

        private readonly IImporter importer;
        private readonly IDialogCoordinator dialogCoordinator;

        private bool preventSaveSettings = false;

        #region Services

        private ISettingsManager settingsManager;
        
        public IMessenger Messenger { get { return base.MessengerInstance; } }

        #endregion

        public MainViewModel(ISettingsManager settingsManager, IImporter importer, IDialogCoordinator dialogCoordinator, IMessenger messenger)
            : base(messenger)
        {
            this.settingsManager = settingsManager;
            this.importer = importer;
            this.dialogCoordinator = dialogCoordinator;

            UploadCommand = new RelayCommand<bool>(Upload, CanUpload);
            LoadCommand = new RelayCommand(LoadGpnAsync, CanLoadGpn);
        }

        private async Task<bool> HandleResultAsync(ImportResult importResult, string section)
        {
            if(importResult.WasSuccessful == true)
            {
                Output.Add(
                    new ImportOutput
                    {
                        Section = section,
                        Date = DateTime.Now,
                        Output = importResult.Text
                    }
                );
                return true;
            }

            var errorResponse = importResult.Response as ErrorResponse;
            var errorMessage = "Unbekannter Fehler";
            if(errorResponse != null)
            {
                errorMessage = $"HTTP {errorResponse.ResponseCode}: " + errorResponse.Message;

                try
                {
                    var jsonResponse = JObject.Parse(errorResponse.ResponseBody);
                    if (jsonResponse.ContainsKey("type"))
                    {
                        errorMessage += $"\nException-Typ: {jsonResponse["type"]}";
                    }

                    if(jsonResponse.ContainsKey("violations"))
                    {
                        var violations = jsonResponse["violations"];

                        foreach(var violation in violations)
                        {
                            errorMessage += $"\n* {violation["property"]}: {violation["message"]}";
                        }
                    }
                }
                catch { }
            }

            var result = await dialogCoordinator.ShowMessageAsync(this, "Fehler", errorMessage, MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { NegativeButtonText = "Import Abbrechen" });
            return result == MessageDialogResult.Affirmative;
        }

        private async void Upload(bool suppressNotifications)
        {
            try
            {
                Output.Clear();

                IsBusy = true;

                if (IsRoomsSyncEnabled)
                {
                    BusyText = "Importiere Räume...";
                    var result = await importer.ImportRoomsAsync(Result);

                    if (await HandleResultAsync(result, "Räume") == false)
                    {
                        return;
                    }
                }

                if (IsDayTextsSyncEnabled)
                {
                    BusyText = "Importiere Tagestexte...";
                    var result = await importer.ImportDayTextsAsync(Result, SubstitutionStart, SubstitutionEnd);

                    if (await HandleResultAsync(result, "Tagestexte") == false)
                    {
                        return;
                    }
                }

                if (IsDayTextsSyncEnabled)
                {
                    BusyText = "Importiere unterrichtsfreie Stunden...";
                    var result = await importer.ImportFreeLessonsAsync(Result, SubstitutionStart, SubstitutionEnd);

                    if (await HandleResultAsync(result, "Unterrichtsfrei") == false)
                    {
                        return;
                    }
                }

                if (IsAbsencesSyncEnabled)
                {
                    BusyText = "Importiere Absenzen...";
                    var result = await importer.ImportAbsencesAsync(Result, SubstitutionStart, SubstitutionEnd);

                    if (await HandleResultAsync(result, "Absenzen") == false)
                    {
                        return;
                    }
                }

                if (IsSubstitutionSyncEnabled)
                {
                    BusyText = "Importiere Vertretungen...";
                    var result = await importer.ImportSubstitutionsAsync(Result, SubstitutionStart, SubstitutionEnd, suppressNotifications);

                    if (await HandleResultAsync(result, "Vertretungen") == false)
                    {
                        return;
                    }
                }

                if (IsTimetableSyncEnabled || IsSupervisionSyncEnabled)
                {
                    BusyText = "Importiere Perioden...";
                    var result = await importer.ImportPeriodsAsync(Result, SelectedPeriods);

                    if (await HandleResultAsync(result, "Perioden") == false)
                    {
                        return;
                    }
                }

                if (IsTimetableSyncEnabled)
                {
                    foreach (var period in SelectedPeriods)
                    {
                        BusyText = $"Importiere Stundenplan ({period.LongName})...";
                        var result = await importer.ImportTimetableAsync(Result, period);

                        if (await HandleResultAsync(result, $"Stundenplan ({period.LongName})") == false)
                        {
                            return;
                        }
                    }
                }

                if (IsSupervisionSyncEnabled)
                {
                    foreach (var period in SelectedPeriods)
                    {
                        BusyText = $"Importiere Aufsichten ({period.LongName})...";
                        var result = await importer.ImportSupervisionsAsync(Result, period);

                        if (await HandleResultAsync(result, $"Aufsichten ({period.LongName})") == false)
                        {
                            return;
                        }
                    }
                }

                if (IsExamsSyncEnabled)
                {
                    BusyText = "Importiere Klausuren...";
                    var result = await importer.ImportExamsAsync(Result, ExamStart, ExamEnd, suppressNotifications);

                    if (await HandleResultAsync(result, "Klausuren") == false)
                    {
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                await dialogCoordinator.ShowMessageAsync(this, "Oups, das hätte nicht passieren dürfen...", "Ein unerwarteter Fehler ist aufgetreten. Details: " + e.Message, MessageDialogStyle.Affirmative);
            }
            finally
            {
                IsBusy = false;
                BusyText = null;
            }
        }

        private bool CanUpload(bool suppressNotifications) => Result != null && !IsBusy;

        public void RestoreSettings()
        {
            preventSaveSettings = true;

            GpnFile = settingsManager.UserSettings.GpnFile;
            IsSubstitutionSyncEnabled = settingsManager.UserSettings.IsSubstitutionSyncEnabled;
            IsDayTextsSyncEnabled = settingsManager.UserSettings.IsDayTextsSyncEnabled;
            IsAbsencesSyncEnabled = settingsManager.UserSettings.IsAbsencesSyncEnabled;
            IsExamsSyncEnabled = settingsManager.UserSettings.IsExamsSyncEnabled;
            IsTimetableSyncEnabled = settingsManager.UserSettings.IsTimetableSyncEnabled;
            IsSupervisionSyncEnabled = settingsManager.UserSettings.IsSupervisionSyncEnabled;
            IsRoomsSyncEnabled = settingsManager.UserSettings.IsRoomsSyncEnabled;
            ExamStart = settingsManager.UserSettings.ExamStart;
            ExamEnd = settingsManager.UserSettings.ExamEnd;

            preventSaveSettings = false;
        }

        public void Prepare()
        {
            if(settingsManager.AppSettings.NumberAutoSelectedDays != null && settingsManager.AppSettings.NumberAutoSelectedDays > 0)
            {
                SubstitutionStart = DateTime.Today;
                SubstitutionEnd = DateTime.Today.AddDays(settingsManager.AppSettings.NumberAutoSelectedDays.Value);
            }
        }

        private bool CanLoadGpn() => !IsBusy && !string.IsNullOrEmpty(GpnFile);

        public async void LoadGpnAsync()
        {
            if (string.IsNullOrEmpty(GpnFile))
            {
                return;
            }

            try
            {
                GpnLoadDateTime = null;
                IsBusy = true;
                Periods.Clear();

                BusyText = "Untis Datei laden...";
                Result = await UntisExporter.ParseFileAsync(GpnFile);

                foreach(var period in Result.Periods)
                {
                    Periods.Add(period);
                }

                GpnLoadDateTime = DateTime.Now;
            }
            catch (Exception e)
            {
                await dialogCoordinator.ShowMessageAsync(this, "Fehler beim Laden der Untis-Datei", e.Message, MessageDialogStyle.Affirmative);
                Result = null;
            }
            finally
            {
                IsBusy = false;
                BusyText = "";
            }
        }

        private void SaveSettings()
        {
            if(preventSaveSettings)
            {
                return;
            }

            settingsManager.UserSettings.GpnFile = GpnFile;
            settingsManager.UserSettings.IsSubstitutionSyncEnabled = IsSubstitutionSyncEnabled;
            settingsManager.UserSettings.IsDayTextsSyncEnabled = IsDayTextsSyncEnabled;
            settingsManager.UserSettings.IsAbsencesSyncEnabled = IsAbsencesSyncEnabled;
            settingsManager.UserSettings.IsExamsSyncEnabled = IsExamsSyncEnabled;
            settingsManager.UserSettings.IsTimetableSyncEnabled = IsTimetableSyncEnabled;
            settingsManager.UserSettings.IsSupervisionSyncEnabled = IsSupervisionSyncEnabled;
            settingsManager.UserSettings.IsRoomsSyncEnabled = IsRoomsSyncEnabled;
            settingsManager.UserSettings.ExamStart = ExamStart;
            settingsManager.UserSettings.ExamEnd = ExamEnd;

            settingsManager.SaveUserSettingsAsync();
        }

        public class ImportOutput
        {
            public DateTime Date { get; set; }

            public string Section { get; set; }

            public string Output { get; set; }
        }
    }
}
