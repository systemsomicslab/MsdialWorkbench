using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.Model.Chart
{
    public sealed class AxisItemSelector<T> : DisposableModelBase
    {
        private Subject<AxisItemModel<T>> _selectedItemSubject;

        public AxisItemSelector(params AxisItemModel<T>[] axisItems) {
            if (axisItems.Length == 0) {
                throw new ArgumentException($"Argument '{nameof(axisItems)}' should have at least one item.");
            }
            AxisItems = new ObservableCollection<AxisItemModel<T>>(axisItems);
            _selectedItemSubject = new Subject<AxisItemModel<T>>().AddTo(Disposables);
            _selectedAxisItem = axisItems.First();

            OnPropertyChanged(nameof(SelectedAxisItem));
        }

        public ObservableCollection<AxisItemModel<T>> AxisItems { get; }

        public AxisItemModel<T> SelectedAxisItem {
            get => _selectedAxisItem;
            set => SetProperty(ref _selectedAxisItem, value);
        }
        private AxisItemModel<T> _selectedAxisItem;

        protected override void OnPropertyChanged(PropertyChangedEventArgs args) {
            base.OnPropertyChanged(args);
            if (args.PropertyName == nameof(SelectedAxisItem)) {
                _selectedItemSubject.OnNext(SelectedAxisItem);
            }
        }

        public IObservable<AxisItemModel<T>> GetAxisItemAsObservable() {
            return _selectedItemSubject.AsObservable();
        }
    }
}
