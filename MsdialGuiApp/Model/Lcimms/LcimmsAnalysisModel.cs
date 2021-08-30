using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Lcimms
{
    class LcimmsAnalysisModel : AnalysisModelBase
    {
        public LcimmsAnalysisModel(
            AnalysisFileBean analysisFile,
            IDataProvider provider,
            DataBaseMapper mapper,
            ParameterBase parameter,
            IAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> mspAnnotator,
            IAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> textDBAnnotator)
            : base(analysisFile) {

            this.provider = provider;
            DataBaseMapper = mapper;
            this.parameter = parameter;
            MspAnnotator = mspAnnotator;
            TextDBAnnotator = textDBAnnotator;

            FileName = analysisFile.AnalysisFileName;

            AmplitudeOrderMin = Ms1Peaks.DefaultIfEmpty().Min(peak => peak?.AmplitudeOrderValue) ?? 0;
            AmplitudeOrderMax = Ms1Peaks.DefaultIfEmpty().Max(peak => peak?.AmplitudeOrderValue) ?? 0;

            var labelsource = this.ObserveProperty(m => m.DisplayLabel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            PlotModel = new AnalysisPeakPlotModel(Ms1Peaks, peak => peak.ChromXValue ?? 0, peak => peak.Mass, Target, labelsource)
            {
                HorizontalTitle = "Drift time [1/k0]",
                VerticalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakWrapper.ChromXValue),
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            };
            Target.Select(
                t => t is null
                    ? string.Empty
                    : $"Spot ID: {t.MasterPeakID} Scan: {t.InnerModel.MS1RawSpectrumIdTop} Mass m/z: {t.Mass:N5} RT min: {t.InnerModel.ChromXsTop.RT.Value} Drift time msec: {t.InnerModel.ChromXsTop.Drift.Value}")
                .Subscribe(title => PlotModel.GraphTitle = title);

            EicLoader = new EicLoader(provider, parameter, ChromXType.RT, ChromXUnit.Min, this.parameter.RetentionTimeBegin, this.parameter.RetentionTimeEnd);
            EicModel = new EicModel(Target, EicLoader)
            {
                HorizontalTitle = PlotModel.HorizontalTitle,
                VerticalTitle = "Abundance",
            };
            Target.CombineLatest(
                EicModel.MaxIntensitySource,
                (t, i) => t is null
                    ? string.Empty
                    : $"EIC chromatogram of {t.Mass:N4} tolerance [Da]: {this.parameter.CentroidMs1Tolerance:F} Max intensity: {i:F0}")
                .Subscribe(title => EicModel.GraphTitle = title);

            Ms2SpectrumModel = new RawDecSpectrumsModel(
                Target,
                new MsRawSpectrumLoader(provider, parameter),
                new MsDecSpectrumLoader(decLoader, Ms1Peaks),
                new MsRefSpectrumLoader(mapper),
                peak => peak.Mass,
                peak => peak.Intensity)
            {
                GraphTitle = "Measure vs. Reference",
                HorizontalTitle = "m/z",
                VerticalTitle = "Aubndance",
                HorizontaProperty = nameof(SpectrumPeak.Mass),
                VerticalProperty = nameof(SpectrumPeak.Intensity),
                LabelProperty = nameof(SpectrumPeak.Mass),
                OrderingProperty = nameof(SpectrumPeak.Intensity),
            };

            SurveyScanModel = new SurveyScanModel(
                Target.SelectMany(t =>
                    Observable.DeferAsync(async token => {
                        var result = await LoadMs1SpectrumAsync(t, token);
                        return Observable.Return(result);
                    })),
                spec => spec.Mass,
                spec => spec.Intensity
            ).AddTo(Disposables);
            SurveyScanModel.Elements.VerticalTitle = "Abundance";
            SurveyScanModel.Elements.HorizontalProperty = nameof(SpectrumPeakWrapper.Mass);
            SurveyScanModel.Elements.VerticalProperty = nameof(SpectrumPeakWrapper.Intensity);

            // PeakTableModel = new ImmsAnalysisPeakTableModel(Ms1Peaks, Target, MassMin, MassMax, ChromMin, ChromMax);

            switch (parameter.TargetOmics) {
                case TargetOmics.Lipidomics:
                    Brush = new KeyBrushMapper<ChromatogramPeakFeatureModel, string>(
                        ChemOntologyColor.Ontology2RgbaBrush,
                        peak => peak.Ontology,
                        Color.FromArgb(180, 181, 181, 181));
                    break;
                case TargetOmics.Metabolomics:
                    Brush = new DelegateBrushMapper<ChromatogramPeakFeatureModel>(
                        peak => Color.FromArgb(
                            180,
                            (byte)(255 * peak.InnerModel.PeakShape.AmplitudeScoreValue),
                            (byte)(255 * (1 - Math.Abs(peak.InnerModel.PeakShape.AmplitudeScoreValue - 0.5))),
                            (byte)(255 - 255 * peak.InnerModel.PeakShape.AmplitudeScoreValue)),
                        enableCache: true);
                    break;
            }
            Target.Subscribe(OnTargetChanged);
        }

        private readonly ParameterBase parameter;

        public double RtMin => Ms1Peaks.Min(peak => peak.InnerModel?.ChromXs.RT.Value) ?? 0;
        public double RtMax => Ms1Peaks.Max(peak => peak.InnerModel?.ChromXs.RT.Value) ?? 0;
        public double DriftMin => Ms1Peaks.Min(peak => peak.InnerModel?.ChromXs.Drift.Value) ?? 0;
        public double DriftMax => Ms1Peaks.Max(peak => peak.InnerModel?.ChromXs.Drift.Value) ?? 0;
        public double MassMin => Ms1Peaks.Min(peak => peak.Mass);
        public double MassMax => Ms1Peaks.Max(peak => peak.Mass);
        public double IntensityMin => Ms1Peaks.Min(peak => peak.Intensity);
        public double IntensityMax => Ms1Peaks.Max(peak => peak.Intensity);

        public IBrushMapper<ChromatogramPeakFeatureModel> Brush { get; }
        public AnalysisPeakPlotModel PlotModel { get; }
        public EicLoader EicLoader { get; }
        public EicModel EicModel { get; }
        public RawDecSpectrumsModel Ms2SpectrumModel { get; }
        public SurveyScanModel SurveyScanModel { get; }

        public double AmplitudeOrderMin { get; }
        public double AmplitudeOrderMax { get; }

        private readonly IDataProvider provider;

        public DataBaseMapper DataBaseMapper { get; }
        public IAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> MspAnnotator { get; }
        public IAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> TextDBAnnotator { get; }

        public double Ms1Tolerance => parameter.CentroidMs1Tolerance;

        public string FileName {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        private string fileName;

        public int FocusID {
            get => focusID;
            set => SetProperty(ref focusID, value);
        }
        private int focusID;

        public double FocusDt {
            get => focusDt;
            set => SetProperty(ref focusDt, value);
        }
        private double focusDt;

        public double FocusMz {
            get => focusMz;
            set => SetProperty(ref focusMz, value);
        }
        private double focusMz;

        void OnTargetChanged(ChromatogramPeakFeatureModel target) {
            if (target != null) {
                FocusID = target.InnerModel.MasterPeakID;
                FocusDt = target.ChromXValue ?? 0;
                FocusMz = target.Mass;
            }
        }

        async Task<List<SpectrumPeakWrapper>> LoadMs1SpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            var ms1Spectrum = new List<SpectrumPeakWrapper>();

            if (target != null) {
                await Task.Run(() => {
                    if (target.MS1RawSpectrumIdTop < 0) {
                        return;
                    }
                    var spectra = DataAccess.GetCentroidMassSpectra(provider.LoadMs1Spectrums()[target.MS1RawSpectrumIdTop], parameter.MSDataType, 0, float.MinValue, float.MaxValue);
                    token.ThrowIfCancellationRequested();
                    ms1Spectrum = spectra.Select(peak => new SpectrumPeakWrapper(peak)).ToList();
                }, token);
            }
            return ms1Spectrum;
        }

        public void FocusByID(IAxisManager axis) {
            var focus = Ms1Peaks.FirstOrDefault(peak => peak.InnerModel.MasterPeakID == FocusID);
            Target.Value = focus;
            axis?.Focus(focus.Mass - MzTol, focus.Mass + MzTol);
        }

        private static readonly double MzTol = 20;
        public void FocusByMz(IAxisManager axis) {
            axis?.Focus(FocusMz - MzTol, FocusMz + MzTol);
        }
    }
}
