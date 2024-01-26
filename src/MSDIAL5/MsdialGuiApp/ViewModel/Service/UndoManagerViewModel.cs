using CompMs.App.Msdial.Model.Service;
using CompMs.CommonMVVM;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Service
{
    internal sealed class UndoManagerViewModel : ViewModelBase
    {
        private readonly UndoManager _manager;

        public UndoManagerViewModel(UndoManager manager) {
            _manager = manager;
        }

        public ICommand UndoCommand => _undoCommand ??= new DelegateCommand(_manager.Undo);
        private ICommand? _undoCommand;

        public ICommand RedoCommand => _redoCommand ??= new DelegateCommand(_manager.Redo);
        private ICommand? _redoCommand;
    }
}
