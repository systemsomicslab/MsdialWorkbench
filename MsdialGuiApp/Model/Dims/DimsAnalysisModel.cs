using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Dims
{
    class DimsAnalysisModel : AnalysisModelBase
    {
        public DimsAnalysisModel(
            AnalysisFileBean analysisFile,
            IDataProvider provider,
            DataBaseMapper mapper,
            ParameterBase parameter,
            IReadOnlyList<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> annotatorContainers)
            : base(analysisFile) {

            FileName = analysisFile.AnalysisFileName;
            DataBaseMapper = mapper;
            Parameter = parameter;
            AnnotatorContainers = annotatorContainers;
            var labelSource = this.ObserveProperty(m => m.DisplayLabel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            PlotModel = new AnalysisPeakPlotModel(Ms1Peaks, peak => peak.Mass, peak => peak.KMD, Target, labelSource)
            {
                VerticalTitle = "Kendrick mass defect",
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.KMD),
                HorizontalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            };
            Target.Select(t => t is null ? string.Empty : $"Spot ID: {t.MasterPeakID} Scan: {t.MS1RawSpectrumIdTop} Mass m/z: {t.Mass:N5}")
                .Subscribe(title => PlotModel.GraphTitle = title);

            EicLoader = new DimsEicLoader(provider, parameter, parameter.MassRangeBegin, parameter.MassRangeEnd);
            EicModel = new EicModel(Target, EicLoader)
            {
                HorizontalTitle = "m/z",
                VerticalTitle = "Abundance"
            };
            Target.CombineLatest(
                EicModel.MaxIntensitySource,
                (t, i) => t is null
                    ? string.Empty
                    : $"EIC chromatogram of {t.Mass:N4} tolerance [Da]: {Parameter.CentroidMs1Tolerance:F} Max intensity: {i:F0}")
                .Subscribe(title => EicModel.GraphTitle = title);

            Ms2SpectrumModel = new RawDecSpectrumsModel(
                Target,
                new MsRawSpectrumLoader(provider, Parameter),
                new MsDecSpectrumLoader(decLoader, Ms1Peaks),
                new MsRefSpectrumLoader(mapper),
                peak => peak.Mass,
                peak => peak.Intensity)
            {
                GraphTitle = "Measure vs. Reference",
                HorizontalTitle = "m/z",
                VerticalTitle = "Relative abundance",
                HorizontaProperty = nameof(SpectrumPeak.Mass),
                VerticalProperty = nameof(SpectrumPeak.Intensity),
                LabelProperty = nameof(SpectrumPeak.Mass),
                OrderingProperty = nameof(SpectrumPeak.Intensity),
            };

            PeakTableModel = new DimsAnalysisPeakTableModel(Ms1Peaks, Target, MassMin, MassMax);

            switch (parameter.TargetOmics) {
                case TargetOmics.Lipidomics:
                    Brush = new KeyBrushMapper<ChromatogramPeakFeatureModel, string>(
                        ChemOntologyColor.Ontology2RgbaBrush,
                        peak => peak?.Ontology ?? string.Empty,
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
        }

        public DataBaseMapper DataBaseMapper { get; }
        public ParameterBase Parameter { get; }
        public IReadOnlyList<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> AnnotatorContainers { get; }

        public string FileName {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        private string fileName;

        public double MassMin => Ms1Peaks.Min(peak => peak.Mass);
        public double MassMax => Ms1Peaks.Max(peak => peak.Mass);

        public AnalysisPeakPlotModel PlotModel { get; }

        public EicModel EicModel { get; }

        public RawDecSpectrumsModel Ms2SpectrumModel { get; }

        public DimsAnalysisPeakTableModel PeakTableModel { get; }

        public IBrushMapper<ChromatogramPeakFeatureModel> Brush { get; }

        public EicLoader EicLoader { get; }

        public string RawSplashKey {
            get => rawSplashKey;
            set => SetProperty(ref rawSplashKey, value);
        }
        private string rawSplashKey = string.Empty;

        public string DeconvolutionSplashKey {
            get => deconvolutionSplashKey;
            set => SetProperty(ref deconvolutionSplashKey, value);
        }
        private string deconvolutionSplashKey = string.Empty;

        private static readonly double MzTol = 20;
        public void FocusByMz(IAxisManager axis, double mz) {
            axis?.Focus(mz - MzTol, mz + MzTol);
        }       

        public void FocusById(IAxisManager mzAxis, int id) {
            var focus = Ms1Peaks.FirstOrDefault(peak => peak.InnerModel.MasterPeakID == id);
            Target.Value = focus;
            FocusByMz(mzAxis, focus.Mass);
        }

        public bool CanSaveSpectra() => Target.Value.InnerModel != null && MsdecResult.Value != null;

        public void SaveSpectra(Stream stream, ExportSpectraFileFormat format) {
            SpectraExport.SaveSpectraTable(
                format,
                stream,
                Target.Value.InnerModel,
                MsdecResult.Value,
                Parameter);
        }

        public void SaveSpectra(string filename) {
            var format = (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.'));
            using (var file = File.Open(filename, FileMode.Create)) {
                SaveSpectra(file, format);
            }
        }

        public void CopySpectrum() {
            var memory = new MemoryStream();
            SaveSpectra(memory, ExportSpectraFileFormat.msp);
            Clipboard.SetText(System.Text.Encoding.UTF8.GetString(memory.ToArray()));
        }
    }
}
