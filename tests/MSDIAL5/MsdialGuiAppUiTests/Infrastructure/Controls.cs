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
            fileName.Text = directory;
            fileName.Focus();
            Keyboard.Type(VirtualKeyShort.ENTER);
            // Navigating repaints the dialog; typing the names before it settles can be lost.
            System.Threading.Thread.Sleep(1000);
            fileName.Text = fileNames.Length == 1 ? fileNames[0] : string.Join(" ", fileNames.Select(name => $"\"{name}\""));
            fileName.Focus();
            Keyboard.Type(VirtualKeyShort.ENTER);
        }
    }
}
