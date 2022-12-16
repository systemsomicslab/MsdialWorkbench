using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Utility;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class WholeImageResultModel : DisposableModelBase
    {
        private readonly ChromatogramPeakFeatureCollection _peaks;
        private readonly ObservableCollection<IntensityImageModel> _intensities;
        private int _roiId = 0;

        public WholeImageResultModel(AnalysisFileBeanModel file) {
            File = file ?? throw new ArgumentNullException(nameof(file));
            _peaks = ChromatogramPeakFeatureCollection.LoadAsync(file.PeakAreaBeanInformationFilePath, default).Result;
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
            Target = new ReactiveProperty<ChromatogramPeakFeatureModel>(initialValue: Peaks.FirstOrDefault()).AddTo(Disposables);
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
                HorizontalProperty = nameof(ChromatogramPeakFeatureModel.ChromXValue),
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            }.AddTo(Disposables);

            var maldiFrameInfos = new MaldiFrames(file.File.GetMaldiFrames());
            var wholeRoi = new RoiModel(file, maldiFrameInfos, Colors.Red);
            var elements = Peaks.Select(item => new Raw2DElement(item.Mass, item.Drift.Value)).ToList();
            var rawSpectraOnPixels = wholeRoi.RetrieveRawSpectraOnPixels(elements);
            ImagingRoiModel = new ImagingRoiModel($"ROI{_roiId++}", wholeRoi, rawSpectraOnPixels, Peaks, Target).AddTo(Disposables);

            MaldiFrameLaserInfo laserInfo = file.File.GetMaldiFrameLaserInfo();
            _intensities = new ObservableCollection<IntensityImageModel>(
                Peaks.Zip(rawSpectraOnPixels.PixelPeakFeaturesList,
                    (peak, pixelPeaks) => new IntensityImageModel(pixelPeaks, maldiFrameInfos, peak, laserInfo)));
            Intensities = new ReadOnlyObservableCollection<IntensityImageModel>(_intensities);
            Target.Select(p => _intensities.FirstOrDefault(intensity => intensity.Peak == p))
                .SkipNull()
                .Subscribe(intensity => SelectedPeakIntensities = intensity)
                .AddTo(Disposables);
        }

        public AnalysisFileBeanModel File { get; }
        public ObservableCollection<ChromatogramPeakFeatureModel> Peaks { get; }
        public AnalysisPeakPlotModel PeakPlotModel { get; }
        public ReactiveProperty<ChromatogramPeakFeatureModel> Target { get; }
        public ImagingRoiModel ImagingRoiModel { get; }
        public ReadOnlyObservableCollection<IntensityImageModel> Intensities { get; }
        public IntensityImageModel SelectedPeakIntensities {
            get => _selectedPeakIntensities;
            set => SetProperty(ref _selectedPeakIntensities, value);
        }
        private IntensityImageModel _selectedPeakIntensities;
    }
}
