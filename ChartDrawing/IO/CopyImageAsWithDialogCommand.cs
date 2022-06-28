using System;
using System.Windows;
using System.Windows.Input;

namespace CompMs.Graphics.IO
{
    public sealed class CopyImageAsWithDialogCommand : ICommand
    {
        public static CopyImageAsWithDialogCommand Instance { get; } = new CopyImageAsWithDialogCommand();

        public CopyImageAsWithDialogCommand() {

        }

        public IElementFormatter Formatter { get; set; } = new NoneFormatter();


        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) {
            return true;
        }

        public void Execute(object parameter) {
            if (parameter is FrameworkElement element) {
                var model = new CopyImageAsDialogModel(element, Formatter);
                var viewmodel = new CopyImageAsDialogViewModel(model);
                var dialog = new CopyImageAsDialog
                {
                    DataContext = viewmodel,
                };
                dialog.Show();
            }
        }
    }
}
