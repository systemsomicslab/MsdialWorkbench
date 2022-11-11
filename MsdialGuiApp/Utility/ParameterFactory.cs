using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using System;

namespace CompMs.App.Msdial.Utility
{
    internal static class ParameterFactory
    {
        public static ParameterBase CreateParameter(Ionization ionization, SeparationType separationType) {
            if (ionization == Ionization.EI && separationType == SeparationType.Chromatography)
                return new MsdialGcmsParameter(GlobalResources.Instance.IsLabPrivate);
            if (ionization == Ionization.ESI && separationType == SeparationType.Chromatography)
                return new MsdialLcmsParameter(GlobalResources.Instance.IsLabPrivate);
            if (ionization == Ionization.ESI && separationType == (SeparationType.Chromatography | SeparationType.IonMobility))
                return new MsdialLcImMsParameter(GlobalResources.Instance.IsLabPrivate);
            if (ionization == Ionization.ESI && separationType == SeparationType.Infusion)
                return new MsdialDimsParameter(GlobalResources.Instance.IsLabPrivate);
            if (ionization == Ionization.ESI && separationType == (SeparationType.Infusion | SeparationType.IonMobility))
                return new MsdialImmsParameter(GlobalResources.Instance.IsLabPrivate);
            throw new Exception("Not supported separation type is selected.");
        }
    }
}
