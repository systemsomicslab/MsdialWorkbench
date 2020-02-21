using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This code should be required to use 'command' code in XAML code.
    /// </summary>
    public class DelegateCommand : ICommand
    {
        private Action<object> executeAction;
        private Func<object, bool> canExecuteAction;
		public event EventHandler CanExecuteChanged;


		public DelegateCommand(Action<object> executeAction, Func<object, bool> canExecuteAction)
        {
            this.executeAction = executeAction;
            this.canExecuteAction = canExecuteAction;
        }

        #region ICommand

        public bool CanExecute(object parameter)
        {
			if(parameter == null)
			{
				return true;
			}
            return canExecuteAction(parameter);
        }

        //public event EventHandler CanExecuteChanged
        //{
        //    add { CommandManager.RequerySuggested += value; }
        //    remove { CommandManager.RequerySuggested -= value; }
        //}

        public void Execute(object parameter)
        {
            executeAction(parameter);
        }

		public void RaiseCanExecuteChanged()
		{
			if (CanExecuteChanged != null)
			{
				CanExecuteChanged(this, EventArgs.Empty);
			}
		}
		#endregion
	}
}
