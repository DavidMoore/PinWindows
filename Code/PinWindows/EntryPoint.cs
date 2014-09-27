namespace PinWindows
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using Properties;

    static class EntryPoint
    {
        [STAThread]
        internal static void Main(string[] args)
        {
            UpgradeSettings();

            var viewModel = new ViewModel();

            viewModel.ApplyRegistrySettings();

            // When starting up automatically with Windows, the /background switch is used to start minimized.
            if (args != null && args.Length > 0)
            {
                if (args.Any(x => string.Equals("/background", x,StringComparison.OrdinalIgnoreCase)))
                {
                    viewModel.WindowState = WindowState.Minimized;
                }
            }

            var window = new MainWindow(viewModel);

            var app = new App();
            app.Run(window);
        }
        
        static void UpgradeSettings()
        {
            try
            {
                // Upgrade / migrate custom settings if necessary
                Settings.Default.Upgrade();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Couldn't upgrade settings: " + ex);
            }
        }
    }
}