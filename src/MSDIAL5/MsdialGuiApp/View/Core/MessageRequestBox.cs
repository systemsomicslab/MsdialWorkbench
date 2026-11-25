using CompMs.App.Msdial.Model.Service;
using System.Windows;

namespace CompMs.App.Msdial.View.Core
{
    /// <summary>
    /// How the view shows a <see cref="MessageRequest"/> as a message box, and what it answers.
    /// </summary>
    internal static class MessageRequestBox
    {
        public static MessageBoxButton ButtonsOf(MessageRequest request) {
            return request.AsksYesNo ? MessageBoxButton.YesNo : MessageBoxButton.OK;
        }

        public static MessageBoxImage IconOf(MessageRequest request) {
            switch (request.Severity) {
                case MessageSeverity.Information:
                    return MessageBoxImage.Information;
                case MessageSeverity.Warning:
                    return MessageBoxImage.Warning;
                default:
                    return MessageBoxImage.Error;
            }
        }

        /// <summary>
        /// The <see cref="MessageRequest.Result"/> of an answer: true for Yes or OK.
        /// </summary>
        public static bool IsAccepted(MessageBoxResult result) {
            return result == MessageBoxResult.Yes || result == MessageBoxResult.OK;
        }
    }
}
