using FlaUI.Core.AutomationElements;
using System;
using System.Text;

namespace CompMs.App.Msdial.UiTests.Infrastructure
{
    /// <summary>
    /// Writes the UI Automation tree as indented text: what a test can see, which is what a person
    /// writing or debugging a test needs when a control cannot be found.
    /// </summary>
    internal static class UiTree
    {
        public static string Dump(AutomationElement root, int maxDepth = 25) {
            var builder = new StringBuilder();
            Append(builder, root, 0, maxDepth);
            return builder.ToString();
        }

        private static void Append(StringBuilder builder, AutomationElement element, int depth, int maxDepth) {
            builder.Append(' ', depth * 2).AppendLine(Describe(element));
            if (depth >= maxDepth) {
                return;
            }
            AutomationElement[] children;
            try {
                children = element.FindAllChildren();
            }
            catch (Exception) {
                return;
            }
            foreach (var child in children) {
                Append(builder, child, depth + 1, maxDepth);
            }
        }

        private static string Describe(AutomationElement element) {
            try {
                var properties = element.Properties;
                var automationId = properties.AutomationId.ValueOrDefault;
                var enabled = properties.IsEnabled.ValueOrDefault ? "" : " (disabled)";
                // What a text box holds and whether a box is ticked are what a failing step most
                // often needs to be told, and neither is in the name.
                var value = element.Patterns.Value.PatternOrDefault?.Value.ValueOrDefault;
                var toggle = element.Patterns.Toggle.PatternOrDefault?.ToggleState.ValueOrDefault;
                return $"{properties.ControlType.ValueOrDefault} \"{properties.Name.ValueOrDefault}\""
                    + (string.IsNullOrEmpty(automationId) ? "" : $" #{automationId}")
                    + $" [{properties.ClassName.ValueOrDefault}]{enabled}"
                    + (value is null ? "" : $" value=\"{value}\"")
                    + (toggle is null ? "" : $" toggle={toggle}");
            }
            catch (Exception exception) {
                return $"(unreadable: {exception.Message})";
            }
        }
    }
}
