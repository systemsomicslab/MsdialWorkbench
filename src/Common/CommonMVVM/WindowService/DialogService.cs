using System;
using System.Windows;

namespace CompMs.CommonMVVM.WindowService
{
    public sealed class DialogService<TView, TViewModel> : IWindowService<TViewModel> where TView : Window, new() {
        public DialogService() {

        }

        public DialogService(Window owner) {
            Owner = owner;
        }

        public Window Owner { get; }

        public WindowStartupLocation WindowStartupLocation { get; set; } = WindowStartupLocation.CenterOwner;

        public void Show(TViewModel viewmodel) {
            var dialog = new TView()
            {
                Owner = Owner,
                WindowStartupLocation = WindowStartupLocation,
                DataContext = viewmodel,
            };

            try {
                dialog.Show();
            }
            catch (InvalidOperationException e) {
                MessageBox.Show(Owner, e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool? ShowDialog(TViewModel viewmodel) {
            var dialog = new TView()
            {
                Owner = Owner,
                WindowStartupLocation = WindowStartupLocation,
                DataContext = viewmodel,
            };

            try {
                return dialog.ShowDialog();
            }
            catch (InvalidOperationException e) {
                MessageBox.Show(Owner, e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }
}
