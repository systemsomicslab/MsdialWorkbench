using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace ChartDrawingUiTest.Chart
{
    internal sealed class ScatterControlSlimNotifyTestViewModel : ViewModelBase
    {
        public ScatterControlSlimNotifyTestViewModel()
        {
            DataAs = new ObservableCollection<DataPoint>(Enumerable.Range(0, 20).Select(v => new DataPoint { X = v / 20d * 2 * Math.PI, Y = Math.Sin(v / 20d * 2 * Math.PI) }));
            DataBs = new ObservableCollection<DataPointWrapper>(Enumerable.Range(0, 20).Select(v => new DataPointWrapper(new DataPoint { X = v / 20d * 2 * Math.PI, Y = Math.Sin(v / 20d * 2 * Math.PI) })));
        }

        public ObservableCollection<DataPoint> DataAs { get; private set; }

        public ObservableCollection<DataPointWrapper> DataBs { get; private set; }

        public DataPoint SelectedA {
            get => _selectedA;
            set => SetProperty(ref _selectedA, value);
        }
        private DataPoint _selectedA;

        public DataPointWrapper SelectedB {
            get => _selectedB;
            set => SetProperty(ref _selectedB, value);
        }
        private DataPointWrapper _selectedB;

        public IAxisManager<double> HorizontalAxis { get; } = new ContinuousAxisManager<double>(0d, 2 * Math.PI);
        public IAxisManager<double> VerticalAxis { get; } = new ContinuousAxisManager<double>(-1.1d, 1.1d);

        public ICommand UpdateCommand => _updateCommand ?? (_updateCommand = new DelegateCommand(Update));
        private ICommand _updateCommand;

        private void Update() {
            DataAs = new ObservableCollection<DataPoint>(DataAs);
            OnPropertyChanged(nameof(DataAs));
            DataBs = new ObservableCollection<DataPointWrapper>(DataBs);
            OnPropertyChanged(nameof(DataBs));
        }
    }
}
