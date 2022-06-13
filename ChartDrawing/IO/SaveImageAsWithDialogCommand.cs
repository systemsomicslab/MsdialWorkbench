using System;
using System.Windows;
using System.Windows.Input;

namespace CompMs.Graphics.IO
{
    public class SaveImageAsWithDialogCommand : ICommand
    {
        public SaveImageAsWithDialogCommand() {

        }

        public IElementFormatter Formatter { get; set; } = new NoneFormatter();


        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) {
            return true;
        }

        public void Execute(object parameter) {
            if (parameter is FrameworkElement element) {
                var model = new SaveImageAsDialogModel(element, Formatter);
                var viewmodel = new SaveImageAsDialogViewModel(model);
                var dialog = new SaveImageAsDialog
                {
                    DataContext = viewmodel,
                };
                dialog.Show();
            }
        }
    }
}
