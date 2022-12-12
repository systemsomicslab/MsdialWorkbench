using System;
using System.Windows.Input;

namespace CompMs.CommonMVVM
{
    public sealed class NeverCommand : ICommand
    {
        public static NeverCommand Instance { get; } = new NeverCommand();

        private NeverCommand() {

        }

#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter) {
            return false;
        }

        public void Execute(object parameter) {
            return;
        }
    }
}
