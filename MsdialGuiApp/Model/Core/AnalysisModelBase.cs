using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Core
{
    internal class AnalysisModelBase : BindableBase, IDisposable
    {
        public AnalysisModelBase(AnalysisFileBean analysisFile) {
            if (analysisFile is null) {
                throw new ArgumentNullException(nameof(analysisFile));
            }

            AnalysisFile = analysisFile;
            var peaks = MsdialSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
            Ms1Peaks = new ObservableCollection<ChromatogramPeakFeatureModel>(
                peaks.Select(peak => new ChromatogramPeakFeatureModel(peak))
            );

            Target = new ReactivePropertySlim<ChromatogramPeakFeatureModel>().AddTo(Disposables);

            decLoader = new MSDecLoader(analysisFile.DeconvolutionFilePath).AddTo(Disposables);
            MsdecResult = Target.Where(t => !(t is null))
                .Select(t => decLoader.LoadMSDecResult(t.MasterPeakID))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        protected readonly MSDecLoader decLoader;

        public AnalysisFileBean AnalysisFile { get; }

        public ObservableCollection<ChromatogramPeakFeatureModel> Ms1Peaks { get; }

        public ReactivePropertySlim<ChromatogramPeakFeatureModel> Target { get; }

        public ReadOnlyReactivePropertySlim<MSDecResult> MsdecResult { get; }

        public string DisplayLabel {
            get => displayLabel;
            set => SetProperty(ref displayLabel, value);
        }
        private string displayLabel = string.Empty;

        // IDisposable fields and methods
        protected CompositeDisposable Disposables = new CompositeDisposable();
        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    Disposables.Dispose();
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
