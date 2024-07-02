using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.ObjectModel;

namespace ChartDrawingUiTest.Bar
{
    public class BarViewModel3 : ViewModelBase
    {
        public BarViewModel3() {
            collection = new ObservableCollection<DataPoint>();
            Collection = new ReadOnlyObservableCollection<DataPoint>(collection);
            Number = 0;
            random = new Random();

            AddCommand = new DelegateCommand(Add);
            RemoveCommand = new DelegateCommand(Remove);
            SetCommand = new DelegateCommand(Set);
            ClearCommand = new DelegateCommand(Clear);
            MoveCommand = new DelegateCommand(Move);

            horizontalAxis = new ContinuousAxisManager<double>(new AxisRange(-1 / 2, Number - 1 / 2), new RelativeMargin(0.05));
            VerticalAxis = new ContinuousAxisManager<double>(new AxisRange(0, 1d), new RelativeMargin(0, 0.05));

            Set();
        }

        private readonly Random random;

        public ReadOnlyObservableCollection<DataPoint> Collection { get; }
        private ObservableCollection<DataPoint> collection;

        public IAxisManager<double> HorizontalAxis => horizontalAxis;
        private readonly ContinuousAxisManager<double> horizontalAxis;

        public IAxisManager<double> VerticalAxis { get; }

        public double Number {
            get => number;
            set {
                if (SetProperty(ref number, value)) {
                    horizontalAxis?.UpdateInitialRange(new AxisRange(-1 / 2, Number - 1 / 2));
                }
            }
        }
        private double number;

        public DelegateCommand AddCommand { get; }

        private void Add() {
            collection.Add(new DataPoint { X = Collection.Count, Y = random.NextDouble(), });
            ++Number;
        }

        public DelegateCommand RemoveCommand { get; }

        private void Remove() {
            if (collection.Count >= 1) {
                collection.Remove(collection[0]);
                for (int i = 0; i < collection.Count; i++) {
                    collection[i].X = i;
                }
                --Number;
            }
        }

        public double NewNumber {
            get => newNumber;
            set => SetProperty(ref newNumber, value);
        }
        private double newNumber = 0;

        public DelegateCommand SetCommand { get; }

        private void Set() {
            if (NewNumber == 0) {
                collection.Clear();
                Number = 0;
                return;
            }

            while (NewNumber < collection.Count) {
                collection.RemoveAt(0);
            }
            while (NewNumber > collection.Count) {
                collection.Add(new DataPoint { X = collection.Count, Y = random.NextDouble() });
            }
            for (int i = 0; i < collection.Count; i++) {
                collection[i].X = i;
            }
            Number = NewNumber;
        }

        public DelegateCommand ClearCommand { get; }

        private void Clear() {
            collection.Clear();
            Number = 0;
        }

        public DelegateCommand MoveCommand { get; }

        private void Move() {
            if (collection.Count >= 2) {
                collection.Move(0, 1);
                collection[0].X = 0;
                collection[1].X = 1;
            }
        }
    }
}
