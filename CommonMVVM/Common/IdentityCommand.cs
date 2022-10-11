using System;
using System.Windows.Input;

namespace CompMs.CommonMVVM
{
    public sealed class IdentityCommand : ICommand
    {
        public static IdentityCommand Instance { get; } = new IdentityCommand();

        private IdentityCommand() { }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) { }
    }
}
