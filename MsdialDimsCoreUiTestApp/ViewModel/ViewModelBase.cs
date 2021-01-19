using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MsdialDimsCoreUiTestApp.ViewModel
{
    class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyname) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));

        protected bool SetProperty<T>(ref T property, T value, [CallerMemberName]string propertyname = "") {
            if (Equals(value, property)) return false;
            property = value;
            RaisePropertyChanged(propertyname);
            return true;
        }
    }
}
