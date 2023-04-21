using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class Ms1BasedSpectrumFeatureCollection : BindableBase, IDisposable
    {
        private bool _disposedValue;

        public Ms1BasedSpectrumFeatureCollection(SpectrumFeatureCollection spectrumFeatures) {
            Items = new ObservableCollection<Ms1BasedSpectrumFeature>(spectrumFeatures.Items.Select(item => new Ms1BasedSpectrumFeature(item)));
        }

        public ObservableCollection<Ms1BasedSpectrumFeature> Items { get; }

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    foreach (var item in Items) {
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
