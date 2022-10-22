using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
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

        public WholeImageResultModel(AnalysisFileBeanModel file) {
            File = file ?? throw new ArgumentNullException(nameof(file));
            _peaks = ChromatogramPeakFeatureCollection.LoadAsync(file.File.PeakAreaBeanInformationFilePath, default).Result;
            Peaks = new ObservableCollection<ChromatogramPeakFeatureModel>(_peaks.Items.Select(item => new ChromatogramPeakFeatureModel(item)));
            var intensityBrush = new BrushMapData<ChromatogramPeakFeatureModel>(
                    new DelegateBrushMapper<ChromatogramPeakFeatureModel>(
                        peak => Color.FromArgb(
                            180,
                            (byte)(255 * peak.InnerModel.PeakShape.AmplitudeScoreValue),
                            (byte)(255 * (1 - Math.Abs(peak.InnerModel.PeakShape.AmplitudeScoreValue - 0.5))),
                            (byte)(255 - 255 * peak.InnerModel.PeakShape.AmplitudeScoreValue)),
                        enableCache: true),
                    "Abundance");
            var brushes = new[] { intensityBrush, };
            Target = new ReactiveProperty<ChromatogramPeakFeatureModel>().AddTo(Disposables);
            PeakPlotModel = new AnalysisPeakPlotModel(
                Peaks,
                peak => peak.ChromXValue ?? 0d,
                peak => peak.Mass,
                Target,
                Observable.Return(string.Empty),
                intensityBrush,
                brushes) {
                HorizontalTitle = "Mobility [1/K0]",
                VerticalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakWrapper.ChromXValue),
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            }.AddTo(Disposables);
        }

        public AnalysisFileBeanModel File { get; }
        public ObservableCollection<ChromatogramPeakFeatureModel> Peaks { get; }
        public AnalysisPeakPlotModel PeakPlotModel { get; }
        public ReactiveProperty<ChromatogramPeakFeatureModel> Target { get; }

        public List<(Raw2DElement, ChromatogramPeakFeatureModel)> GetTargetElements() {
            return Peaks.Select(item => (new Raw2DElement(item.Mass, item.InnerModel.ChromXsTop.Drift.Value), item)).ToList();
        }
    }
}
