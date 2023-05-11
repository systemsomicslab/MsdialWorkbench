using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;

namespace CompMs.MsdialCore.Parser
{
    public sealed class CentroidMsScanPropertyLoader : IMsScanPropertyLoader<ChromatogramPeakFeature>
    {
        private readonly IDataProvider _provider;
        private readonly MSDataType _msDataType;

        public CentroidMsScanPropertyLoader(IDataProvider provider, MSDataType msDataType) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _msDataType = msDataType;
        }

        IMSScanProperty IMsScanPropertyLoader<ChromatogramPeakFeature>.Load(ChromatogramPeakFeature source) {
            var rawSpectrum = _provider.LoadMsSpectrumFromIndex(source.MS2RawSpectrumID);
            var spectrum = DataAccess.GetCentroidMassSpectra(rawSpectrum, _msDataType, 0, float.MinValue, float.MaxValue);
            return new MSScanProperty(source.MasterPeakID, source.PrecursorMz, source.PeakFeature.ChromXsTop.GetRepresentativeXAxis(), source.IonMode)
            {
                Spectrum = spectrum,
            };
        }
    }
}
