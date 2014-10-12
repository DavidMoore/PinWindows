namespace PinWindows
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Deployment.Application;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.Win32;
    using Properties;

    public class ViewModel
    {
        public ViewModel()
        {
            WindowState = WindowState.Normal;

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

        delegate bool EnumThreadDelegate(IntPtr handle, IntPtr param);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool EnumThreadWindows(int threadId, EnumThreadDelegate callback, IntPtr param);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCharacters);

        static WindowModel GetWindowModel(IntPtr handle)
        {
            if (!IsWindowVisible(handle)) return null;

            var window = new WindowModel();
            
            window.Handle = handle;
            window.Title = GetWindowTitle(window.Handle);
            window.IsPinned = AlwaysOnTop.IsWindowTopMost(window.Handle);

            return window;
        }

        static string GetWindowTitle(IntPtr handle)
        {
            var title = new StringBuilder(Int16.MaxValue);
            var result = GetWindowText(handle, title, title.Capacity);
            if (result == 0) return null;
            if (result > 0) return title.ToString(0, result);
            var ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
            Trace.TraceWarning("Couldn't get window title for handle {0}: {1}", handle, ex);
            return null;
        }

        internal void EnumerateWindows()
        {
            var results = new List<WindowModel>();

            foreach (var process in Process.GetProcesses()
                .Where(x => x.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(x.MainWindowTitle))
                .OrderBy(x => x.MainWindowTitle))
            {
                foreach (ProcessThread thread in process.Threads)
                {
                    EnumThreadWindows(thread.Id, (handle, ptr) =>
                    {
                        var model = GetWindowModel(handle);
                        if( model != null) results.Add(model);
                        return true;
                    }, IntPtr.Zero);
                }
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

                if (!ApplicationDeployment.IsNetworkDeployed) return;

                Settings.Default.StartupWithWindows = value;
                Settings.Default.Save();

                ApplyRegistrySettings();
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

        public WindowState WindowState { get; set; }

        public void ApplyRegistrySettings()
        {
            if (!ApplicationDeployment.IsNetworkDeployed)
            {
                Trace.TraceWarning("Not applying registry settings, as this copy of Pin Windows is not an installed deployment.");
                return;
            }

            using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key == null) return;

                if (Settings.Default.StartupWithWindows)
                {
                    key.SetValue("PinWindows", string.Format("\"{0}\" /background", Process.GetCurrentProcess().MainModule.FileName));
                }
                else
                {
                    key.DeleteValue("PinWindows", false);
                }
            }
        }
    }
}