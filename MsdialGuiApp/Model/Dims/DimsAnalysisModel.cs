using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Dims
{
    class DimsAnalysisModel : AnalysisModelBase
    {
        public DimsAnalysisModel(
            AnalysisFileBean analysisFile,
            IDataProvider provider,
            IMatchResultRefer refer,
            ParameterBase parameter,
            IAnnotator<IMSIonProperty, IMSScanProperty> mspAnnotator,
            IAnnotator<IMSIonProperty, IMSScanProperty> textDBAnnotator) {

            MspAnnotator = mspAnnotator;
            TextDBAnnotator = textDBAnnotator;

            AnalysisFile = analysisFile;
            FileName = analysisFile.AnalysisFileName;
            Parameter = parameter;

            var peaks = MsdialSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
            Ms1Peaks = new ObservableCollection<ChromatogramPeakFeatureModel>(
                peaks.Select(peak => new ChromatogramPeakFeatureModel(peak, parameter.TargetOmics != TargetOmics.Metabolomics)));

            var labelSource = this.ObserveProperty(m => m.DisplayLabel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            PlotModel = new Chart.AnalysisPeakPlotModel(Ms1Peaks, peak => peak.Mass, peak => peak.KMD, labelSource)
            {
                VerticalTitle = "Kendrick mass defect",
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.KMD),
                HorizontalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            };

            Target = PlotModel.ToReactivePropertySlimAsSynchronized(m => m.Target);

            EicLoader = new DimsEicLoader(provider, parameter, parameter.MassRangeBegin, parameter.MassRangeEnd);
            EicModel = new Chart.EicModel(Target, EicLoader)
            {
                HorizontalTitle = "m/z",
                VerticalTitle = "Abundance"
            };

            var loader = new MSDecLoader(analysisFile.DeconvolutionFilePath).AddTo(Disposables);
            Ms2SpectrumModel = new Chart.RawDecSpectrumsModel(
                Target,
                new MsRawSpectrumLoader(provider, Parameter),
                new MsDecSpectrumLoader(loader, Ms1Peaks),
                new MsRefSpectrumLoader(refer),
                peak => peak.Mass,
                peak => peak.Intensity)
            {
                GraphTitle = "Measure vs. Reference",
                HorizontalTitle = "m/z",
                VerticalTitle = "Abundance",
                HorizontaProperty = nameof(SpectrumPeak.Mass),
                VerticalProperty = nameof(SpectrumPeak.Intensity),
                LabelProperty = nameof(SpectrumPeak.Mass),
                OrderingProperty = nameof(SpectrumPeak.Intensity),
            };

            PeakTableModel = new DimsAnalysisPeakTableModel(Ms1Peaks, Target, MassMin, MassMax);

            MsdecResult = Target.Where(t => t != null)
                .Select(t => loader.LoadMSDecResult(t.MasterPeakID))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

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
        }

        public AnalysisFileBean AnalysisFile { get; }
        public ParameterBase Parameter { get; }

        public IAnnotator<IMSIonProperty, IMSScanProperty> MspAnnotator { get; }
        public IAnnotator<IMSIonProperty, IMSScanProperty> TextDBAnnotator { get; }

        public string FileName {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        private string fileName;

        public ObservableCollection<ChromatogramPeakFeatureModel> Ms1Peaks { get; }

        public double MassMin => Ms1Peaks.Min(peak => peak.Mass);
        public double MassMax => Ms1Peaks.Max(peak => peak.Mass);

        public ReactivePropertySlim<ChromatogramPeakFeatureModel> Target { get; }

        public Chart.AnalysisPeakPlotModel PlotModel { get; }

        public Chart.EicModel EicModel { get; }

        public Chart.RawDecSpectrumsModel Ms2SpectrumModel { get; }

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

        public ReadOnlyReactivePropertySlim<MSDecResult> MsdecResult { get; }

        private static readonly double MzTol = 20;
        public void FocusByMz(IAxisManager axis, double mz) {
            axis?.Focus(mz - MzTol, mz + MzTol);
        }       

        public void FocusById(IAxisManager mzAxis, int id) {
            var focus = Ms1Peaks.FirstOrDefault(peak => peak.InnerModel.MasterPeakID == id);
            Target.Value = focus;
            FocusByMz(mzAxis, focus.Mass);
        }

        public void SaveSpectra(string filename) {
            SpectraExport.SaveSpectraTable(
                (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.')),
                filename,
                Target.Value.InnerModel,
                MsdecResult.Value,
                Parameter);
        }

        public bool CanSaveSpectra() => Target.Value.InnerModel != null && MsdecResult.Value != null;
    }
}
