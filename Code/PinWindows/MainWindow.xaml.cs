namespace PinWindows
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Resources;
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

            MaxHeight = Screen.PrimaryScreen.Bounds.Height*0.75;
            MinHeight = 300;
        }

        public MainWindow(ViewModel viewModel) : this()
        {
            ViewModel = viewModel;
        }

        internal ViewModel ViewModel
        {
            get { return DataContext as ViewModel; }
            set { DataContext = value; }
        }

        void OnNotifyIconDoubleClick(object sender, EventArgs args)
        {
            Show();

            WindowState = WindowState.Normal;
            
            Activate();
            Win32Api.BringToFront(Process.GetCurrentProcess().MainWindowHandle);
            Show();
            Activate();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized && ViewModel.MinimizeToTray)
            {
                Hide();
            }

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