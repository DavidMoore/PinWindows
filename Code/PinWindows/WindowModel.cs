namespace PinWindows
{
    using System;
    using System.ComponentModel;
    using System.Media;

    public class WindowModel : INotifyPropertyChanged
    {
        bool isPinned;
        public IntPtr Handle { get; set; }
        public string Title { get; set; }

        public bool IsPinned
        {
            get { return isPinned; }
            set
            {
                if (value.Equals(isPinned)) return;

                // Are we pinning, or unpinning?
                if (AlwaysOnTop.SetWindowTopMost(Handle, value))
                {
                    isPinned = value;                    
                }
                else
                {
                    SystemSounds.Exclamation.Play();
                }

                OnPropertyChanged("IsPinned");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}