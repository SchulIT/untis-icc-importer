using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.ObjectModel;
using UntisIccImporter.Gui.Message;
using UntisIccImporter.Gui.Settings;

namespace UntisIccImporter.Gui.ViewModel
{
    public class SettingsViewModel : ViewModelBase
    {
        private bool isBusy;

        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                Set(() => IsBusy, ref isBusy, value);
                SaveCommand?.RaiseCanExecuteChanged();
            }
        }

        private string iccEndpoint;

        public string IccEndpoint
        {
            get { return iccEndpoint; }
            set { Set(() => IccEndpoint, ref iccEndpoint, value); }
        }

        private string iccToken;

        public string IccToken
        {
            get { return iccToken; }
            set { Set(() => IccToken, ref iccToken, value); }
        }

        private int? substitutionDays;

        public int? SubstitutionDays
        {
            get { return substitutionDays; }
            set { Set(() => SubstitutionDays, ref substitutionDays, value); }
        }

        private bool alwaysIncludeStudents;

        public bool AlwaysIncludeStudents
        {
            get { return alwaysIncludeStudents; }
            set { Set(() => AlwaysIncludeStudents, ref alwaysIncludeStudents, value); }
        }

        private string excludeRegExp;

        public string ExcludeRegExp
        {
            get { return excludeRegExp; }
            set { Set(() => ExcludeRegExp, ref excludeRegExp, value); }
        }

        public ObservableCollection<SubjectOverride> SubjectOverrides { get; } = new ObservableCollection<SubjectOverride>();

        #region Commands

        public RelayCommand SaveCommand { get; private set; }

        #endregion

        #region Services

        public IMessenger Messenger { get { return base.MessengerInstance; } }

        private readonly ISettingsManager settingsManager;

        #endregion

        public SettingsViewModel(ISettingsManager settingsManager, IMessenger messenger)
            : base(messenger)
        {
            this.settingsManager = settingsManager;

            SaveCommand = new RelayCommand(Save, CanSave);
        }

        private bool CanSave() => !IsBusy;

        private async void Save()
        {
            try
            {
                IsBusy = true;
                var appSettings = settingsManager.AppSettings;
                appSettings.IccEndpoint = IccEndpoint;
                appSettings.IccToken = IccToken;
                appSettings.NumberAutoSelectedDays = SubstitutionDays;
                appSettings.SubjectOverrides.Clear();
                appSettings.SubjectOverrides.AddRange(SubjectOverrides);
                appSettings.AlwaysIncludeStudents = AlwaysIncludeStudents;
                appSettings.ExcludeRegExp = ExcludeRegExp;

                await settingsManager.SaveAppSettingsAsync();

                Messenger.Send(new SettingsSavedMessage());
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage { Exception = e });
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void LoadSettings()
        {
            IccEndpoint = settingsManager.AppSettings.IccEndpoint;
            IccToken = settingsManager.AppSettings.IccToken;
            SubstitutionDays = settingsManager.AppSettings.NumberAutoSelectedDays;
            AlwaysIncludeStudents = settingsManager.AppSettings.AlwaysIncludeStudents;
            ExcludeRegExp = settingsManager.AppSettings.ExcludeRegExp;
            SubjectOverrides.Clear();

            foreach(var @override in settingsManager.AppSettings.SubjectOverrides)
            {
                SubjectOverrides.Add(@override);
            }
        }
    }
}
