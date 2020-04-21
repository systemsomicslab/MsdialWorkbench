using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Rfx.Riken.OsakaUniv;

namespace ChartDrawingUiTest.Default
{
    public class DefaultVM : ViewModelBase
    {
        public Object Data
        {
            get => data;
            set => SetProperty(ref data, value);
        }
        Object data;

        public DefaultVM()
        {
            Data = null;
            PropertyChanged += (s, e) => Console.WriteLine(s.ToString() + " Property changed fire!");
        }

        void SetProperty<T>(ref T property, T value, [CallerMemberName]string propertyname = null)
        {
            if (Equals(value, property)) return;
            property = value;
            OnPropertyChanged(propertyname);
        }
    }
}
