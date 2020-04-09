using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Rfx.Riken.OsakaUniv;
using CompMs.Graphics.Core.Base;

namespace ChartDrawingUiTest.Default
{
    public class DefaultVM : ViewModelBase
    {
        public DefaultChartData Data
        {
            get => data;
            set {
                SetProperty(ref data, value);
                Data.PropertyChanged += new PropertyChangedEventHandler(DataChanged);
            }
        }
        DefaultChartData data;

        public DefaultVM()
        {
            Data = new DefaultChartData();
            PropertyChanged += (s, e) => Console.WriteLine(s.ToString() + " Property changed fire!");
        }

        void SetProperty<T>(ref T property, T value, [CallerMemberName]string propertyname = null)
        {
            if (Equals(value, property)) return;
            property = value;
            OnPropertyChanged(propertyname);
        }

        public void DataChanged(object sender, PropertyChangedEventArgs e)
        {
            IChartData changedData = sender as IChartData;
            if(Equals(changedData, Data))
            {
                OnPropertyChanged("Data");
            }
        }
    }
}
