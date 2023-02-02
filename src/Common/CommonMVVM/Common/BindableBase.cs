using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CompMs.CommonMVVM
{
    public class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);
        protected void OnPropertyChanged(string propertyname) => OnPropertyChanged(new PropertyChangedEventArgs(propertyname));

        protected virtual bool SetProperty<T>(ref T prop, T value, [CallerMemberName]string propertyname = "") {
            if (Equals(prop, value)) {
                return false;
            }

            prop = value;
            OnPropertyChanged(propertyname);
            return true;
        }
    }
}
