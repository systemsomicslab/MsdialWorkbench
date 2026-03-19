using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Threading;

namespace CompMs.MsdialCore.Algorithm
{
    public class StandardDataProvider : BaseDataProvider
    {
        public StandardDataProvider(IEnumerable<RawSpectrum> spectrums) : base(spectrums) {

        }

        public StandardDataProvider(RawMeasurement rawObj) : this(rawObj.SpectrumList) {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardDataProvider"/> class.
        /// This constructor loads raw measurement data asynchronously based on the provided parameters.
        /// </summary>
        /// <param name="file">The analysis file containing the raw measurement data.</param>
        /// <param name="isImagingMs">Indicates whether the data is from imaging mass spectrometry.</param>
        /// <param name="isGuiProcess">Indicates whether the process is running in a GUI context.</param>
        /// <param name="retry">The number of retry attempts for loading the measurement data.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        public StandardDataProvider(AnalysisFileBean file, bool isImagingMs, bool isGuiProcess, int retry, CancellationToken token = default)
            : base(LoadMeasurementAsync(file, false, isImagingMs, isGuiProcess, retry, false, token)) {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardDataProvider"/> class.
        /// This constructor loads raw measurement data asynchronously based on the provided parameters.
        /// </summary>
        /// <param name="file">The analysis file containing the raw measurement data.</param>
        /// <param name="isImagingMs">Indicates whether the data is from imaging mass spectrometry.</param>
        /// <param name="isGuiProcess">Indicates whether the process is running in a GUI context.</param>
        /// <param name="retry">The number of retry attempts for loading the measurement data.</param>
        /// <param name="ignoreRtCorrection">Specifies whether to ignore retention time correction during data loading.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        public StandardDataProvider(AnalysisFileBean file, bool isImagingMs, bool isGuiProcess, int retry, bool ignoreRtCorrection, CancellationToken token = default)
            : base(LoadMeasurementAsync(file, false, isImagingMs, isGuiProcess, retry, ignoreRtCorrection, token)) {

        }
    }

    public class StandardDataProviderFactory : IDataProviderFactory<AnalysisFileBean>, IDataProviderFactory<RawMeasurement>
    {
        public StandardDataProviderFactory(int retry = 5, bool isGuiProcess = false) {
            this.retry = retry;
            this.isGuiProcess = isGuiProcess;
        }
        
        private readonly bool isGuiProcess;
        private readonly int retry = 5;

        /// <summary>
        /// Whether to ignore RT correction when reading raw spectra.
        /// </summary>
        public bool IgnoreRtCorrection { get; set; } = false;

        public IDataProvider Create(AnalysisFileBean file) {
            return new StandardDataProvider(file, false, isGuiProcess, retry, IgnoreRtCorrection);
        }

        public IDataProvider Create(RawMeasurement rawMeasurement) {
            return new StandardDataProvider(rawMeasurement);
        }
    }
}
