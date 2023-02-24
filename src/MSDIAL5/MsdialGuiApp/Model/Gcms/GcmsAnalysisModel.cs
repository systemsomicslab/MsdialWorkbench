using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsAnalysisModel : BindableBase, IAnalysisModel, IDisposable
    {
        private bool _disposedValue;
        private readonly Ms1BasedSpectrumFeatureCollection _spectrumFeatures;

        public GcmsAnalysisModel(AnalysisFileBeanModel file) {
            _spectrumFeatures = file.LoadMs1BasedSpectrumFeatureCollection();
        }

        // IAnalysisModel interface
        Task IAnalysisModel.SaveAsync(CancellationToken token) {
            throw new NotImplementedException();
        }

        // IResultModel interface
        void IResultModel.InvokeMsfinder() {
            throw new NotImplementedException();
        }

        void IResultModel.SearchFragment() {
            throw new NotImplementedException();
        }

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~GcmsAnalysisModel()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        void IDisposable.Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
