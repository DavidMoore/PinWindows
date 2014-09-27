namespace PinWindows
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Deployment.Application;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.Win32;
    using Properties;

    class ViewModel
    {
        public ViewModel()
        {
            Windows = new ObservableCollection<WindowModel>();
            
            Refresh = new DelegateCommand(o => EnumerateWindows());

            EnumerateWindows();

            CheckForUpdates();
        }

        void CheckForUpdates()
        {
            if (!ApplicationDeployment.IsNetworkDeployed) return;

            ApplicationDeployment deployment = ApplicationDeployment.CurrentDeployment;

            UpdateCheckInfo info;

            try
            {
                info = deployment.CheckForDetailedUpdate();
            }
            catch (DeploymentDownloadException dde)
            {
                MessageBox.Show("The new version of the application cannot be downloaded at this time. \n\nPlease check your network connection, or try again later. Error: " + dde.Message);
                return;
            }
            catch (InvalidDeploymentException ide)
            {
                MessageBox.Show("Cannot check for a new version of the application. The ClickOnce deployment is corrupt. Please redeploy the application and try again. Error: " + ide.Message);
                return;
            }
            catch (InvalidOperationException ioe)
            {
                MessageBox.Show("This application cannot be updated. It is likely not a ClickOnce application. Error: " + ioe.Message);
                return;
            }

            if (!info.UpdateAvailable) return;

            if (!info.IsUpdateRequired)
            {
                var result = MessageBox.Show("An update is available. Would you like to update the application now?", "Update Available", MessageBoxButton.YesNo);
                if (MessageBoxResult.Yes != result) return;
            }
            else
            {
                var result = MessageBox.Show("This application has detected a mandatory update from your current " +
                                "version to version " + info.MinimumRequiredVersion +
                                ". The application will now install the update and restart.",
                    "Update Available", MessageBoxButton.OKCancel,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Cancel) return;
            }

            try
            {
                deployment.Update();
                MessageBox.Show("The application has been upgraded, and will now restart.");
                System.Windows.Forms.Application.Restart();
            }
            catch (DeploymentDownloadException dde)
            {
                MessageBox.Show("Cannot install the latest version of the application. \n\nPlease check your network connection, or try again later. Error: " + dde);
            }
        }

        public ICommand Refresh { get; private set; }

        void EnumerateWindows()
        {
            var results = new List<WindowModel>();

            foreach (var process in Process.GetProcesses()
                .Where(x => x.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(x.MainWindowTitle))
                .OrderBy(x => x.MainWindowTitle))
            {
                var window = new WindowModel();
                window.Handle = process.MainWindowHandle;
                window.Title = process.MainWindowTitle;
                window.IsPinned = AlwaysOnTop.IsWindowTopMost(window.Handle);
                results.Add(window);
            }

            Windows.Clear();
            results.ForEach(model => Windows.Add(model));
        }

        public ObservableCollection<WindowModel> Windows { get; private set; }

        public bool StartupWithWindows
        {
            get { return Settings.Default.StartupWithWindows; }
            set
            {
                if( Settings.Default.StartupWithWindows == value) return; 

                Settings.Default.StartupWithWindows = value;
                Settings.Default.Save();

                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null) return;

                    if (Settings.Default.StartupWithWindows)
                    {
                        key.SetValue("PinWindows", Process.GetCurrentProcess().MainModule.FileName);
                    }
                    else
                    {
                        key.DeleteValue("PinWindows", false);
                    }
                }
            }
        }

        public bool MinimizeToTray
        {
            get { return Settings.Default.MinimizeToTray; }
            set
            {
                if( Settings.Default.MinimizeToTray == value) return;
                Settings.Default.MinimizeToTray = value;
                Settings.Default.Save();
            }
        }
    }
}