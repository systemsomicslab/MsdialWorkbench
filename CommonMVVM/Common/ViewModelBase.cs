using CompMs.CommonMVVM.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Once this ViewModelBase.cs is inherited in a class, our developers can easily use 'command', 'validator', and 'propertychanged' functions in C# and XAML codes.
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged, IDataErrorInfo
    {
        #region // Required Method for Command
        private ICommand okCommand;
        public ICommand OkCommand
        {
            get
            {
                if (okCommand != null) return okCommand;
                okCommand = new DelegateCommand(executeCommand, canExecuteCommand);
                return okCommand;
            }
        }

        private bool canExecuteCommand(object parameter)
        {
            return !HasError;
        }

        protected virtual void executeCommand(object parameter)
        {
        }


        public event EventHandler<EventArgs> CloseViewHandler;

        private ICommand closeCommand;
        public ICommand CloseCommand { get { if (closeCommand != null) return closeCommand; closeCommand = new DelegateCommand(excuteCloseCommand, canExcuteCloseCommand); return closeCommand; } }

        private bool canExcuteCloseCommand(object parameter)
        {
            return true;
        }

        private void excuteCloseCommand(object parameter)
        {
            CloseView();
        }

        public void CloseView()
        {
            if (CloseViewHandler != null)
            {
                CloseViewHandler(this, EventArgs.Empty);
            }
        }



        #endregion

        #region // Required Method for IDataErrorInfo
        private Dictionary<string, string> errors = new Dictionary<string, string>();
        public string Error { get { return null; } }

        public string this[string columnName]
        {
            get
            {
                try
                {
                    var result = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                    if (Validator.TryValidateProperty(GetType().GetProperty(columnName).GetValue(this, null), new ValidationContext(this, null, null) { MemberName = columnName }, result))
                    {
                        if (ValidationResultException(columnName)) this.ClearError(columnName);
                    }
                    else
                    {
                        if (ValidationResultException(columnName)) this.SetError(columnName, result.First().ErrorMessage);
                    }

                    if (errors.ContainsKey(columnName))
                    {
                        return errors[columnName];
                    }
                    return null;
                }
                finally { CommandManager.InvalidateRequerySuggested(); }
            }
        }

        //Register the exception properties
        private bool ValidationResultException(string columnName)
        {
            return true;
        }

        protected void SetError(string propertyName, string errorMessage)
        {
            this.errors[propertyName] = errorMessage;
            this.OnPropertyChanged("HasError");
        }

        protected void ClearError(string propertyName)
        {
            if (this.errors.ContainsKey(propertyName))
            {
                this.errors.Remove(propertyName);
                this.OnPropertyChanged("HasError");
            }
        }

        protected void ClearErrors()
        {
            this.errors.Clear();
            this.OnPropertyChanged("HasError");
        }

        private bool hasViewError;

        public bool HasViewError
        {
            get
            { return hasViewError; }
            set
            {
                if (EqualityComparer<bool>.Default.Equals(hasViewError, value)) return;
                hasViewError = value;
                OnPropertyChanged("HasViewError");
                OnPropertyChanged("HasError");
            }
        }

        public bool HasError
        {
            get
            {
                return this.errors.Count != 0 || HasViewError;
            }
        }
        #endregion

        #region // Required Methods for INotifyPropertyChanged
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler eventHandlers = this.PropertyChanged;
            if (null != eventHandlers)
                eventHandlers(this, e);
        }
        #endregion // Required Methods for INotifyPropertyChanged

        
    }
}

namespace CompMs.CommonMVVM
{
    public class ViewModelBase : ValidatableBase, IDisposable
    {
        protected DisposableCollection Disposables { get; } = new DisposableCollection();

        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    Disposables.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
