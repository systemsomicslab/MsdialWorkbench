using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CompMs.Graphics.Core.Base
{
    public class DataPoint : INotifyPropertyChanged
    {
        public double X
        {
            get => x;
            set => SetProperty(ref x, value);
        }
        public double Y
        {
            get => y;
            set => SetProperty(ref y, value);
        }
        public int ID { get; }
        public int Type
        {
            get => type;
            set => SetProperty(ref type, value);
        }

        double x, y;
        int type;

        static int member_id = 0;

        public DataPoint()
        {
            ID = member_id++;
        }

        public DataPoint Clone() {
            return new DataPoint
            {
                X = X,
                Y = Y,
                Type = Type,
            };
        }

        public override string ToString()
        {
            return $"X={X} Y={Y} Type={Type} ID={ID}";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyname) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));

        protected bool SetProperty<T>(ref T prop, T value, [CallerMemberName]string propertyname = "")
        {
            if (prop.Equals(value)) return false;
            prop = value;
            RaisePropertyChanged(propertyname);
            return true;
        }
    }
}
