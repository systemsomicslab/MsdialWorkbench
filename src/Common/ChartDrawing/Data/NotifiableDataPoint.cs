using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Helper;
using System;
using System.ComponentModel;

namespace CompMs.Graphics.Data;

internal class NotifiableDataPoint : INotifyPropertyChanged, IDisposable
{
    private readonly IAxisManager _xAxisManager, _yAxisManager;
    protected bool _disposedValue;
    private ValueHolder? _xValueHolder, _yValueHolder;

    public event PropertyChangedEventHandler PropertyChanged;

    public NotifiableDataPoint(object data, IAxisManager xAxisManager, IAxisManager yAxisManager, IPropertiesAccessor xPropertiesAccessor, IPropertiesAccessor yPropertiesAccessor) {
        _item = data;
        _xValueHolder = new ValueHolder { Item = data, Accessor = xPropertiesAccessor };
        _xValueHolder.PropertyChanged += UpdateX;
        X = xAxisManager.TranslateToAxisValue(_xValueHolder.Value);
        _yValueHolder = new ValueHolder { Item = data, Accessor = yPropertiesAccessor };
        _yValueHolder.PropertyChanged += UpdateY;
        Y = yAxisManager.TranslateToAxisValue(_yValueHolder.Value);
        _xAxisManager = xAxisManager;
        _yAxisManager = yAxisManager;
    }

    public object? Item {
        get => _item;
        set {
            if (_item != value) {
                _item = value;
                _xValueHolder!.Item = value;
                _yValueHolder!.Item = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Item)));
            }
        }
    }
    private object? _item;

    public AxisValue X { get; private set; }
    public AxisValue Y { get; private set; }

    private void UpdateX(object sender, PropertyChangedEventArgs e) {
        if (_disposedValue) {
            return;
        }
        X = _xAxisManager.TranslateToAxisValue(_xValueHolder!.Value);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(X)));
    }

    private void UpdateY(object sender, PropertyChangedEventArgs e) {
        if (_disposedValue) {
            return;
        }
        Y = _yAxisManager.TranslateToAxisValue(_yValueHolder!.Value);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Y)));
    }

    protected virtual void Dispose(bool disposing) {
        if (!_disposedValue) {
            if (disposing) {
                _xValueHolder?.Dispose();
                _yValueHolder?.Dispose();
            }

            if (_xValueHolder is not null) {
                _xValueHolder.PropertyChanged -= UpdateX;
                _xValueHolder = null;
            }
            if (_yValueHolder is not null) {
                _yValueHolder.PropertyChanged -= UpdateY;
                _yValueHolder = null;
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
