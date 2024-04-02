using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Helper;
using System;
using System.ComponentModel;
using System.Linq;

namespace CompMs.Graphics.Data;

internal sealed class NotifiableDataPoint : INotifyPropertyChanged, IDisposable
{
    private readonly IAxisManager _xAxisManager, _yAxisManager;
    private readonly TypedPropertiesAccessor _xPropertiesAccessor, _yPropertiesAccessor;
    private bool _disposedValue;
    private object[]? _xValues, _yValues;
    private PropertyChangedEventHandler[]? _xHandles, _yHandles;

    public event PropertyChangedEventHandler PropertyChanged;

    public NotifiableDataPoint(object data, IAxisManager xAxisManager, IAxisManager yAxisManager, TypedPropertiesAccessor xPropertiesAccessor, TypedPropertiesAccessor yPropertiesAccessor) {
        Item = data;
        X = xPropertiesAccessor.GetAxisValue(data, xAxisManager);
        Y = yPropertiesAccessor.GetAxisValue(data, yAxisManager);
        _xAxisManager = xAxisManager;
        _yAxisManager = yAxisManager;
        _xPropertiesAccessor = xPropertiesAccessor;
        _yPropertiesAccessor = yPropertiesAccessor;

        _xValues = xPropertiesAccessor.Delegates.Select(d => d.DynamicInvoke(data)).Prepend(data).ToArray();
        _xHandles = CreateHandles(xPropertiesAccessor, _xValues, UpdateX);
        for (int i = 0; i < _xHandles.Length; i++) {
            if (_xValues[i] is INotifyPropertyChanged np) {
                np.PropertyChanged += _xHandles[i];
            }
        }

        _yValues = yPropertiesAccessor.Delegates.Select(d => d.DynamicInvoke(data)).Prepend(data).ToArray();
        _yHandles = CreateHandles(yPropertiesAccessor, _yValues, UpdateY);
        for (int i = 0; i < _yHandles.Length; i++) {
            if (_yValues[i] is INotifyPropertyChanged np) {
                np.PropertyChanged += _yHandles[i];
            }
        }
    }

    public object Item { get; }

    public AxisValue X { get; private set; }
    public AxisValue Y { get; private set; }

    private PropertyChangedEventHandler[] CreateHandles(TypedPropertiesAccessor accessor, object[] values, Action update) {
        var handles = new PropertyChangedEventHandler[accessor.Properties.Length];
        for (int i = 0; i < handles.Length; i++) {
            handles[i] = CreateHandle(i, accessor, values, handles, update);
        }
        return handles;
    }

    private PropertyChangedEventHandler CreateHandle(int depth, TypedPropertiesAccessor accessor, object[] values, PropertyChangedEventHandler[] handles, Action update) {
        var property = accessor.Properties[depth];
        void handle(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == property) {
                for (int i = depth + 1; i < values.Length - 1; i++) {
                    if (values[i] is INotifyPropertyChanged oldValue) {
                        oldValue.PropertyChanged -= handles[i];
                    }
                }
                for (int i = depth; i < accessor.Delegates.Length; i++) {
                    values[i + 1] = accessor.Delegates[i].DynamicInvoke(Item);
                }
                for (int i = depth + 1; i < values.Length - 1; i++) {
                    if (values[i] is INotifyPropertyChanged newValue) {
                        newValue.PropertyChanged += handles[i];
                    }
                }
                update();
            }
        }
        return handle;
    }

    private void UpdateX() {
        if (_disposedValue) {
            return;
        }
        X = _xPropertiesAccessor.GetAxisValue(Item, _xAxisManager);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(X)));
    }

    private void UpdateY() {
        if (_disposedValue) {
            return;
        }
        Y = _yPropertiesAccessor.GetAxisValue(Item, _yAxisManager);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Y)));
    }

    private void Dispose(bool disposing) {
        if (!_disposedValue) {
            if (disposing) {

            }

            for (int i = 0; i < _xHandles.Length; i++) {
                if (_xValues[i] is INotifyPropertyChanged np) {
                    np.PropertyChanged -= _xHandles[i];
                }
            }
            for (int i = 0; i < _yHandles.Length; i++) {
                if (_yValues[i] is INotifyPropertyChanged np) {
                    np.PropertyChanged -= _yHandles[i];
                }
            }
            _xValues = _yValues = null;
            _xHandles = _yHandles = null;
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
