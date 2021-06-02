using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Imms
{
    class ImmsAlignmentModel : BindableBase, IDisposable {
        private bool disposedValue;

        public ImmsAlignmentModel(
            AlignmentFileBean alignmentFileBean,
            ParameterBase param,
            IMatchResultRefer refer,
            IAnnotator<AlignmentSpotProperty, MSDecResult> mspAnnotator, IAnnotator<AlignmentSpotProperty, MSDecResult> textDBAnnotator) {

            var fileName = alignmentFileBean.FileName;
            var resultFile = alignmentFileBean.FilePath;

            var Container = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(resultFile);

            Ms1Spots = new ObservableCollection<AlignmentSpotPropertyModel>(
                Container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyModel(prop, param.FileID_ClassName)));

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
            Target
                .Subscribe(async t => await OnTargetChangedAsync(t));

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
        }

        public Chart.AlignmentPeakPlotModel PlotModel { get; }

        public Chart.MsSpectrumModel Ms2SpectrumModel { get; }

        public ObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }

        public ReadOnlyReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }

        private async Task OnTargetChangedAsync(AlignmentSpotPropertyModel target) {
            await Task.WhenAll(
                ).ConfigureAwait(false);
        }

        private readonly CompositeDisposable disposables = new CompositeDisposable();

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
