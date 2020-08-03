using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CompMs.Graphics.Core.Base
{
    public class DataSeries : INotifyPropertyChanged
    {
        public ObservableCollection<DataPoint> Datas
        {
            get => datas;
            set
            {
                var d = datas;
                if (SetProperty(ref datas, value))
                {
                    if (d != null)
                        d.CollectionChanged -= OnDatasElemntChanged;
                    if (datas != null)
                        datas.CollectionChanged += OnDatasElemntChanged;
                }
            }
        }
        public int ID { get; }
        public int Type
        {
            get => type;
            set => SetProperty(ref type, value);
        }

        ObservableCollection<DataPoint> datas;
        int type;

        static int member_id = 0;

        public DataSeries()
        {
            ID = member_id++;
            datas = new ObservableCollection<DataPoint>();
        }

        public override string ToString()
        {
            return $"Type={Type} ID={ID}";
        }

        void OnDatasElemntChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            RaisePropertyChanged("Datas");

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyname) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));

        protected bool SetProperty<T>(ref T prop, T value, [CallerMemberName]string propertyname = "")
        {
            if (prop == null || prop.Equals(value)) return false;
            prop = value;
            RaisePropertyChanged(propertyname);
            return true;
        }
    }
}
