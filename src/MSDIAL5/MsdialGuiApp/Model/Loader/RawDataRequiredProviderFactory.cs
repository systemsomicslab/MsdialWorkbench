using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.IO;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class RawDataNotAvailableException : FileNotFoundException
    {
        public RawDataNotAvailableException(string analysisFileName, string analysisFilePath)
            : base($"The raw data file of {analysisFileName} was not found: {analysisFilePath}", analysisFilePath) {
            AnalysisFileName = analysisFileName;
        }

        public string AnalysisFileName { get; }
    }

    /// <summary>
    /// For an export that reads the raw data: a sample whose raw data file is missing stops it at once with
    /// <see cref="RawDataNotAvailableException"/>, instead of the provider retrying the missing file for about 25 seconds.
    /// </summary>
    internal sealed class RawDataRequiredProviderFactory : IDataProviderFactory<AnalysisFileBean>
    {
        private readonly IDataProviderFactory<AnalysisFileBean> _inner;

        public RawDataRequiredProviderFactory(IDataProviderFactory<AnalysisFileBean> inner) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public IDataProvider Create(AnalysisFileBean file) {
            if (!DataAccess.RawDataExists(file.AnalysisFilePath)) {
                throw new RawDataNotAvailableException(file.AnalysisFileName, file.AnalysisFilePath);
            }
            return _inner.Create(file);
        }
    }

    /// <summary>
    /// For an export that reads the raw data only for a column it can leave empty: a sample whose raw data file is missing
    /// gets <see cref="RawDataUnavailableProvider"/>, so the peak table is written from the project with its MS1 isotope column as null.
    /// </summary>
    internal sealed class RawDataOptionalProviderFactory : IDataProviderFactory<AnalysisFileBean>
    {
        private readonly IDataProviderFactory<AnalysisFileBean> _inner;

        public RawDataOptionalProviderFactory(IDataProviderFactory<AnalysisFileBean> inner) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public IDataProvider Create(AnalysisFileBean file) {
            return DataAccess.RawDataExists(file.AnalysisFilePath) ? _inner.Create(file) : RawDataUnavailableProvider.Instance;
        }
    }
}
