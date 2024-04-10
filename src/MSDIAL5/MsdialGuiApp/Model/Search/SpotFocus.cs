using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;

namespace CompMs.App.Msdial.Model.Search
{
    public class ChromSpotFocus : DisposableModelBase, ISpotFocus
    {
        public ChromSpotFocus(IAxisManager<double> axis, double tolerance, IObservable<double> update, string format, string label, bool isItalic) {
            this.axis = axis;
            this.tolerance = tolerance;
            Format = format;
            Label = label;
            IsItalic = isItalic;
            update.Subscribe(v => Value = v).AddTo(Disposables);
        }

        private readonly IAxisManager<double> axis;
        private readonly double tolerance;

        public double Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
        private double _value;
        public string Label { get; }
        public bool IsItalic { get; }
        public string Format { get; }

        public void Focus() {
            axis.Focus(Value - tolerance, Value + tolerance);
        }
    }

    public class IdSpotFocus<T> : DisposableModelBase, ISpotFocus
    {
        private readonly IReactiveProperty<T?> property;
        private readonly Func<double, T> selector;
        private readonly (ISpotFocus, Func<T, double>)[] focuses;

        public IdSpotFocus(IReactiveProperty<T?> property, Func<double, T> selector, IObservable<double> update, string label, params (ISpotFocus, Func<T, double>)[] focuses) {
            Label = label;
            this.property = property;
            this.selector = selector;
            this.focuses = focuses;
            update.Subscribe(v => Value = v).AddTo(Disposables);
        }

        public double Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
        private double _value;

        public string Label { get; }

        public bool IsItalic { get; } = false;

        public string Format { get; } = "F0";

        public void Focus() {
            var item = selector(Value);
            if (item == null) {
                return;
            }
            property.Value = item;
            foreach ((var focus, var map) in focuses) {
                focus.Value = map(item);
                focus.Focus();
            }
        }
    }
}
