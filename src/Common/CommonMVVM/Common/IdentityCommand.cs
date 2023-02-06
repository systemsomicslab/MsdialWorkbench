using System;
using System.Windows.Input;

namespace CompMs.CommonMVVM
{
    public sealed class IdentityCommand : ICommand
    {
        public static IdentityCommand Instance { get; } = new IdentityCommand();

        private IdentityCommand() { }

#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) { }
    }
}
