using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using UntisIccImporter.Gui.ViewModel;

namespace UntisIccImporter.Gui.View
{
    public partial class MainView : MetroWindow
    {
        public MainView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;

            viewModel.RestoreSettings();
            viewModel.Prepare();
            viewModel.LoadGpnAsync();
        }

        private void OnGithubButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            var locator = App.Current.Resources["ViewModelLocator"] as ViewModelLocator;
            var projectUrl = locator.About.ProjectUrl;

            Process.Start(new ProcessStartInfo(projectUrl) { UseShellExecute = true });
            e.Handled = true;
        }

        private void OnRequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void OnChooseFileClick(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Untis GPN Datei (*.gpn)|*.gpn"
            };
            var result = dialog.ShowDialog();

            if(result == true)
            {
                var viewModel = DataContext as MainViewModel;
                viewModel.GpnFile = dialog.FileName;
            }
        }

        private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            settingsFlyout.IsOpen = true;
        }

        private void OnAboutButtonClick(object sender, RoutedEventArgs e)
        {
            aboutFlyout.IsOpen = true;
        }
    }
}
