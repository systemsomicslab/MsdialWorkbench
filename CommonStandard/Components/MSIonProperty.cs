using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System;

namespace CompMs.Common.Components
{
    public class MSIonProperty : IMSIonProperty
    {
        public MSIonProperty(
            double precursorMz,
            ChromXs chromXs,
            IonMode ionMode,
            AdductIon adductType,
            double collisionCrossSection) {

            PrecursorMz = precursorMz;
            ChromXs = chromXs;
            IonMode = ionMode;
            AdductType = adductType;
            CollisionCrossSection = collisionCrossSection;
        }

        public ChromXs ChromXs { get; set; }
        public IonMode IonMode { get; set; }
        public double PrecursorMz { get; set; }
        public AdductIon AdductType { get; private set; }
        public void SetAdductType(AdductIon adduct) {
            AdductType = adduct;
        }

        public double CollisionCrossSection { get; set; }
    }
}
