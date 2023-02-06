using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public interface ISpectrumPeakGenerator
    {
        IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, AcylChain acylChain, AdductIon adduct, double nlMass, double abundance);
        IEnumerable<SpectrumPeak> GetAlkylDoubleBondSpectrum(ILipid lipid, AlkylChain acylChain, AdductIon adduct, double nlMass, double abundance);
        IEnumerable<SpectrumPeak> GetSphingoDoubleBondSpectrum(ILipid lipid, SphingoChain acylChain, AdductIon adduct, double nlMass, double abundance);
    }
}
