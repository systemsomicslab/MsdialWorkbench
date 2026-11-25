using CompMs.App.Msdial.Model.DataObj;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using System;

namespace CompMs.App.Msdial.Model.Loader
{
    /// <summary>
    /// The way a sample viewer obtains its provider: a sample whose raw data file is missing gets
    /// <see cref="RawDataUnavailableProvider"/>, which never waits or fails, so the viewer opens with what the project stores.
    /// Processing and exports keep the real factory and say why they cannot run.
    /// </summary>
    internal sealed class BrowsingDataProviderFactory : IDataProviderFactory<AnalysisFileBeanModel>
    {
        private readonly IDataProviderFactory<AnalysisFileBean> _inner;

        public BrowsingDataProviderFactory(IDataProviderFactory<AnalysisFileBean> inner) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public IDataProvider Create(AnalysisFileBeanModel file) {
            return file.RefreshRawDataAvailability() ? _inner.Create(file.File) : RawDataUnavailableProvider.Instance;
        }
    }
}
