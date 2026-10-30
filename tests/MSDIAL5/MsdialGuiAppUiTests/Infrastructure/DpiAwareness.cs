using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.InteropServices;

namespace CompMs.App.Msdial.UiTests.Infrastructure
{
    /// <summary>
    /// Makes the test process see real pixels on every monitor. UI Automation reports physical
    /// coordinates, while a process that is not DPI aware is handed scaled ones for the mouse, so
    /// on a scaled monitor a click or a drag would land beside the control it aims at.
    /// </summary>
    [TestClass]
    public static class DpiAwareness
    {
        private static readonly IntPtr PerMonitorAwareV2 = new IntPtr(-4);

        [AssemblyInitialize]
        public static void Initialize(TestContext context) {
            try {
                SetProcessDpiAwarenessContext(PerMonitorAwareV2);
            }
            catch (EntryPointNotFoundException) {
                // Older than Windows 10 1703; nothing better is available.
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    }
}
