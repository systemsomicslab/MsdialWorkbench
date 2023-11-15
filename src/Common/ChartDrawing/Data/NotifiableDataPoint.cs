using CompMs.Graphics.Core.Base;
using System;
using System.ComponentModel;

namespace CompMs.Graphics.Data
{
    internal class NotifiableDataPoint : INotifyPropertyChanged, IDisposable
    {
        private INotifyPropertyChanged _data;
        private PropertyChangedEventHandler _handle;
        private readonly Func<object, AxisValue> _xMap, _yMap;
        private bool _disposedValue;

        public event PropertyChangedEventHandler PropertyChanged;

        public NotifiableDataPoint(object data,  string xProperty, string yProperty, Func<object, AxisValue> xMap, Func<object, AxisValue> yMap)
        {
            _xMap = xMap;
            _yMap = yMap;
            X = xMap(data);
            Y = yMap(data);
            Item = data;

            if (data is INotifyPropertyChanged np) {
                _data = np;
                _handle = CreateHandle(this, xProperty, yProperty);
                np.PropertyChanged += _handle;
            }
        }

        public object Item { get; }

        public AxisValue X { get; private set; }
        public AxisValue Y { get; private set; }

        private void UpdateX() {
            X = _xMap(_data);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(X)));
        }

        private void UpdateY() {
            Y = _yMap(_data);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Y)));
        }

        private static PropertyChangedEventHandler CreateHandle(NotifiableDataPoint dp, string xProperty, string yProperty) {
            void handle(object sender, PropertyChangedEventArgs e) {
                if (e.PropertyName == xProperty) {
                    dp.UpdateX();
                }
                if (e.PropertyName == yProperty) {
                    dp.UpdateY();
                }
            }
            return handle;
        }

        protected virtual void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {

                }

                if (_data != null) {
                    _data.PropertyChanged -= _handle;
                    _data = null;
                    _handle = null;
                }
                _disposedValue = true;
            }
        }

        ~NotifiableDataPoint()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
