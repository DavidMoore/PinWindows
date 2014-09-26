namespace PinWindows
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Documents;
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
                Settings.Default.StartupWithWindows = value; 
                Settings.Default.Save();

                if (Settings.Default.StartupWithWindows)
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (key != null)
                        {
                            key.SetValue("PinWindows", Environment.CommandLine);
                        }
                    }
                }
                else
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue("PinWindows", false);
                        }
                    }
                }
            }
        }

        public bool MinimizeToTray
        {
            get { return Settings.Default.MinimizeToTray; }
            set
            {
                Settings.Default.MinimizeToTray = value;
                Settings.Default.Save();
            }
        }
    }
}