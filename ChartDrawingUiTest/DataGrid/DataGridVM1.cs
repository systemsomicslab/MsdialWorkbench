using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using CompMs.Common.DataObj;

namespace ChartDrawingUiTest.DataGrid
{
    public class DataGridVM1 : INotifyPropertyChanged
    {
        public ObservableCollection<RawData> Datas
        {
            get => datas;
            set => SetProperty(ref datas, value);
        }

        private ObservableCollection<RawData> datas;

        public DataGridVM1()
        {
            var datas = new List<RawData>();
            for (int i = 0; i < 10; i++)
            {
                datas.Add(new RawData
                {
                    ScanNumber = i,
                    PrecursorMz = Math.Pow(i, 2),
                    Name = $"Sample {i}",
                    IonMode = CompMs.Common.Enum.IonMode.Positive,
                    IsMarked = false,
                });
            }
            Datas = new ObservableCollection<RawData>(datas);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyname = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));

        protected bool SetProperty<T>(ref T prop, T value, [CallerMemberName]string propertyname = "")
        {
            if ((prop == null && value != null) || !prop.Equals(value))
            {
                prop = value;
                RaisePropertyChanged(propertyname);
                return true;
            }
            return false;
        }
    }
}
