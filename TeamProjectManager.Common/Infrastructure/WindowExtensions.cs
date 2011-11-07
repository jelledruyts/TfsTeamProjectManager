using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace TeamProjectManager.Common.Infrastructure
{
    public static class WindowExtensions
    {
        public static IWin32Window GetIWin32Window(this Visual visual)
        {
            var source = (System.Windows.Interop.HwndSource)PresentationSource.FromVisual(visual);
            return new Win32WindowWrapper(source.Handle);
        }

        private class Win32WindowWrapper : IWin32Window
        {
            public IntPtr Handle { get; private set; }

            public Win32WindowWrapper(IntPtr handle)
            {
                this.Handle = handle;
            }
        }
    }
}