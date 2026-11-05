using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace CompMs.App.Msdial.UiTests.Infrastructure
{
    /// <summary>
    /// Drags items from a File Explorer window onto an MS-DIAL control, as a user adds the runs
    /// that vendors store as folders (Agilent and Bruker .d, Waters .raw): a file dialog cannot
    /// pick a folder, so dropping them is the only way in.
    ///
    /// It moves the real mouse, so both windows are arranged side by side first.
    /// </summary>
    internal static class ExplorerDrag
    {
        public static void DragFromExplorer(this MsdialSession session, string directory, IReadOnlyList<string> names, Window targetWindow, AutomationElement target, params string[] allowedDialogs) {
            var desktop = session.Automation.GetDesktop();
            var title = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar));
            Process.Start("explorer.exe", $"\"{directory}\"");
            var explorer = session.WaitFor("opening File Explorer",
                // The window is named after the folder, followed by " - File Explorer" or similar
                // depending on the Windows version.
                () => desktop.FindAllChildren(cf => cf.ByClassName("CabinetWClass"))
                    .FirstOrDefault(window => window.Name.StartsWith(title, StringComparison.OrdinalIgnoreCase))?.AsWindow(),
                Controls.FindTimeout,
                allowedDialogs.Contains);
            try {
                // Side by side on the monitor MS-DIAL is on: the desktop spans every monitor, and a
                // drag across monitors with different scaling misses its target.
                var screen = System.Windows.Forms.Screen.FromRectangle(session.MainWindow.BoundingRectangle).WorkingArea;
                Arrange(explorer, screen.Left, screen.Top, screen.Width / 3, screen.Height / 2);
                Arrange(targetWindow, screen.Left + screen.Width / 3, screen.Top, screen.Width * 2 / 3, screen.Height * 2 / 3);
                Thread.Sleep(500);

                var items = names.Select(name => session.WaitFor($"finding '{name}' in File Explorer",
                    () => explorer.FindFirstDescendant(cf => cf.ByControlType(ControlType.ListItem).And(cf.ByName(name))),
                    Controls.FindTimeout,
                    allowedDialogs.Contains)).ToList();
                items[0].Click();
                foreach (var item in items.Skip(1)) {
                    using (Keyboard.Pressing(VirtualKeyShort.CONTROL)) {
                        item.Click();
                    }
                }

                // Pressing on the item just clicked would count as a double click and open the
                // folder instead of dragging it.
                Thread.Sleep(System.Windows.Forms.SystemInformation.DoubleClickTime + 200);

                // OLE drag and drop starts only after the mouse moves a few pixels with the button
                // held, and the target reacts to intermediate moves, so the path is walked slowly.
                var from = items[0].GetClickablePoint();
                var bounds = target.BoundingRectangle;
                var to = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
                Mouse.MoveTo(from);
                Mouse.Down(MouseButton.Left);
                for (var step = 1; step <= 30; step++) {
                    Mouse.MoveTo(new Point(from.X + (to.X - from.X) * step / 30, from.Y + (to.Y - from.Y) * step / 30));
                    Thread.Sleep(30);
                }
                Thread.Sleep(300);
                Mouse.Up(MouseButton.Left);
            }
            finally {
                explorer.Close();
            }
        }

        private static void Arrange(Window window, double left, double top, double width, double height) {
            var transform = window.Patterns.Transform.PatternOrDefault;
            if (transform is null) {
                return;
            }
            if (window.Patterns.Window.PatternOrDefault is { } windowPattern && windowPattern.WindowVisualState.Value != WindowVisualState.Normal) {
                windowPattern.SetWindowVisualState(WindowVisualState.Normal);
            }
            if (transform.CanMove) {
                transform.Move(left, top);
            }
            if (transform.CanResize) {
                transform.Resize(width, height);
            }
        }
    }
}
