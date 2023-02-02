using System;
using System.Windows.Input;
using System.Diagnostics;

namespace edu.ucdavis.fiehnlab.MonaExport.UtilClasses
{
	public class RelayCommand : ICommand {
        //this class alows the buttons connected to list entries to operate properly
        readonly Action<object> _execute;
        readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute) : this(execute, null) { }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute) {
            if (execute == null) throw new ArgumentNullException("execute");
            _execute = execute;
            _canExecute = canExecute;
        }

        [DebuggerStepThrough]
        public bool CanExecute(object parameters) {
            return _canExecute == null ? true : _canExecute(parameters);
        }
        public event EventHandler CanExecuteChanged {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        public void Execute(object parameters) {
            _execute(parameters);
        }
        
    }
}
