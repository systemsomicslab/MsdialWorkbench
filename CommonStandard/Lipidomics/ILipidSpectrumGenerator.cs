using CompMs.Common.DataObj.Property;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Lipidomics
{
    public interface ILipidSpectrumGenerator
    {
        bool CanGenerate(ILipid lipid, AdductIon adduct);
        IMSScanProperty Generate(SubLevelLipid lipid, AdductIon adduct, IMoleculeProperty molecule = null);
        IMSScanProperty Generate(SomeAcylChainLipid lipid, AdductIon adduct, IMoleculeProperty molecule = null);
        IMSScanProperty Generate(PositionSpecificAcylChainLipid lipid, AdductIon adduct, IMoleculeProperty molecule = null);
    }
}
