using System.Linq;
using System.Windows;

namespace CompMs.App.Msdial.View.Statistics {
    internal partial class NotamePackageDialog : Window {
        private NotamePackageDialog(string title, string details) {
            InitializeComponent();
            Title = title;
            DetailsTextBox.Text = details;
        }

        public static void Show(string title, string details) {
            var application = Application.Current;
            var owner = application?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
                ?? application?.MainWindow;
            var dialog = new NotamePackageDialog(title, details) {
                Owner = owner,
                WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
            };
            dialog.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
