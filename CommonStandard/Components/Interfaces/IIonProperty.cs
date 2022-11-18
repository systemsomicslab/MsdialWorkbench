using CompMs.Common.DataObj.Property;

namespace CompMs.Common.Interfaces
{
    public interface IIonProperty
    {
        // Molecule ion metadata
        AdductIon AdductType { get; }
        void SetAdductType(AdductIon adduct);
        double CollisionCrossSection { get; }
    }
}