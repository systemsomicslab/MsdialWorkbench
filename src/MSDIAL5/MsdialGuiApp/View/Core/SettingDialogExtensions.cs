using CompMs.Graphics.UI;

namespace CompMs.App.Msdial.View.Core
{
    internal static class SettingDialogExtensions
    {
        /// <summary>
        /// Shows <paramref name="dialog"/> modally. When it closes without Finish (the title-bar close button,
        /// Alt+F4 or Cancel), runs the view model's CancelCommand, so that edits made in a view model that outlives
        /// the dialog are thrown away as Cancel would throw them away, not left behind uncommitted.
        /// </summary>
        /// <returns>The dialog result: true only when the dialog was finished.</returns>
        public static bool? ShowDialogAndCancelUnlessFinished(this SettingDialog dialog) {
            var result = dialog.ShowDialog();
            if (result != true && dialog.DataContext is SettingDialogViewModel viewModel) {
                var cancel = viewModel.CancelCommand;
                if (cancel?.CanExecute(null) == true) {
                    cancel.Execute(null);
                }
            }
            return result;
        }
    }
}
