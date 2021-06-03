using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Imms
{
    class ImmsAlignmentModel : BindableBase, IDisposable {
        public ImmsAlignmentModel(
            AlignmentFileBean alignmentFileBean,
            ParameterBase parameter,
            IMatchResultRefer refer,
            IAnnotator<AlignmentSpotProperty, MSDecResult> mspAnnotator,
            IAnnotator<AlignmentSpotProperty, MSDecResult> textDBAnnotator) {

            resultFile = alignmentFileBean.FilePath;
            container = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(resultFile);
            Ms1Spots = new ObservableCollection<AlignmentSpotPropertyModel>(
                container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyModel(prop, parameter.FileID_ClassName)));

            var fileName = alignmentFileBean.FileName;
            PlotModel = new Chart.AlignmentPeakPlotModel(Ms1Spots, spot => spot.TimesCenter, spot => spot.MassCenter)
            {
                GraphTitle = fileName,
                HorizontalProperty = nameof(AlignmentSpotProperty.TimesCenter),
                VerticalProperty = nameof(AlignmentSpotProperty.MassCenter),
                HorizontalTitle = "Drift time [1/k0]",
                VerticalTitle = "m/z",
            };

            Target = PlotModel
                .ObserveProperty(m => m.Target)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(disposables);

            var decLoader = new MsDecSpectrumLoader(alignmentFileBean.SpectraFilePath, Ms1Spots).AddTo(disposables);
            var refLoader = new MsRefSpectrumLoader(refer);
            Ms2SpectrumModel = Chart.MsSpectrumModel.Create(
                Target, decLoader, refLoader,
                peak => peak.Mass,
                peak => peak.Intensity);
            Ms2SpectrumModel.GraphTitle = "Representation vs. Reference";
            Ms2SpectrumModel.HorizontalTitle = "m/z";
            Ms2SpectrumModel.VerticalTitle = "Abundance";
            Ms2SpectrumModel.HorizontalProperty = nameof(SpectrumPeak.Mass);
            Ms2SpectrumModel.VerticalProperty = nameof(SpectrumPeak.Intensity);

            var barLoader = new HeightBarItemsLoader(parameter.FileID_ClassName);
            BarChartModel = Chart.BarChartModel.Create(
                Target, barLoader,
                item => item.Class,
                item => item.Height);
            BarChartModel.Elements.HorizontalTitle = "Class";
            BarChartModel.Elements.VerticalTitle = "Height";
            BarChartModel.Elements.HorizontalProperty = nameof(BarItem.Class);
            BarChartModel.Elements.VerticalProperty = nameof(BarItem.Height);

            var eicFile = alignmentFileBean.EicFilePath;
            var eicLoader = new AlignmentEicLoader(chromatogramSpotSerializer, eicFile, parameter.FileID_ClassName);
            AlignmentEicModel = Chart.AlignmentEicModel.Create(
                Target, eicLoader,
                peak => peak.Time,
                peak => peak.Intensity);
            AlignmentEicModel.Elements.GraphTitle = "TIC, EIC, or BPC chromatograms";
            AlignmentEicModel.Elements.HorizontalTitle = "Drift time [1/k0]";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);
        }

        static ImmsAlignmentModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Drift);
        }

        public Chart.AlignmentPeakPlotModel PlotModel { get; }

        public ObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }

        public ReadOnlyReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }

        public Chart.MsSpectrumModel Ms2SpectrumModel { get; }

        public Chart.BarChartModel BarChartModel { get; }

        public Chart.AlignmentEicModel AlignmentEicModel { get; }

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;
        private readonly AlignmentResultContainer container;
        private readonly string resultFile;

        public void SaveProject() {
            MessagePackHandler.SaveToFile(container, resultFile);
        }

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                    disposables.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
