using CompMs.Common.DataObj.Property;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Lipidomics
{
    public interface ILipidSpectrumGenerator
    {
        bool CanGenerate(ILipid lipid, AdductIon adduct);
        IMSScanProperty? Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null);
    }
}
