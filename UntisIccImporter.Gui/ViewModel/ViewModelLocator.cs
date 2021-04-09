using Autofac;
using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.Controls.Dialogs;
using SchulIT.IccImport;
using UntisIccImporter.Gui.Import;
using UntisIccImporter.Gui.Settings;

namespace UntisIccImporter.Gui.ViewModel
{
    public class ViewModelLocator
    {
        private static IContainer container;

        static ViewModelLocator()
        {
            RegisterServices();
        }

        public static void RegisterServices()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<IccImporter>().As<IIccImporter>().SingleInstance();
            builder.RegisterType<Importer>().As<IImporter>().SingleInstance();
            builder.Register(x => DialogCoordinator.Instance).As<IDialogCoordinator>().SingleInstance();

            builder.RegisterType<MainViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<SettingsViewModel>().AsSelf().SingleInstance().OnActivated(
                x =>
                {
                    x.Instance.LoadSettings();
                });
            builder.RegisterType<AboutViewModel>().AsSelf().SingleInstance();

            builder.RegisterType<SettingsManager>().As<ISettingsManager>().SingleInstance().OnActivated(
                x =>
                {
                    x.Instance.LoadSettings();
                });
            builder.RegisterType<Messenger>().As<IMessenger>().SingleInstance();

            container = builder.Build();
        }

        public IMessenger Messenger { get { return container.Resolve<IMessenger>(); } }

        public MainViewModel Main { get { return container.Resolve<MainViewModel>(); } }

        public SettingsViewModel Settings { get { return container.Resolve<SettingsViewModel>(); } }

        public AboutViewModel About { get { return container.Resolve<AboutViewModel>(); } }

    }
}
