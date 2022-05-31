using CompMs.Common.Enum;

namespace CompMs.App.Msdial.Model.Normalize
{
    class IonAbundance
    {
        public IonAbundance(IonAbundanceUnit unit, string label) {
            Unit = unit;
            Label = label;
        }

        public IonAbundanceUnit Unit { get; set; }
        public string Label { get; set; }
    }
}
