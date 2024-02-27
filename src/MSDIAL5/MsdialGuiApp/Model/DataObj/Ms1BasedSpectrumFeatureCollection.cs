using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class Ms1BasedSpectrumFeatureCollection : BindableBase, IDisposable
    {
        private bool _disposedValue;
        private readonly SpectrumFeatureCollection _spectrumFeatures;
        private readonly ObservableCollection<Ms1BasedSpectrumFeature> _items;

        public Ms1BasedSpectrumFeatureCollection(SpectrumFeatureCollection spectrumFeatures) {
            _spectrumFeatures = spectrumFeatures;
            _items = new ObservableCollection<Ms1BasedSpectrumFeature>(spectrumFeatures.Items.Select(item => new Ms1BasedSpectrumFeature(item)));
            Items = new ReadOnlyObservableCollection<Ms1BasedSpectrumFeature>(_items);
            SelectedSpectrum = new ReactivePropertySlim<Ms1BasedSpectrumFeature?>();
        }

        public ReadOnlyObservableCollection<Ms1BasedSpectrumFeature> Items { get; }

        public ReactivePropertySlim<Ms1BasedSpectrumFeature?> SelectedSpectrum { get; }

        public Task SaveAsync(AnalysisFileBeanModel file) {
            return Task.Run(() => file.File.SaveSpectrumFeatures(_spectrumFeatures));
        }

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    SelectedSpectrum.Dispose();
                    foreach (var item in _items) {
                        item?.Dispose();
                    }
                }
                _disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
