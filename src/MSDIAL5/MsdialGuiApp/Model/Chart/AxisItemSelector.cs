using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.Model.Chart
{
    public sealed class AxisItemSelector<T> : DisposableModelBase
    {
        private readonly BehaviorSubject<AxisItemModel<T>> _selectedItemSubject;

        public AxisItemSelector(params AxisItemModel<T>[] axisItems) {
            if (axisItems.Length == 0) {
                throw new ArgumentException($"Argument '{nameof(axisItems)}' should have at least one item.");
            }
            AxisItems = new ReadOnlyObservableCollection<AxisItemModel<T>>(new ObservableCollection<AxisItemModel<T>>(axisItems));
            _selectedAxisItem = axisItems[0];
            _selectedItemSubject = new BehaviorSubject<AxisItemModel<T>>(_selectedAxisItem).AddTo(Disposables);
        }

        public ReadOnlyObservableCollection<AxisItemModel<T>> AxisItems { get; }

        public AxisItemModel<T> SelectedAxisItem {
            get => _selectedAxisItem;
            set {
                // WPF can clear its selection while rebuilding the list. Keep the current axis.
                if (value is not null) {
                    SetProperty(ref _selectedAxisItem, value);
                }
            }
        }
        private AxisItemModel<T> _selectedAxisItem;

        protected override void OnPropertyChanged(PropertyChangedEventArgs args) {
            base.OnPropertyChanged(args);
            if (args.PropertyName == nameof(SelectedAxisItem)) {
                _selectedItemSubject.OnNext(SelectedAxisItem);
            }
        }

        public IObservable<AxisItemModel<T>> GetAxisItemAsObservable() {
            return _selectedItemSubject;
        }

        public void Select(AxisItemModel<T> item) {
            SelectedAxisItem = item;
        }
    }
}
