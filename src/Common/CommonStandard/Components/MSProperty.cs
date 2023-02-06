using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System;

namespace CompMs.Common.Components
{
    public class MSProperty : IMSProperty
    {
        public MSProperty(double precursorMz, ChromXs chromXs, IonMode ionMode) {
            ChromXs = chromXs ?? throw new ArgumentNullException(nameof(chromXs));
            IonMode = ionMode;
            PrecursorMz = precursorMz;
        }

        public ChromXs ChromXs { get; set; }
        public IonMode IonMode { get; set; }
        public double PrecursorMz { get; set; }
    }
}
