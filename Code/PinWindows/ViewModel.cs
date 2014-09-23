namespace PinWindows
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Input;

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
            foreach (var process in Process.GetProcesses()
                .Where(x => x.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(x.MainWindowTitle))
                .OrderBy(x => x.MainWindowTitle))
            {
                var window = new WindowModel();
                window.Handle = process.MainWindowHandle;
                window.Title = process.MainWindowTitle;
                window.IsPinned = AlwaysOnTop.IsWindowTopMost(window.Handle);
                Windows.Add(window);
            }
        }

        public ObservableCollection<WindowModel> Windows { get; private set; }
    }
}