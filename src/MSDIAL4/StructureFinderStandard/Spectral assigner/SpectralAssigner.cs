using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.StructureFinder.Parser;
using Riken.Metabolomics.StructureFinder.Property;
using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.StructureFinder.SpectralAssigner
{
    public sealed class FragmentPeakMatcher
    {
        private FragmentPeakMatcher() { }

        private static double hydrogenMass = 1.007825035;
        private static double protonMass = 1.00727646677;
        private static double electronMass = 0.0005485799;
        
        private static double maxBondDissociationEnergy;

        /// <summary>
        /// This is main program
        /// </summary>
        public static List<PeakFragmentPair> GetSpectralAssignmentResult(Structure structure, List<Fragment> fragments, List<Peak> peaks, 
            AdductIon adduct, int treeDepth, double massTolerance, MassToleranceType massTolType, IonMode ionMode, List<FragmentLibrary> fragmentDB = null)
        {
            //consider fragment (X) or fragment+adduct (X+Na etc.)
            //In protonated or deprotonated precursor ions,
            //the loss of odd number's hydrogens (-H, -3H etc) is recognized as the result of double bond or ring generation (even electron); 
            //otherwise, as radical (no H move, -2H etc.).
            //In solvent adduct precursor, 
            //the loss of even number's hydrogens (-2H, -4H etc) is recognized as the result of double bond or ring generation (even electron); 
            var solventAdduct = ionMode == IonMode.Positive ? adduct.AdductIonAccurateMass - electronMass : adduct.AdductIonAccurateMass + electronMass;
            if (solventAdduct < 1.1) solventAdduct = 0; //currently, consider the plus effect only like Na+, NH4+, HCOO- etc.

            maxBondDissociationEnergy = getMaxBDE(fragments);

            //consider ring bond cleavage + dehydration
            var fragmentMinusShift = ionMode == IonMode.Positive ? Math.Max((double)treeDepth * hydrogenMass * 4.0 + protonMass, solventAdduct + (double)treeDepth * hydrogenMass * 4.0 + protonMass)
                : Math.Max((double)treeDepth * hydrogenMass * 4.0 - protonMass, (double)treeDepth * hydrogenMass * 4.0 - protonMass + solventAdduct);
            var fragmentPlusShift = ionMode == IonMode.Positive ? Math.Max(solventAdduct, protonMass) : Math.Max(solventAdduct, electronMass);

            var peakFragmentPairs = new List<PeakFragmentPair>();
            var peakDones = new List<int>();
            peaks = peaks.OrderByDescending(n => n.Mz).ToList();

            //in EI-MS assignment, this knowledge based fragment assignment is performed.
            if (fragmentDB != null && fragmentDB.Count > 0)
                doFragmentDbBasedSpectraAssignment(structure, peaks, peakFragmentPairs,
                    peakDones, fragmentDB, massTolerance, massTolType);

            for (int i = 0; i < peaks.Count; i++) {

                if (peakDones.Contains(i)) continue; //to avoid 'second' assignments from fragments DB results

                var peak = peaks[i];
                var peakFragmentPair = new PeakFragmentPair() { Peak = peak };
                var massTol = massTolType == MassToleranceType.Da ? massTolerance : MolecularFormulaUtility.ConvertPpmToMassAccuracy(peak.Mz, massTolerance);
                var fragmentStartIndex = getFragmentStartIndex(fragments, peak.Mz - fragmentPlusShift, massTol);

                var maxMatchedValue = double.MinValue;
                for (int j = fragmentStartIndex; j < fragments.Count; j++) {
                    var fragment = fragments[j];

                    if (fragment.ExactMass + fragmentPlusShift + massTol < peak.Mz) continue;
                    if (fragment.ExactMass - fragmentMinusShift - massTol > peak.Mz) break;

                    //Debug.WriteLine("Search mass pair\t" + peak.Mz + "\t" + fragments[i].SaturatedExactMass);

                    var matchedFragmentInfo = getMatchedFragmentInfo(j, fragment, peak, solventAdduct, adduct.AdductIonName, massTol, ionMode, fragments, peakFragmentPairs);
                    if (matchedFragmentInfo == null) continue;

                    if (maxMatchedValue < matchedFragmentInfo.TotalLikelihood) {
                        maxMatchedValue = matchedFragmentInfo.TotalLikelihood;
                        peakFragmentPair.MatchedFragmentInfo = matchedFragmentInfo;
                    }
                }
                if (maxMatchedValue > 0) {
                    peakFragmentPairs.Add(peakFragmentPair);
                }
            }

            peakFragmentPairs = getRefinedPeakFragmentPairs(peakFragmentPairs);

            foreach (var peakFragmentPair in peakFragmentPairs) {

                var peak = peakFragmentPair.Peak;
                var matchedInfo = peakFragmentPair.MatchedFragmentInfo;

                if (matchedInfo.FragmentID >= 0) { 
                    var fragment = fragments[matchedInfo.FragmentID];
                    var saturatedMass = fragment.ExactMass;
                    var container = MoleculeConverter.DictionaryToAtomContainer(fragment.AtomDictionary, fragment.BondDictionary);
                    var smiles = MoleculeConverter.AtomContainerToSmiles(container);
                    var formula = MoleculeConverter.ConvertAtomDicionaryToFormula(fragment.AtomDictionary);

                    var massDiff = Math.Abs(peak.Mz - matchedInfo.MatchedMass);
                    var ppm = MolecularFormulaUtility.PpmCalculator(peak.Mz, matchedInfo.MatchedMass);

                    matchedInfo.SaturatedMass = saturatedMass;
                    matchedInfo.Smiles = smiles;
                    matchedInfo.Formula = formula.FormulaString;
                    matchedInfo.Massdiff = Math.Round(massDiff, 6);
                    matchedInfo.Ppm = Math.Round(Math.Abs(ppm), 2);
                    matchedInfo.BdEnergy = fragment.BondDissociationEnergy;
                }
                else { //it means the result is coming from Fragment DB 
                    //as it is
                }

                //Debug.WriteLine(peakFragmentPair.Peak.Mz + "\t" + peakFragmentPair.Peak.Intensity + "\t" + saturatedMass + "\t"
                //    + peakFragmentPair.MatchedFragmentInfo.MatchedMass + "\t" + peakFragmentPair.MatchedFragmentInfo.RearrangedHydrogen + "\t"
                //    + peakFragmentPair.MatchedFragmentInfo.IsEeRule + "\t" + peakFragmentPair.MatchedFragmentInfo.IsHrRule + "\t"
                //    + peakFragmentPair.MatchedFragmentInfo.MaLikelihood + "\t" + peakFragmentPair.MatchedFragmentInfo.HrLikelihood + "\t" + peakFragmentPair.MatchedFragmentInfo.FlLikelihood + "\t"
                //    + peakFragmentPair.MatchedFragmentInfo.TotalLikelihood + "\t" + fragment.TreeDepth + "\t" + smiles);
            }
            return peakFragmentPairs;
        }

        private static double getMaxBDE(List<Fragment> fragments) {
            return fragments.Max(n => n.BondDissociationEnergy);
        }

        /// <summary>
        /// the mass spectral assignment will be performed here by means of fragment ion database
        /// /// </summary>
        private static void doFragmentDbBasedSpectraAssignment(Structure structure, List<Peak> peakList, 
            List<PeakFragmentPair> peakFragmentPairs, List<int> peakDones,
            List<FragmentLibrary> fragmentDB, double massTolerance, MassToleranceType massTolType)
        {
            var originalFormula = structure.Formula;
            for (int i = 0; i < peakList.Count; i++) {
                var peakMass = peakList[i].Mz;
                var ms2Tol = massTolerance; 
                if (massTolType == MassToleranceType.Ppm) 
                    ms2Tol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(peakMass, massTolerance);

                var maxScore = double.MinValue;
                var maxID = -1;
                for (int j = 0; j < fragmentDB.Count; j++) {

                    var fragment = fragmentDB[j];
                    //currently, most nearest m/z peak is asssigned with the consideration with the matching of molecular formula
                    if (Math.Abs(peakMass - fragment.FragmentMass) < ms2Tol && isFormulaMatch(originalFormula, fragment.FragmentFormula)) {

                        var massScore = getGaussianSimilarity(peakMass, fragment.FragmentMass, ms2Tol);
                        var jaccardScore = getJaccardSimilarity(fragment.FragmentStructure, structure);

                        if (massScore + jaccardScore > maxScore) {
                            maxID = j;
                            maxScore = (massScore + jaccardScore) * 0.5;
                        }
                    }
                }

                if (maxID >= 0) {
                    peakDones.Add(i); 
                    
                    var peak = peakList[i];
                    var matchedFrag = fragmentDB[maxID];

                    var matchedInfo = new MatchedFragmentInfo() {
                        TreeDepth = -1,
                        FragmentID = -1,
                        MatchedMass = matchedFrag.FragmentMass,
                        SaturatedMass = matchedFrag.FragmentStructure.ExactMass,
                        Formula = matchedFrag.FragmentFormula.FormulaString,
                        RearrangedHydrogen = (int)(matchedFrag.FragmentMass - matchedFrag.FragmentStructure.ExactMass),
                        Ppm = Math.Round((peak.Mz - matchedFrag.FragmentMass) / matchedFrag.FragmentMass * 1000000, 4),
                        Massdiff = Math.Round(peak.Mz - matchedFrag.FragmentMass, 7),
                        IsEeRule = true, IsHrRule = true, IsSolventAdductFragment = false,
                        AssignedAdductMass = 0, AssignedAdductString = string.Empty,
                        BdEnergy = matchedFrag.FragmentStructure.TotalBondEnergy,
                        Smiles = matchedFrag.FragmentSmiles,
                        TotalLikelihood = maxScore * 5.0,
                        HrLikelihood = maxScore,
                        BcLikelihood = maxScore,
                        MaLikelihood = maxScore,
                        FlLikelihood = maxScore,
                        BeLikelihood = maxScore
                    };

                    peakFragmentPairs.Add(new PeakFragmentPair() { Peak = peak, MatchedFragmentInfo = matchedInfo });
                }
            }
        }

        private static double getJaccardSimilarity(Structure fragment, Structure structure)
        {
            var structureDescriptor = structure.MolecularDescriptor;
            var fragmentDescriptor = fragment.MolecularDescriptor;

            var sDescriptorCount = 0;
            var fDescriptorCount = 0;
            var intersectionSize = 0;

            var infoArray = structureDescriptor.GetType().GetProperties();
            foreach (var info in infoArray) {
                var sValue = (int)info.GetValue(structureDescriptor, null);
                var fValue = (int)info.GetValue(fragmentDescriptor, null);
                if (sValue == 1) {
                    sDescriptorCount++;
                }
                if (fValue == 1) {
                    fDescriptorCount++;
                }
                if (sValue == 1 && fValue == 1) {
                    intersectionSize++;
                }
            }

            return (double)intersectionSize / (double)(sDescriptorCount + fDescriptorCount - intersectionSize);
        }

        /// <summary>
        /// check if fragment formula is inside of molecular structure formula
        /// </summary>
        private static bool isFormulaMatch(Formula parentFormula, Formula fragmentFormula)
        {
            if (parentFormula.Cnum < fragmentFormula.Cnum) return false;
            if (parentFormula.Hnum < fragmentFormula.Hnum) return false;
            if (parentFormula.Nnum < fragmentFormula.Nnum) return false;
            if (parentFormula.Onum < fragmentFormula.Onum) return false;
            if (parentFormula.Snum < fragmentFormula.Snum) return false;
            if (parentFormula.Pnum < fragmentFormula.Pnum) return false;
            if (parentFormula.Fnum < fragmentFormula.Fnum) return false;
            if (parentFormula.Clnum < fragmentFormula.Clnum) return false;
            if (parentFormula.Brnum < fragmentFormula.Brnum) return false;
            if (parentFormula.Inum < fragmentFormula.Inum) return false;
            if (parentFormula.Sinum < fragmentFormula.Sinum) return false;
            return true;
        }

        /// <summary>
        /// remove duplicate assignments (definition: same fragment ID 'AND' same HR count)
        /// </summary>
        private static List<PeakFragmentPair> getRefinedPeakFragmentPairs(List<PeakFragmentPair> peakFragmentPairs)
        {
            if (peakFragmentPairs.Count <= 1) return peakFragmentPairs;

            peakFragmentPairs = peakFragmentPairs.OrderBy(n => n.MatchedFragmentInfo.FragmentID).ThenBy(n => n.MatchedFragmentInfo.RearrangedHydrogen).ToList();

            var rPeakFragmentPairs = new List<PeakFragmentPair>();

            // first, the result from fragment DB is stored as refined peak pairs
            var initialCount = peakFragmentPairs.Count(n => n.MatchedFragmentInfo.FragmentID == -1);
            for (int i = 0; i < initialCount; i++) 
                rPeakFragmentPairs.Add(peakFragmentPairs[i]);
            if (initialCount == peakFragmentPairs.Count)
                return rPeakFragmentPairs;

            // second, dupulicate fragments will be removed by the criterion of peak intensities
            rPeakFragmentPairs.Add(peakFragmentPairs[initialCount]);
            var currentFragId = peakFragmentPairs[initialCount].MatchedFragmentInfo.FragmentID;
            var currentFragHrCount = peakFragmentPairs[initialCount].MatchedFragmentInfo.RearrangedHydrogen;

            for (int i = initialCount + 1; i < peakFragmentPairs.Count; i++) {
                //if (peakFragmentPairs.Count - 1 < i) break;
                var pair = peakFragmentPairs[i];
                rPeakFragmentPairs.Add(pair);
                #region previous code
                //if (pair.MatchedFragmentInfo.FragmentID != currentFragId || pair.MatchedFragmentInfo.RearrangedHydrogen != currentFragHrCount) {
                //    currentFragId = pair.MatchedFragmentInfo.FragmentID;
                //    currentFragHrCount = pair.MatchedFragmentInfo.RearrangedHydrogen;
                //    rPeakFragmentPairs.Add(pair);
                //}
                //else {
                //    if (pair.Peak.Intensity > rPeakFragmentPairs[rPeakFragmentPairs.Count - 1].Peak.Intensity) { //currently, higher intensity's fragment has the priority 
                //        rPeakFragmentPairs.RemoveAt(rPeakFragmentPairs.Count - 1);
                //        rPeakFragmentPairs.Add(pair);
                //    }
                //}
                #endregion
            }
            if (rPeakFragmentPairs.Count <= 1) return rPeakFragmentPairs; 
            else return rPeakFragmentPairs.OrderBy(n => n.Peak.Mz).ToList();
        }

        /// <summary>
        /// Return MatchedFragmentInfo. If there is no match pair with any hydrogen shift, return null
        /// </summary>
        private static MatchedFragmentInfo getMatchedFragmentInfo(int fragmentID, Fragment fragment, Peak peak, 
            double solventAdduct, string adductString, double massTol, IonMode ionMode, 
            List<Fragment> fragments, List<PeakFragmentPair> peakFragmentPairs)
        {
            var matchedFragmentInfo = new MatchedFragmentInfo() { FragmentID = fragmentID };
            var electron = ionMode == IonMode.Positive ? electronMass : - electronMass;
            var initialCounter = ionMode == IonMode.Positive ? -1 : 0; // means from considering [M+H]+ in positive ion mode
            var lastCounter = ionMode == IonMode.Positive ? 0 : 2;
            for (int i = initialCounter; i <= 2 * fragment.CleavedBondDictionary.Count + lastCounter; i++) {

                var matchedMass = fragment.ExactMass - i * hydrogenMass - electron;
                var matchedMassWithAdduct = fragment.ExactMass - i * hydrogenMass - electron + solventAdduct;

                //if (Math.Abs(108.0499 - peak.Mz) < 0.05) {
                //    Console.WriteLine();
                //}

                //if (Math.Abs(108.0499 - matchedMassWithAdduct) < 0.05) {
                //    Console.WriteLine();
                //}
                //Debug.WriteLine(peak.Mz + "\t" + matchedMass + "\t" + matchedMassWithAdduct + "\t" + Math.Abs(matchedMass - peak.Mz) + "\t" + Math.Abs(matchedMassWithAdduct - peak.Mz));
                
                if (matchedMass - massTol < peak.Mz && peak.Mz < matchedMass + massTol) {

                    setMatchedFragmentInfo(matchedFragmentInfo, fragment, peak, i, matchedMass, false, massTol, ionMode, 0.0, string.Empty, fragments, peakFragmentPairs);
                    return matchedFragmentInfo;
                }
                else if (matchedMassWithAdduct - massTol < peak.Mz && peak.Mz < matchedMassWithAdduct + massTol) {

                    setMatchedFragmentInfo(matchedFragmentInfo, fragment, peak, i, matchedMassWithAdduct, true, massTol, ionMode, solventAdduct, 
                        AdductIonParcer.GetAdductContent(adductString), fragments, peakFragmentPairs);
                    return matchedFragmentInfo;
                }
            }

            return null;
        }

        /// <summary>
        /// set matched fragment scores
        /// </summary>
        private static void setMatchedFragmentInfo(MatchedFragmentInfo matchedFragmentInfo, Fragment fragment, Peak peak, int rearrangedHydrogen,
            double matchedMass, bool isSolventAdduct, double massTol, IonMode ionMode, double adductMass, string adductString,
            List<Fragment> fragments, List<PeakFragmentPair> peakFragmentPairs)
        {
            var isEeRule = false;
            var isHrRule = false;

            matchedFragmentInfo.TreeDepth = fragment.TreeDepth;
            matchedFragmentInfo.RearrangedHydrogen = -1 * rearrangedHydrogen;
            matchedFragmentInfo.MatchedMass = matchedMass;
            matchedFragmentInfo.IsSolventAdductFragment = isSolventAdduct;
            matchedFragmentInfo.AssignedAdductMass = adductMass;
            matchedFragmentInfo.AssignedAdductString = adductString;
            matchedFragmentInfo.MaLikelihood = getGaussianSimilarity(peak.Mz, matchedMass, massTol);
            matchedFragmentInfo.HrLikelihood = getFragmentLikelihood(fragment, peak, matchedMass, rearrangedHydrogen, isSolventAdduct, ionMode
                , out isEeRule, out isHrRule);
            matchedFragmentInfo.BeLikelihood = maxBondDissociationEnergy == 0 ? 1 : Math.Sqrt(1.0 - fragment.BondDissociationEnergy / maxBondDissociationEnergy);
            matchedFragmentInfo.BcLikelihood = getBondCleavageLikelihood(fragment);
            matchedFragmentInfo.FlLikelihood = getFragmentLinkageLikelihood(fragment, fragments, peakFragmentPairs);
            matchedFragmentInfo.TotalLikelihood = matchedFragmentInfo.MaLikelihood + matchedFragmentInfo.FlLikelihood + 
                matchedFragmentInfo.HrLikelihood + matchedFragmentInfo.BcLikelihood + matchedFragmentInfo.BeLikelihood;

            matchedFragmentInfo.IsEeRule = isEeRule;
            matchedFragmentInfo.IsHrRule = isHrRule;
        }

        private static float getFragmentLinkageLikelihood(Fragment fragment, List<Fragment> fragments, List<PeakFragmentPair> peakFragmentPairs)
        {
            if (fragment.TreeDepth <= 1 || peakFragmentPairs.Count == 0) return 1.0F;
            var fragAtoms = fragment.AtomDictionary;
            var isParentExist = false;
            foreach (var pair in peakFragmentPairs.Where(n => n.MatchedFragmentInfo.TreeDepth < fragment.TreeDepth)) {
                if (pair.MatchedFragmentInfo.FragmentID == -1)
                    continue;

                var parentAtoms = fragments[pair.MatchedFragmentInfo.FragmentID].AtomDictionary;

                var isAllAtomExist = true;
                foreach (var fAtom in fragAtoms.Where(n => n.Value.AtomString != "H")) {
                    if (!parentAtoms.ContainsKey(fAtom.Key)) {
                        isAllAtomExist = false;
                        break;
                    }
                }
                if (isAllAtomExist == false) {
                    continue;
                }
                else {
                    isParentExist = true;
                    break;
                }
            }
            if (isParentExist) return 1.0F;
            else return 0.7F;
        }

        private static float getBondCleavageLikelihood(Fragment fragment)
        {
            if (fragment.CleavedBondDictionary.Count == 0) return 1.0F;

            var likelihood = 1.0F;
            foreach (var cBond in fragment.CleavedBondDictionary) {
                var cBondID = cBond.Key;
                var cBondProp = cBond.Value;

                if (cBondProp.BondType == BondType.Double) likelihood = likelihood * 0.5F;
                else if (cBondProp.BondType == BondType.Triple) likelihood = likelihood * 0.25F;
                if (cBondProp.IsAromaticity) likelihood = likelihood * 0.25F;
                
                if (cBondProp.BondEnv.First_HalogenCount == 0 && cBondProp.BondEnv.First_HeteroatomCount == 0 && cBondProp.BondEnv.First_SiliconCount == 0 &&
                    cBondProp.BondEnv.Second_HalogenCount == 0 && cBondProp.BondEnv.Second_HeteroatomCount == 0 && cBondProp.BondEnv.Second_SiliconCount == 0) {
                        likelihood = likelihood * 0.25F;
                }
                likelihood = likelihood * 0.9F;
            }

            return likelihood;


            //return fragment.CleavedBondDictionary.Aggregate(1.0F, (m, i) => m * i.Value.CleavageLikelihood * 0.9F); //multiplied all of clevage likelihoods
        }

        private static float getGaussianSimilarity(double actual, double reference, double tolrance)
        {
            return (float)Math.Exp(-0.5 * Math.Pow((actual - reference) / tolrance, 2.0));
        }

        private static float getFragmentLikelihood(Fragment fragment, Peak peak, double matchedMass, int rearrangedHydrogen, bool isSolventAdduct, IonMode ionMode, out bool isEeRule, out bool isHrRule)
        {
            var evenElectronPenalty = 1.0F;
            var hrRulePenalty = 1.0F;

            isEeRule = false;
            isHrRule = false;

            //the below is recognized as even electron fragment ion
            //[X-H]+ is recognized as [X-2H+H]+ (protonated ion with double bond generation).
            //[X-3H]- is recognized as [X-2H-H]- (deprotonated ion with double bond generation).
            //[X-2H+Na] is recognized as adduct ion with double bond generation.
            //[X-2H+HCOO]- is recognized as adduct ion with double bond generation.

            //below is the description for protonated or deprotonated ions.
            //for solvent adduct case, hr rules are not optimized yet. Therefore, even electron rule is used as the equal effect to HR rule in solvent adduct case.
            //the rearranged hydrogen must be odd (even electron rule).
            //if ion mode is negative and sulfur is not included as cleaved atom, the rearranged hydrogen can be even (HR: hydrogen rearrangement rule).
            //otherwise, it should be odd.

            if (isSolventAdduct) {
                #region
                if (rearrangedHydrogen % 2 != 0) {
                    evenElectronPenalty = 0.2F;
                    hrRulePenalty = 0.2F;
                    return Math.Max(evenElectronPenalty, hrRulePenalty);
                }
                else {
                    isEeRule = true;
                    isHrRule = true;
                    return 1.0F;
                }
                #endregion
            }

            if (rearrangedHydrogen % 2 == 0) evenElectronPenalty = 0.2F;
            var isSulphurContained = fragment.CleavedAtomDictionary.Count(n => n.Value.AtomString == "S") > 0 ? true : false;
            if ((ionMode != IonMode.Negative || isSulphurContained == false) && rearrangedHydrogen % 2 == 0) hrRulePenalty = 0.2F;

            //HR rule mild penalty
            //C, P (positive): 1 cleavage (-1), 2 cleavage (-3), 3 cleavage (-5) => count * -2 + 1
            //C, P (negative): 1 cleavage (-3), 2 cleavage (-5), 3 cleavege (-7) => count * -2 - 1
            //N, O (positive): 1 cleavage (+1), 2 cleavage (+1), 3 cleavage (+1) => + 1
            //N, O (negative): 1 cleavage (-1), 2 cleavage (-1), 3 cleavage (-1) => - 1
            //S (positive): 1 cleavage (-1), 2 cleavage (-1 or -3), 3 cleavage (-1 or -3 or -5) => 
            //S (negative): 1 cleavage (-1, -2), 2 cleavage (-3 or -4), 3 cleavage (-5 or -6) => 
            var cCount = fragment.CleavedAtomDictionary.Count(n => n.Value.AtomString == "C");
            var pCount = fragment.CleavedAtomDictionary.Count(n => n.Value.AtomString == "P");
            var nCount = fragment.CleavedAtomDictionary.Count(n => n.Value.AtomString == "N");
            var oCount = fragment.CleavedAtomDictionary.Count(n => n.Value.AtomString == "O");
            var sCount = fragment.CleavedAtomDictionary.Count(n => n.Value.AtomString == "S");

            var totalAtomCount = cCount + pCount + nCount + oCount + sCount;
            if (totalAtomCount == fragment.CleavedAtomDictionary.Count) { // if other elements contained, the current program cannot evaluate the probability
                var expectedHRfromCNOP = ionMode == IonMode.Positive ?
                    cCount * -2 + pCount * -2 + 1 : cCount * -2 + pCount * -2 - 1;

                var isMatched = false;
                if (ionMode == IonMode.Negative) {
                    if (expectedHRfromCNOP - 2 * sCount - 1 == rearrangedHydrogen || expectedHRfromCNOP - 2 * sCount == rearrangedHydrogen) {
                        isMatched = true;
                    }
                }
                else {
                    for (int i = 0; i < sCount; i++) {
                        if (expectedHRfromCNOP == rearrangedHydrogen || expectedHRfromCNOP - 2 * sCount == rearrangedHydrogen) {
                            isMatched = true;
                            break;
                        }
                    }
                }

                if (isMatched == false && hrRulePenalty == 1.0)
                    hrRulePenalty = 0.7F;
            }

            if (hrRulePenalty > 0.5) isHrRule = true;
            if (evenElectronPenalty > 0.5) isEeRule = true;

            return Math.Max(evenElectronPenalty, hrRulePenalty);
        }

        private static int getFragmentStartIndex(List<Fragment> fragments, double fragmentMassMin, double massTol)
        {
            if (fragments == null || fragments.Count == 0) return 0;
            var targetMass = fragmentMassMin - massTol;
            int startIndex = 0, endIndex = fragments.Count - 1;
            int counter = 0;

            while (counter < 5) {
                if (fragments[startIndex].ExactMass <= targetMass && targetMass < fragments[(startIndex + endIndex) / 2].ExactMass) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (fragments[(startIndex + endIndex) / 2].ExactMass <= targetMass && targetMass < fragments[endIndex].ExactMass) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }
    }
}
