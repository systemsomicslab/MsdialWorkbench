using CompMs.Common.Enum;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class IonAbundance
    {
        public IonAbundance(IonAbundanceUnit unit) {
            Unit = unit;
            Label = unit.ToLabel();
        }

        public IonAbundanceUnit Unit { get; set; }
        public string Label { get; set; }
    }
}
