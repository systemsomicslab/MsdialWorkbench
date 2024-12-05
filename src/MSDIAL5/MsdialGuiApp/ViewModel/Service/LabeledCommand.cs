using CompMs.CommonMVVM;
using System;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Service
{
    internal sealed class LabeledCommand : ViewModelBase
    {
        public LabeledCommand(ICommand command, string label, bool disposeCommand = true) {
            Command = command;
            Label = label;

            if (disposeCommand && command is IDisposable disposable) {
                Disposables.Add(disposable);
            }
        }

        public ICommand Command { get; }
        public string Label { get; }
    }
}
