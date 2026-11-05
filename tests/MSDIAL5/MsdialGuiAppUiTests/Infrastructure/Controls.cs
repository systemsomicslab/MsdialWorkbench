using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using System;
using System.Linq;

namespace CompMs.App.Msdial.UiTests.Infrastructure
{
    /// <summary>
    /// Finding and operating MS-DIAL's controls, each wait guarded by the session's health checks.
    /// </summary>
    internal static class Controls
    {
        public static readonly TimeSpan FindTimeout = TimeSpan.FromSeconds(30);

        public static AutomationElement ById(this MsdialSession session, AutomationElement root, string automationId, params string[] allowedDialogs) {
            return session.WaitFor($"finding #{automationId}",
                () => root.FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
                FindTimeout,
                allowedDialogs.Contains);
        }

        public static Button ButtonNamed(this MsdialSession session, AutomationElement root, string name, params string[] allowedDialogs) {
            return session.WaitFor($"finding the '{name}' button",
                () => root.FindFirstDescendant(cf => cf.ByName(name).And(cf.ByControlType(ControlType.Button)))?.AsButton(),
                FindTimeout,
                allowedDialogs.Contains);
        }

        /// <summary>
        /// The control of type <paramref name="type"/> that follows a label: MS-DIAL lays its
        /// settings out as a label and then its control, and most of those controls have no name.
        /// Only what comes after the label counts; the first control in the label's container may
        /// belong to the setting above it.
        /// </summary>
        public static AutomationElement ByLabel(this MsdialSession session, AutomationElement root, string label, ControlType type, params string[] allowedDialogs) {
            return session.WaitFor($"finding the {type} labelled '{label}'",
                () => root.FindAllDescendants(cf => cf.ByName(label).And(cf.ByControlType(ControlType.Text)))
                    .Select(text => FollowingControl(text, type))
                    .FirstOrDefault(found => found is not null),
                FindTimeout,
                allowedDialogs.Contains);
        }

        private static AutomationElement? FollowingControl(AutomationElement label, ControlType type) {
            // Climb from the label until one of the siblings after it is, or holds, the control.
            for (var element = label; element.Parent is { } parent; element = parent) {
                var siblings = parent.FindAllChildren();
                var index = Array.FindIndex(siblings, sibling => sibling.Equals(element));
                foreach (var sibling in siblings.Skip(index + 1)) {
                    if (sibling.ControlType == type) {
                        return sibling;
                    }
                    if (sibling.FindFirstDescendant(cf => cf.ByControlType(type)) is { } inside) {
                        return inside;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Brings a window to the front before the real mouse is used on it: a click lands on
        /// whatever window is on top at that point, which need not be MS-DIAL's once something else
        /// on the desktop has taken the foreground.
        /// </summary>
        public static void BringToFront(this Window window) {
            window.SetForeground();
            Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(300));
        }

        /// <summary>
        /// Presses a button once it is enabled; a command that never becomes executable fails the
        /// step instead of being clicked into nothing.
        /// </summary>
        public static void Press(this MsdialSession session, Button button, string step, params string[] allowedDialogs) {
            session.WaitFor($"'{step}' becoming enabled", () => button.IsEnabled ? button : null, FindTimeout, allowedDialogs.Contains);
            button.Invoke();
        }

        /// <summary>
        /// Types a value into a text box the way a person does. Most of MS-DIAL's text boxes hand
        /// their value to the view model only when they lose focus, and setting the text through
        /// UI Automation moves no focus, so without the Tab the value may never arrive.
        /// </summary>
        public static void Enter(this TextBox textBox, string value) {
            textBox.Focus();
            textBox.Text = value;
            Keyboard.Type(VirtualKeyShort.TAB);
            Wait.UntilInputIsProcessed();
        }

        /// <summary>
        /// Picks files in a Windows common file dialog by typing into its file name box: first the
        /// folder, to navigate there, then the names, quoted for a multiple selection.
        /// </summary>
        public static void ChooseFiles(this MsdialSession session, Window fileDialog, string directory, params string[] fileNames) {
            // 1148 is the file name box of the common file dialog, stable across Windows versions.
            var fileName = session.WaitFor("finding the file name box",
                () => fileDialog.FindFirstDescendant(cf => cf.ByAutomationId("1148").And(cf.ByControlType(ControlType.Edit)))?.AsTextBox(),
                FindTimeout,
                _ => true);
            // The names are looked up in the folder the dialog shows, which is the one it showed
            // last time unless navigation has finished; so wait until its address bar names the
            // folder, pressing Enter again if the first one was lost.
            for (var attempt = 0; attempt < 3 && !ShowsFolder(fileDialog, directory); attempt++) {
                fileName.Text = directory;
                fileName.Focus();
                Keyboard.Type(VirtualKeyShort.ENTER);
                FlaUI.Core.Tools.Retry.WhileFalse(() => ShowsFolder(fileDialog, directory), TimeSpan.FromSeconds(5));
            }
            if (!ShowsFolder(fileDialog, directory)) {
                session.Fail("choosing files", $"The file dialog did not move to {directory}.");
            }
            fileName.Text = fileNames.Length == 1 ? fileNames[0] : string.Join(" ", fileNames.Select(name => $"\"{name}\""));
            fileName.Focus();
            Keyboard.Type(VirtualKeyShort.ENTER);
        }

        // 1001 is the address bar of the common file dialog; its name ends with the folder shown,
        // after a localized "Address:" label.
        private static bool ShowsFolder(Window fileDialog, string directory) {
            try {
                var address = fileDialog.FindFirstDescendant(cf => cf.ByAutomationId("1001"))?.Name;
                return address is not null && address.TrimEnd('\\').EndsWith(directory.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception) {
                return false;
            }
        }
    }
}
