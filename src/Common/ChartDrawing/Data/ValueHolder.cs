using CompMs.Graphics.Helper;
using System;
using System.ComponentModel;
using System.Linq;

namespace CompMs.Graphics.Data;

internal sealed class ValueHolder : INotifyPropertyChanged, IDisposable
{
    private bool _disposedValue;
    private object[]? _values;
    private PropertyChangedEventHandler[]? _handles;

    public event PropertyChangedEventHandler PropertyChanged;

    public ValueHolder() {
        _accessor = IdentityAccessor.Instance;
        InitHandles();
    }

    public object? Item {
        get => _item;
        set {
            if (_item != value) {
                _item = value;
                InitHandles();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Item)));
            }
        }
    }
    private object? _item;

    public object? Value {
        get => _value;
        private set {
            if (_value != value) {
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }
    private object? _value;

    public IPropertiesAccessor Accessor {
        get => _accessor;
        set {
            if (_accessor != value) {
                _accessor = value;
                InitHandles();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Accessor)));
            }
        }
    }
    private IPropertiesAccessor _accessor;

    private void InitHandles() {
        DetachHandles(0);
        _values = Item is null
            ? Enumerable.Repeat((object)null, Accessor.Properties.Length + 1).ToArray()
            : Enumerable.Range(0, Accessor.Properties.Length).Select(i => Accessor.Apply(i, Item)).Prepend(Item).ToArray();
        CreateHandles();
        AttachHandles(0);
        Value = _values.Last();
    }

    private void CreateHandles() {
        _handles = new PropertyChangedEventHandler[_accessor.Properties.Length];
        for (int i = 0; i < _handles.Length; i++) {
            _handles[i] = CreateHandle(i);
        }
    }

    private PropertyChangedEventHandler CreateHandle(int depth) {
        var property = _accessor.Properties[depth];
        void handle(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == property) {
                DetachHandles(depth + 1);
                for (int i = depth; i < _accessor.Properties.Length; i++) {
                    _values[i + 1] = _accessor.Apply(i, Item);
                }
                AttachHandles(depth + 1);
                Update();
            }
        }
        return handle;
    }

    private void AttachHandles(int from) {
        if (_handles is null) {
            return;
        }
        for (int i = from; i < _handles.Length; i++) {
            if (_values[i] is INotifyPropertyChanged np) {
                np.PropertyChanged += _handles[i];
            }
        }
    }

    private void DetachHandles(int from) {
        if (_handles is null) {
            return;
        }
        for (int i = from; i < _handles.Length; i++) {
            if (_values[i] is INotifyPropertyChanged np) {
                np.PropertyChanged -= _handles[i];
            }
        }
    }

    private void Update() {
        if (_disposedValue) {
            return;
        }
        Value = Item is null ? null : Accessor.Apply(Item);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }

    private void Dispose(bool disposing) {
        if (!_disposedValue) {
            if (disposing) {

            }

            DetachHandles(0);
            _values = null;
            _handles = null;
            _disposedValue = true;
        }
    }

    ~ValueHolder()
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
