using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System;

namespace CompMs.App.Msdial.Model.Search
{
    public class ChromSpotFocus : BindableBase, ISpotFocus
    {
        public ChromSpotFocus(string label, IAxisManager<double> axis, double tolerance) {
            this.axis = axis;
            this.tolerance = tolerance;
            Label = label;
        }

        private readonly IAxisManager<double> axis;
        private readonly double tolerance;

        public string Label { get; }

        public double Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
        private double _value;

        public void Focus() {
            axis.Focus(Value - tolerance, Value + tolerance);
        }
    }

    public class IdSpotFocus<T> : BindableBase, ISpotFocus
    {
        private readonly IObserver<T> observer;
        private readonly Func<double, T> selector;
        private readonly (ISpotFocus, Func<T, double>)[] focuses;

        public IdSpotFocus(string label, IObserver<T> observer, Func<double, T> selector, params (ISpotFocus, Func<T, double>)[] focuses) {
            Label = label;
            this.observer = observer;
            this.selector = selector;
            this.focuses = focuses;
        }

        public string Label { get; }

        public double Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
        private double _value;

        public void Focus() {
            var item = selector(Value);
            if (item == null) {
                return;
            }
            observer.OnNext(item);
            foreach ((var focus, var map) in focuses) {
                focus.Value = map(item);
                focus.Focus();
            }
        }
    }
}
