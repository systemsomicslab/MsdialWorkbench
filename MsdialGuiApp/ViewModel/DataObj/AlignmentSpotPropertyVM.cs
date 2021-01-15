using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    public class AlignmentSpotPropertyVM
    {
        public int AlignmentID => innerModel.AlignmentID;
        public int MasterAlignmentID => innerModel.MasterAlignmentID;
        public double TimesCenter => innerModel.TimesCenter.Value;
        public double MassCenter => innerModel.MassCenter;
        public double HeightAverage => innerModel.HeightAverage;
        public ReadOnlyCollection<AlignmentChromPeakFeature> AlignedPeakProperties => innerModel.AlignedPeakProperties.AsReadOnly();

        private readonly AlignmentSpotProperty innerModel;

        public static readonly double KMIupacUnit;
        public static readonly double KMNominalUnit;
        public double KM => MassCenter / KMIupacUnit * KMNominalUnit;
        public double NominalKM => Math.Round(KM);
        public double KMD => NominalKM - KM;
        public double KMR => NominalKM % KMNominalUnit;

        static AlignmentSpotPropertyVM() {
            KMIupacUnit = CompMs.Common.DataObj.Property.AtomMass.hMass * 2 + CompMs.Common.DataObj.Property.AtomMass.cMass; // CH2
            KMNominalUnit = Math.Round(KMIupacUnit);
        }

        public AlignmentSpotPropertyVM(AlignmentSpotProperty innerModel) {
            this.innerModel = innerModel;
        }
    }
}
