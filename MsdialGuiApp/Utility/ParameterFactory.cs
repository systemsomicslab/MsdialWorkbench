using CompMs.App.Msdial.StartUp;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Utility
{
    class ParameterFactory
    {
        public static ParameterBase CreateParameter(Ionization ionization, SeparationType separationType) {
            if (ionization == Ionization.EI && separationType == SeparationType.Chromatography)
                return new MsdialGcmsParameter();
            if (ionization == Ionization.ESI && separationType == SeparationType.IonMobility)
                return new MsdialLcImMsParameter();
            if (ionization == Ionization.ESI && separationType == SeparationType.Chromatography)
                return new MsdialLcmsParameter();
            if (separationType == SeparationType.Infusion)
                return new MsdialDimsParameter();
            return null;
        }

        public static void SetParameterFromStartUpVM(ParameterBase parameter, StartUpWindowVM vm) {
            parameter.ProjectFilePath = vm.ProjectFilePath;
            parameter.ProjectFolderPath = Path.GetDirectoryName(vm.ProjectFilePath);
            parameter.Ionization = vm.Ionization;
            // parameter.SeparationType = vm.SeparationType;
            parameter.AcquisitionType = vm.AcquisitionType;
            parameter.MSDataType = vm.MS1DataType;
            parameter.MS2DataType = vm.MS2DataType;
            parameter.IonMode = vm.IonMode;
            parameter.TargetOmics = vm.TargetOmics;
            parameter.InstrumentType = vm.InstrumentType;
            parameter.Instrument = vm.Instrument;
            parameter.Authors = vm.Authors;
            parameter.License = vm.License;
            // parameter.ColliionEnergy = vm.CollisionEnergy;
            parameter.Comment = vm.Comment;
        }
    }
}
