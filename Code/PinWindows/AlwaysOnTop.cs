namespace PinWindows
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    class AlwaysOnTop
    {
        internal static readonly IntPtr WindowHandleTopMost = new IntPtr(-1);
        internal static readonly IntPtr WindowHandleNotTopMost = new IntPtr(-2);
        
        [Flags]
        public enum SetWindowPosFlags : uint
        {
            /// <summary>
            ///     Retains the current position.
            /// </summary>
            SetWindowPositionNoMove = 0x0002,
            
            /// <summary>
            ///     Retains the current size.
            /// </summary>
            SetWindowPositionNoSize = 0x0001
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        internal const int GetWindowLongExtendedStyles = -20;
        internal const UInt32 WindowExtendedStyleTopMost = 0x0008;

        public static bool IsWindowTopMost(IntPtr handle)
        {
            return (GetWindowLong(handle, GetWindowLongExtendedStyles) & WindowExtendedStyleTopMost) != 0;
        }

        public static bool SetWindowTopMost(IntPtr handle, bool pin)
        {
            var pinToggle = pin ? WindowHandleTopMost : WindowHandleNotTopMost;
            var result = SetWindowPos(handle, pinToggle, 0, 0, 0, 0, SetWindowPosFlags.SetWindowPositionNoMove | SetWindowPosFlags.SetWindowPositionNoSize);
            if (!result)
            {
                var lastHResult = Marshal.GetHRForLastWin32Error();
                var lastErrorCode = Marshal.GetLastWin32Error();
                var ex = Marshal.GetExceptionForHR(lastHResult);
                Trace.TraceWarning(ex.ToString());
                return false;
            }

            return true;
        }
    }
}