using System;
using System.Windows.Input;

namespace CompMs.CommonMVVM
{
    public sealed class NeverCommand : ICommand
    {
        public static NeverCommand Instance { get; } = new NeverCommand();

        private NeverCommand() {

        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) {
            return false;
        }

        public void Execute(object parameter) {
            return;
        }
    }
}
