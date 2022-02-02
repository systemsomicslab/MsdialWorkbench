using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace ChartDrawingUiTest.Behavior
{
    public class AreaSelectBehaviorTestViewModel : ViewModelBase
    {
        public AreaSelectBehaviorTestViewModel() {
            Datas = new ObservableCollection<DataPoint>(Enumerable.Range(0, 360).Select(i => new DataPoint { X = i, Y = Math.Cos(i / 100d), }));

            HorizontalAxis = ContinuousAxisManager<double>.Build(Datas, d => d.X);
            VerticalAxis = ContinuousAxisManager<double>.Build(Datas, d => d.Y);
        }

        public ObservableCollection<DataPoint> Datas { get; }
        public ContinuousAxisManager<double> HorizontalAxis { get; }
        public ContinuousAxisManager<double> VerticalAxis { get; }

        public ObservableCollection<ObservableCollection<object>> SelectedItems {
            get => selectedItems;
            set => SetProperty(ref selectedItems, value);
        }
        private ObservableCollection<ObservableCollection<object>> selectedItems;

        public DelegateCommand StopCommand => stopCommand ?? (stopCommand = new DelegateCommand(Stop));
        private DelegateCommand stopCommand;

        private void Stop() { }
    }
}
