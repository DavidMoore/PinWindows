namespace PinWindows
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Resources;
    using Microsoft.Win32;
    using Properties;
    using Application = System.Windows.Application;

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly NotifyIcon notifyIcon;

        public MainWindow()
        {
            InitializeComponent();

            notifyIcon = new NotifyIcon();

            StreamResourceInfo resourceStream = Application.GetResourceStream(new Uri("pack://application:,,,/App.ico"));
            if (resourceStream != null)
            {
                using (Stream iconStream = resourceStream.Stream)
                {
                    notifyIcon.Icon = new Icon(iconStream);
                }

                notifyIcon.Visible = true;
                notifyIcon.DoubleClick += OnNotifyIconDoubleClick;
            }

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key != null)
                {
                    if (Settings.Default.StartupWithWindows) key.SetValue("PinWindows", Environment.CommandLine);
                    else key.DeleteValue("PinWindows", false);
                }
            }

            DataContext = new ViewModel();
        }

        void OnNotifyIconDoubleClick(object sender, EventArgs args)
        {
            WindowState = WindowState.Normal;
            Show();
            Activate();
            Win32Api.BringToFront(Process.GetCurrentProcess().MainWindowHandle);
            Show();
            Activate();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized) Hide();

            base.OnStateChanged(e);
        }

        /// <summary>
        ///     Raises the <see cref="E:System.Windows.Window.Closed" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (notifyIcon != null) notifyIcon.Dispose();
        }
    }
}