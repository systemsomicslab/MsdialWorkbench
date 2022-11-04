using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Algorithm;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialImmsCore.Process
{
    public sealed class PeakPickProcess
    {
        private readonly IMsdialDataStorage<MsdialImmsParameter> _storage;

        public PeakPickProcess(IMsdialDataStorage<MsdialImmsParameter> storage) {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public List<ChromatogramPeakFeature> Pick(AnalysisFileBean file, IDataProvider provider, Action<int> reportAction) {
            var parameter = _storage.Parameter;
            parameter.FileID2CcsCoefficients.TryGetValue(file.AnalysisFileId, out var coeff);
            var chromPeakFeatures = new PeakSpotting(parameter).Run(provider, 0, 30, reportAction);
            IsotopeEstimator.Process(chromPeakFeatures, parameter, _storage.IupacDatabase, true);
            CcsEstimator.Process(chromPeakFeatures, parameter, parameter.IonMobilityType, coeff, parameter.IsAllCalibrantDataImported);
            return chromPeakFeatures;
        }
    }
}
