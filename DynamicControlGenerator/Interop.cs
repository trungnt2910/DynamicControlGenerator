using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Uno.Extras.ToastNotification
{
    public static class Interop
    {
        #region Unmanaged Functions
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        #endregion

        #region Constants
        const uint SPI_GETWORKAREA = 0x0030;
        #endregion

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            int left;
            int top;
            int right;
            int bottom;

            public static implicit operator Rect(RECT r)
            {
                Debug.WriteLine(r.left);
                Debug.WriteLine(r.top);
                Debug.WriteLine(r.right);
                Debug.WriteLine(r.bottom);

                var factor = GetScaleFactor();

                Debug.WriteLine(factor);

                return new Rect
                {
                    X = r.left / factor,
                    Y = r.top / factor,
                    Width = (r.right - r.left) / factor,
                    Height = (r.bottom - r.top) / factor
                };
            }
        }

        public static Rect GetWorkArea()
        {
            var rect = new RECT();

            if (!SystemParametersInfo(SPI_GETWORKAREA, 0, ref rect, 0))
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }

            return rect;
        }

        private static float GetScaleFactor()
        {
            IntPtr desktopWnd = IntPtr.Zero;
            IntPtr dc = GetDC(desktopWnd);
            var dpi = 100f;
            const int LOGPIXELSX = 88;
            try
            {
                dpi = GetDeviceCaps(dc, LOGPIXELSX);
            }
            finally
            {
                ReleaseDC(desktopWnd, dc);
            }
            return dpi / 96f;
        }
    }
}
