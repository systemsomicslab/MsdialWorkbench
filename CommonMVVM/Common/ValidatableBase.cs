using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CompMs.CommonMVVM
{
    public class ValidatableBase : BindableBase, INotifyDataErrorInfo
    {
        public bool HasValidationErrors => errors.Values.SelectMany(values => values).Any();
        bool INotifyDataErrorInfo.HasErrors => HasValidationErrors;

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        private readonly Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();

        protected void OnErrorsChanged(DataErrorsChangedEventArgs args) => ErrorsChanged?.Invoke(this, args);
        protected virtual void OnErrorsChanged([CallerMemberName] string propertyname = "") => ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyname));

        public IEnumerable GetErrors(string propertyName) {
            if (propertyName is null) return null;
            if (errors.ContainsKey(propertyName)) {
                return errors[propertyName];
            }
            return null;
        }

        public bool ContainsError(string propertyName) => errors.ContainsKey(propertyName) && errors[propertyName].Count > 0;

        protected override bool SetProperty<T>(ref T prop, T value, [CallerMemberName] string propertyname = "") {
            var setted = base.SetProperty(ref prop, value, propertyname);
            if (setted) {
                ValidateProperty(propertyname, value);
            }
            return setted;
        }

        protected void ValidateProperty(string propertyName, object value) {
            var context = new ValidationContext(this) { MemberName = propertyName };
            var validationErrors = new List<ValidationResult>();
            bool succeed;
            try {
                succeed = System.ComponentModel.DataAnnotations.Validator.TryValidateProperty(value, context, validationErrors);
            }
            catch (ArgumentException) {
                return;
            }
            if (succeed) {
                ClearErrors(propertyName);
            }
            else {
                SetErrors(propertyName, validationErrors.Select(error => error.ErrorMessage));
            }
        }

        protected void AddError(string propertyname, string error) {
            if (string.IsNullOrEmpty(error)) {
                return;
            }

            if (!errors.ContainsKey(propertyname)) {
                errors[propertyname] = new List<string>();
            }

            if (errors[propertyname].Contains(error)) {
                return;
            }

            errors[propertyname].Add(error);
            OnErrorsChanged(propertyname);
        } 

        protected void RemoveError(string propertyname, string error) {
            if (string.IsNullOrEmpty(error)) {
                return;
            }

            if (!errors.ContainsKey(propertyname)) {
                return;
            }

            errors[propertyname].RemoveAll(e => e == error);
            OnErrorsChanged(propertyname);
        } 

        protected void AddErrors(string propertyname, IEnumerable<string> errors) {
            if (errors == null) {
                return;
            }

            if (!this.errors.ContainsKey(propertyname)) {
                this.errors[propertyname] = new List<string>();
            }
            this.errors[propertyname].AddRange(errors.Where(error => !string.IsNullOrEmpty(error)));
            OnErrorsChanged(propertyname);
        }

        protected void RemoveErrors(string propertyname, IEnumerable<string> errors) {
            if (errors == null) {
                return;
            }

            if (!this.errors.ContainsKey(propertyname)) {
                return;
            }
            var errorset = errors.Where(error => !string.IsNullOrEmpty(error)).ToHashSet();
            this.errors[propertyname].RemoveAll(error => errorset.Contains(error));
            OnErrorsChanged(propertyname);
        }

        protected void SetErrors(string propertyname, IEnumerable<string> errors) {
            if (errors == null) {
                return;
            }

            this.errors[propertyname] = errors.Where(error => !string.IsNullOrEmpty(error)).ToList();
            OnErrorsChanged(propertyname);
        }

        protected void ClearErrors(string propertyname) {
            if (errors.ContainsKey(propertyname)) {
                errors.Remove(propertyname);
                OnErrorsChanged(propertyname);
            }
        }

        protected void UpdateErrors(string propertyname, IEnumerable<string> errors) {
            var errors_ = errors.Where(error => !string.IsNullOrEmpty(error)).ToList();
            if (errors_.Count > 0) {
                SetErrors(propertyname, errors_);
            }
            else {
                ClearErrors(propertyname);
            }
        }
    }
}
