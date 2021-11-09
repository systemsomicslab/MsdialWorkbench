using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
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
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Lcms
{
    class LcmsAnalysisModel : AnalysisModelBase {
        private readonly IDataProvider provider;

        public LcmsAnalysisModel(
            AnalysisFileBean analysisFile,
            IDataProvider provider,
            DataBaseMapper mapper,
            ParameterBase parameter,
            IReadOnlyList<ISerializableAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> annotators)
            : base(analysisFile) {
            if (analysisFile is null) {
                throw new ArgumentNullException(nameof(analysisFile));
            }

            if (provider is null) {
                throw new ArgumentNullException(nameof(provider));
            }

            if (mapper is null) {
                throw new ArgumentNullException(nameof(mapper));
            }

            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            if (annotators is null) {
                throw new ArgumentNullException(nameof(annotators));
            }

            this.provider = provider;
            DataBaseMapper = mapper;
            Parameter = parameter;
            Annotators = annotators;

            // Peak scatter plot
            var labelSource = this.ObserveProperty(m => m.DisplayLabel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            PlotModel = new AnalysisPeakPlotModel(Ms1Peaks, peak => peak.ChromXValue ?? 0, peak => peak.Mass, Target, labelSource) {
                HorizontalTitle = "Retention time [min]",
                VerticalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakFeatureModel.ChromXValue),
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            };
            Target.Select(
                t => t is null
                    ? string.Empty
                    : $"Spot ID: {t.MasterPeakID} Scan: {t.MS1RawSpectrumIdTop} Mass m/z: {t.Mass:N5}")
                .Subscribe(title => PlotModel.GraphTitle = title);

            // Eic chart
            EicLoader = new EicLoader(this.provider, Parameter, ChromXType.RT, ChromXUnit.Min, Parameter.RetentionTimeBegin, Parameter.RetentionTimeEnd);
            EicModel = new EicModel(Target, EicLoader) {
                HorizontalTitle = PlotModel.HorizontalTitle,
                VerticalTitle = "Abundance",
            };
            Target.CombineLatest(
                EicModel.MaxIntensitySource,
                (t, i) => t is null
                    ? string.Empty
                    : $"EIC chromatogram of {t.Mass:N4} tolerance [Da]: {Parameter.CentroidMs1Tolerance:F} Max intensity: {i:F0}")
                .Subscribe(title => EicModel.GraphTitle = title);

            // Ms2 spectrum
            Ms2SpectrumModel = new RawDecSpectrumsModel(
                Target,
                new MsRawSpectrumLoader(this.provider, Parameter),
                new MsDecSpectrumLoader(decLoader, Ms1Peaks),
                new MsRefSpectrumLoader(mapper),
                peak => peak.Mass,
                peak => peak.Intensity) {
                GraphTitle = "Measure vs. Reference",
                HorizontalTitle = "m/z",
                VerticalTitle = "Relative abundance",
                HorizontaProperty = nameof(SpectrumPeak.Mass),
                VerticalProperty = nameof(SpectrumPeak.Intensity),
                LabelProperty = nameof(SpectrumPeak.Mass),
                OrderingProperty = nameof(SpectrumPeak.Intensity),
            };

            // SurveyScan
            SurveyScanModel = new SurveyScanModel(
                Target.SelectMany(t =>
                    Observable.Defer(() => {
                        if (t is null) {
                            return Observable.Return(new List<SpectrumPeakWrapper>());
                        }
                        var spectra = DataAccess.GetCentroidMassSpectra(
                            this.provider.LoadMs1Spectrums()[t.MS1RawSpectrumIdTop],
                            Parameter.MSDataType, 0, float.MinValue, float.MaxValue);
                        return Observable.Return(spectra.Select(peak => new SpectrumPeakWrapper(peak)).ToList());
                    })),
                spec => spec.Mass,
                spec => spec.Intensity).AddTo(Disposables);
            SurveyScanModel.Elements.VerticalTitle = "Abundance";
            SurveyScanModel.Elements.HorizontalProperty = nameof(SpectrumPeakWrapper.Mass);
            SurveyScanModel.Elements.VerticalProperty = nameof(SpectrumPeakWrapper.Intensity);

            // Peak table
            PeakTableModel = new LcmsAnalysisPeakTableModel(Ms1Peaks, Target, MassMin, MassMax, ChromMin, ChromMax);

            switch (Parameter.TargetOmics) {
                case TargetOmics.Lipidomics:
                    Brush = new KeyBrushMapper<ChromatogramPeakFeatureModel, string>(
                        ChemOntologyColor.Ontology2RgbaBrush,
                        peak => peak?.Ontology ?? string.Empty,
                        Color.FromArgb(180, 181, 181, 181));
                    break;
                case TargetOmics.Metabolomics:
                case TargetOmics.Proteomics:
                    Brush = new DelegateBrushMapper<ChromatogramPeakFeatureModel>(
                        peak => Color.FromArgb(
                            180,
                            (byte)(255 * peak.InnerModel.PeakShape.AmplitudeScoreValue),
                            (byte)(255 * (1 - Math.Abs(peak.InnerModel.PeakShape.AmplitudeScoreValue - 0.5))),
                            (byte)(255 - 255 * peak.InnerModel.PeakShape.AmplitudeScoreValue)),
                        enableCache: true);
                    break;
            }

            CanSearchCompound = new[]
            {
                Target.Select(t => t is null || t.InnerModel is null),
                MsdecResult.Select(r => r is null),
            }.CombineLatestValuesAreAllFalse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public DataBaseMapper DataBaseMapper { get; }
        public ParameterBase Parameter { get; }
        public IReadOnlyList<ISerializableAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> Annotators { get; }
        public EicLoader EicLoader { get; }

        public AnalysisPeakPlotModel PlotModel { get; }

        public EicModel EicModel { get; }

        public RawDecSpectrumsModel Ms2SpectrumModel { get; }

        public SurveyScanModel SurveyScanModel { get; }

        public LcmsAnalysisPeakTableModel PeakTableModel { get; }

        public IBrushMapper<ChromatogramPeakFeatureModel> Brush { get; }

        public double ChromMin => Ms1Peaks.DefaultIfEmpty().Min(peak => peak?.ChromXValue) ?? 0d;
        public double ChromMax => Ms1Peaks.DefaultIfEmpty().Max(peak => peak?.ChromXValue) ?? 0d;
        public double MassMin => Ms1Peaks.DefaultIfEmpty().Min(peak => peak?.Mass) ?? 0d;
        public double MassMax => Ms1Peaks.DefaultIfEmpty().Max(peak => peak?.Mass) ?? 0d;
        public double IntensityMin => Ms1Peaks.DefaultIfEmpty().Min(peak => peak?.Intensity) ?? 0d;
        public double IntensityMax => Ms1Peaks.DefaultIfEmpty().Max(peak => peak?.Intensity) ?? 0d;

        public ReadOnlyReactivePropertySlim<bool> CanSearchCompound { get; }

        public CompoundSearchModel<ChromatogramPeakFeature> CreateCompoundSearchModel() {
            if (Target.Value?.InnerModel is null || MsdecResult.Value is null) {
                return null;
            }

            return new LcmsCompoundSearchModel<ChromatogramPeakFeature>(
                AnalysisFile,
                Target.Value.InnerModel,
                MsdecResult.Value,
                null,
                Annotators);
        }
    }
}
