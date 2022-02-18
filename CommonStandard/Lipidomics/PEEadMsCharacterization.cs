using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Lipidomics
{
    public static class PEEadMsCharacterization
    {
        public static (ILipid, double[]) Characterize(
            IMSScanProperty scan, ILipid molecule, MoleculeMsReference reference,
            float tolerence, float mzBegin, float mzEnd)
        {

            var exp_spectrum = scan.Spectrum;
            var ref_spectrum = reference.Spectrum;
            var adduct = reference.AdductType;

            if (adduct.AdductIonName == "[M+H]+")
            {
                var classions = ref_spectrum.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.metaboliteclass)).ToList();
                var classionsDetected = EadMsCharacterizationUtility.CountDetectedIons(exp_spectrum, classions, tolerence);
                var isClassIonExisted = false;
                if (classionsDetected >= 2)
                {
                    isClassIonExisted = true;
                }
                if (isClassIonExisted == false) return (null, new double[2] { 0.0, 0.0 });

                var chainIons = ref_spectrum.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.acylchain)).ToList();
                var chainIonsDetected = EadMsCharacterizationUtility.CountDetectedIons(exp_spectrum, classions, tolerence);
                var isChainIonExisted = false;
                if (classionsDetected >= 2)
                {
                    isClassIonExisted = true;
                }
                var positionIons = ref_spectrum.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.acylchain)).ToList();
                var positionIonsDetected = EadMsCharacterizationUtility.CountDetectedIons(exp_spectrum, classions, tolerence);
                var isPositionIonExisted = false;
                if (classionsDetected >= 2)
                {
                    isClassIonExisted = true;
                }
                var doublebondIons = ref_spectrum.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond)).ToList();
                var matchedions = MsScanMatching.GetMatchedPeaksScores(exp_spectrum, doublebondIons, tolerence, mzBegin, mzEnd);
                var matchedPercent = matchedions[0];
                var matchedCount = matchedions[1];
                var isDoubleBondIdentified = matchedPercent > 0.5 ? true : false;

                var classionScore = isClassIonExisted ? 1.0 : 0.0;
                var chainionScore = isChainIonExisted ? 1.0 : 0.0;
                var positionScore = isPositionIonExisted ? 1.0 : 0.0;
                var doublebondScore = matchedPercent;
                var score = classionScore + chainionScore + positionScore * 5 + doublebondScore * 10.0;
                var counter = classionsDetected + chainIonsDetected + positionIonsDetected + matchedCount;

                var chains = molecule.Chains;
                if (!isChainIonExisted)
                {
                    var obj = new Lipid(LbmClass.PE, molecule.Mass, new TotalChain(chains.CarbonCount, chains.DoubleBondCount, chains.OxidizedCount,
                        2, 0, 0));
                    return (obj, new double[2] { score, counter });
                }
                else
                {
                    var deepChains = (SeparatedChains)chains;
                    if (isPositionIonExisted && isDoubleBondIdentified)
                    {
                        return (molecule, new double[2] { score, counter });
                    }
                    else if (isPositionIonExisted)
                    {
                        var acyl1 = new AcylChain(deepChains.Chains[0].CarbonCount,
                            new DoubleBond(deepChains.Chains[0].DoubleBondCount),
                            new Oxidized(0));
                        var acyl2 = new AcylChain(deepChains.Chains[1].CarbonCount,
                            new DoubleBond(deepChains.Chains[0].DoubleBondCount),
                            new Oxidized(0));
                        var obj = new Lipid(LbmClass.PE, molecule.Mass, new PositionLevelChains(acyl1, acyl2));
                        return (obj, new double[2] { score, counter });
                    }
                    else
                    {
                        var acyl1 = new AcylChain(deepChains.Chains[0].CarbonCount,
                            new DoubleBond(deepChains.Chains[0].DoubleBondCount),
                            new Oxidized(0));
                        var acyl2 = new AcylChain(deepChains.Chains[1].CarbonCount,
                            new DoubleBond(deepChains.Chains[1].DoubleBondCount),
                            new Oxidized(0));
                        var obj = new Lipid(LbmClass.PE, molecule.Mass, new MolecularSpeciesLevelChains(acyl1, acyl2));
                        return (obj, new double[2] { score, counter });
                    }
                }
            }
            return (null, new double[2] { 0.0, 0.0 });
        }
    }
}
