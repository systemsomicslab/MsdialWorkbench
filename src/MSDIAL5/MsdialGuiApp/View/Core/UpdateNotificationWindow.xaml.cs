using CompMs.App.Msdial.Model.Notification;
using System.Windows;

namespace CompMs.App.Msdial.View.Core
{
    /// <summary>
    /// Tells the user a new MS-DIAL is available and asks what to do about it.
    /// </summary>
    /// <remarks>
    /// Closing the window without choosing leaves <see cref="Choice"/> null, which records nothing,
    /// so the notification comes back on the next start as it always has.
    /// </remarks>
    public partial class UpdateNotificationWindow : Window
    {
        public UpdateNotificationWindow(string latestVersion) {
            InitializeComponent();
            MessageText.Text = $"A new MS-DIAL is available: {latestVersion}";
            RemindLaterButton.Content = $"Remind me in {UpdateReminder.RemindLaterInterval.Days} days";
        }

        public UpdateNotificationChoice? Choice { get; private set; }

        /// <summary>
        /// Shows the window over <paramref name="owner"/> and returns what the user chose.
        /// </summary>
        public static UpdateNotificationChoice? Ask(Window? owner, string latestVersion) {
            var window = new UpdateNotificationWindow(latestVersion);
            if (owner is not null && owner.IsLoaded) {
                window.Owner = owner;
            }
            else {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            window.ShowDialog();
            return window.Choice;
        }

        private void Choose(UpdateNotificationChoice choice) {
            Choice = choice;
            DialogResult = true;
        }

        private void OpenReleasePage_Click(object sender, RoutedEventArgs e) => Choose(UpdateNotificationChoice.OpenReleasePage);

        private void RemindLater_Click(object sender, RoutedEventArgs e) => Choose(UpdateNotificationChoice.RemindLater);

        private void SkipThisVersion_Click(object sender, RoutedEventArgs e) => Choose(UpdateNotificationChoice.SkipThisVersion);
    }
}
