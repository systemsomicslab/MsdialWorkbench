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
    class ParameterFactory
    {
        public static ParameterBase CreateParameter(Ionization ionization, SeparationType separationType) {
            if (ionization == Ionization.EI && separationType == SeparationType.Chromatography)
                return new MsdialGcmsParameter();
            if (ionization == Ionization.ESI && separationType == SeparationType.Chromatography)
                return new MsdialLcmsParameter();
            if (ionization == Ionization.ESI && separationType == (SeparationType.Chromatography | SeparationType.IonMobility))
                return new MsdialLcImMsParameter();
            if (ionization == Ionization.ESI && separationType == SeparationType.Infusion)
                return new MsdialDimsParameter();
            if (ionization == Ionization.ESI && separationType == (SeparationType.Infusion | SeparationType.IonMobility))
                return new MsdialImmsParameter();
            throw new Exception("Not supported separation type is selected.");
        }
    }
}
