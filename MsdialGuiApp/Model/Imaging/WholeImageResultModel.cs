using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class WholeImageResultModel : DisposableModelBase
    {
        private readonly ChromatogramPeakFeatureCollection _peaks;

        public WholeImageResultModel(AnalysisFileBean file) {
            File = file ?? throw new System.ArgumentNullException(nameof(file));
            _peaks = ChromatogramPeakFeatureCollection.LoadAsync(file.PeakAreaBeanInformationFilePath, default).Result;
            Peaks = new ObservableCollection<ChromatogramPeakFeatureModel>(_peaks.Items.Select(item => new ChromatogramPeakFeatureModel(item)));
            PeakPlotModel = new AnalysisPeakPlotModel(
                Peaks,
                peak => peak.ChromXValue ?? 0d,
                peak => peak.Mass,
                new ReactiveProperty<ChromatogramPeakFeatureModel>().AddTo(Disposables),
                Observable.Return(string.Empty),
                new BrushMapData<ChromatogramPeakFeatureModel>(new ConstantBrushMapper<ChromatogramPeakFeatureModel>(Brushes.Gray), "Test"),
                new List<BrushMapData<ChromatogramPeakFeatureModel>> { }) {
                HorizontalTitle = "Mobility [1/K0]",
                VerticalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakWrapper.ChromXValue),
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            }.AddTo(Disposables);
        }

        public AnalysisFileBean File { get; }
        public ObservableCollection<ChromatogramPeakFeatureModel> Peaks { get; }
        public AnalysisPeakPlotModel PeakPlotModel { get; }

        public List<Raw2DElement> GetTargetElements() {
            return _peaks.Items.Select(item => new Raw2DElement(item.Mass, item.ChromXsTop.Drift.Value)).ToList();
        }
    }
}
