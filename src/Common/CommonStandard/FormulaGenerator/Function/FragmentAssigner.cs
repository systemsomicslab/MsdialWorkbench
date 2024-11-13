using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.FormulaGenerator.Function {
    public sealed class FragmentAssigner
    {
        private FragmentAssigner() { }
        private static double electron = 0.0005485799;
        
        /// <summary>
        /// peaklist should be centroid and refined. For peaklist refining, use GetRefinedPeaklist.
        /// </summary>
        /// <param name="peaklist"></param>
        /// <param name="formula"></param>
        /// <param name="analysisParam"></param>
        public static List<ProductIon> FastFragmnetAssigner(List<SpectrumPeak> peaklist, List<ProductIon> productIonDB, 
            Formula formula, double ms2Tol, MassToleranceType massTolType, AdductIon adductIon)
        {
            var productIons = new List<ProductIon>();
            double eMass = electron; if (adductIon.IonMode == IonMode.Negative) eMass = -1.0 * electron; 

            foreach (var peak in peaklist)
            {
                if (peak.Comment != "M") continue;

                double mass = peak.Mass + eMass;
                double minDiff = double.MaxValue;
                double massTol = ms2Tol;
                if (massTolType == MassToleranceType.Ppm)
                    massTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ms2Tol);
                int minId = -1;

                //for precursor annotation
                if (Math.Abs(mass - formula.Mass - adductIon.AdductIonAccurateMass) < massTol)
                {
                    var nFormula = MolecularFormulaUtility.ConvertFormulaAdductPairToPrecursorAdduct(formula, adductIon);
                    productIons.Add(new ProductIon() { Formula = nFormula, Mass = peak.Mass, MassDiff = formula.Mass + adductIon.AdductIonAccurateMass - mass, Intensity = peak.Intensity });
                    continue;
                }

                //library search
                var fragmentFormulas = getFormulaCandidatesbyLibrarySearch(formula, adductIon.IonMode, peak.Mass, massTol, productIonDB);
                if (fragmentFormulas == null || fragmentFormulas.Count == 0)
                    fragmentFormulas = getValenceCheckedFragmentFormulaList(formula, adductIon.IonMode, peak.Mass, massTol);

                for (int i = 0; i < fragmentFormulas.Count; i++)
                {
                    if (minDiff > Math.Abs(mass - fragmentFormulas[i].Mass))
                    {
                        minId = i;
                        minDiff = Math.Abs(mass - fragmentFormulas[i].Mass);
                    }
                }
                if (minId >= 0)
                    productIons.Add(new ProductIon() {
                        Formula = fragmentFormulas[minId],
                        Mass = peak.Mass,
                        MassDiff = fragmentFormulas[minId].Mass - mass,
                        Intensity = peak.Intensity
                    }); 
            }

            foreach (var ion in productIons) {
                var startIndex = getStartIndex(ion.Mass, 0.1, productIonDB);
                for (int i = startIndex; i < productIonDB.Count; i++) {
                    var ionQuery = productIonDB[i];
                    if (ionQuery.IonMode != adductIon.IonMode) continue;
                    if (ionQuery.Formula.Mass > ion.Mass + 0.1) break;

                    if (isFormulaComposition(ion.Formula, ionQuery.Formula)) {
                        ion.CandidateInChIKeys = ionQuery.CandidateInChIKeys;
                        ion.CandidateOntologies = ionQuery.CandidateOntologies;
                        ion.Frequency = ionQuery.Frequency;
                        break;
                    }
                }
            }

            return productIons;
        }

        private static List<Formula> getFormulaCandidatesbyLibrarySearch(Formula formula, IonMode ionMode, 
            double mz, double massTol, List<ProductIon> productIonDB) {
            var candidates = new List<Formula>();
            var startIndex = getStartIndex(mz, massTol, productIonDB);
            for (int i = startIndex; i < productIonDB.Count; i++) {
                var ionQuery = productIonDB[i];
                if (ionQuery.IonMode != ionMode) continue;
                if (ionQuery.Formula.Mass < mz - massTol) continue;
                if (ionQuery.Formula.Mass > mz + massTol) break;

                if (isFormulaComposition(ionQuery.Formula, formula)) {
                    candidates.Add(ionQuery.Formula);
                }
            }

            return candidates;
        }

        private static int getStartIndex(double mass, double tol, List<ProductIon> productIonDB)
        {
            if (productIonDB == null || productIonDB.Count == 0) return 0;
            double targetMass = mass - tol;
            int startIndex = 0, endIndex = productIonDB.Count - 1;
            int counter = 0;

            while (counter < 5) {
                if (productIonDB[startIndex].Mass <= targetMass && targetMass < productIonDB[(startIndex + endIndex) / 2].Mass) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (productIonDB[(startIndex + endIndex) / 2].Mass <= targetMass && targetMass < productIonDB[endIndex].Mass) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        /// <summary>
        /// The neutralLosslist can be made by GetNeutralLossList after using GetRefinedPeaklist.
        /// </summary>
        /// <param name="neutralLosslist"></param>
        /// <param name="neutralLossDB"></param>
        /// <param name="originalFormula"></param>
        /// <param name="analysisParam"></param>
        /// <param name="adductIon"></param>
        /// <returns></returns>
        public static List<NeutralLoss> FastNeutralLossAssigner(List<NeutralLoss> neutralLosslist, List<NeutralLoss> neutralLossDB, 
            Formula originalFormula, double ms2Tol, MassToleranceType massTolType, AdductIon adductIon)
        {
            var neutralLossResult = new List<NeutralLoss>();
            //double eMass = electron; if (adductIon.IonMode == IonMode.Negative) eMass = -1.0 * electron; 

            foreach (var nloss in neutralLosslist)
            {
                double mass = nloss.MassLoss;
                double minDiff = double.MaxValue;
                double massTol = ms2Tol; if (massTolType == MassToleranceType.Ppm) massTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ms2Tol);
                int minID = -1;

                var startIndex = getStartIndex(mass, 0.1, neutralLossDB);

                for (int i = startIndex; i < neutralLossDB.Count; i++)
                {
                    if (mass - massTol > neutralLossDB[i].Formula.Mass) continue;
                    if (adductIon.IonMode != neutralLossDB[i].Iontype) continue;
                    if (mass + massTol < neutralLossDB[i].Formula.Mass) break;

                    if (isFormulaComposition(neutralLossDB[i].Formula, originalFormula))
                    {
                        if (minDiff > Math.Abs(mass - neutralLossDB[i].Formula.Mass))
                        {
                            minDiff = Math.Abs(mass - neutralLossDB[i].Formula.Mass);
                            minID = i;
                        }
                    }
                }

                if (minID >= 0)
                {
                    neutralLossResult.Add(new NeutralLoss()
                    {
                        Comment = neutralLossDB[minID].Comment,
                        Formula = neutralLossDB[minID].Formula,
                        Iontype = neutralLossDB[minID].Iontype,
                        CandidateInChIKeys = neutralLossDB[minID].CandidateInChIKeys,
                        CandidateOntologies = neutralLossDB[minID].CandidateOntologies,
                        Frequency = neutralLossDB[minID].Frequency,
                        MassLoss = nloss.MassLoss,
                        PrecursorMz = nloss.PrecursorMz,
                        ProductMz = nloss.ProductMz,
                        PrecursorIntensity = nloss.PrecursorIntensity,
                        ProductIntensity = nloss.ProductIntensity,
                        MassError = neutralLossDB[minID].Formula.Mass - mass,
                        Smiles = neutralLossDB[minID].Smiles
                    });
                }
            }
            return neutralLossResult.OrderByDescending(n => Math.Max(n.PrecursorIntensity, n.ProductIntensity)).ToList();
        }

        private static int getStartIndex(double mass, double tol, List<NeutralLoss> neutralLossDB)
        {
            if (neutralLossDB == null || neutralLossDB.Count == 0) return 0;
            double targetMass = mass - tol;
            int startIndex = 0, endIndex = neutralLossDB.Count - 1;
            int counter = 0;

            while (counter < 5) {
                if (neutralLossDB[startIndex].Formula.Mass <= targetMass && targetMass < neutralLossDB[(startIndex + endIndex) / 2].Formula.Mass) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (neutralLossDB[(startIndex + endIndex) / 2].Formula.Mass <= targetMass && targetMass < neutralLossDB[endIndex].Formula.Mass) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        /// <summary>
        /// peaklist should be centroid and refined. For peaklist refining, use GetRefinedPeaklist.
        /// </summary>
        /// <param name="peaklist"></param>
        /// <returns></returns>
        public static List<NeutralLoss> GetNeutralLossList(List<SpectrumPeak> peaklist, double precurosrMz, double masstol)
        {
            if (peaklist == null || peaklist.Count == 0) return new List<NeutralLoss>();

            var neutralLosslist = new List<NeutralLoss>();
            var monoIsotopicPeaklist = new List<SpectrumPeak>();
            var maxNeutralLoss = 1000;

            foreach (var peak in peaklist) if (peak.Comment == "M") monoIsotopicPeaklist.Add(peak);
            monoIsotopicPeaklist = monoIsotopicPeaklist.OrderByDescending(n => n.Mass).ToList();

            var highestMz = monoIsotopicPeaklist[0].Mass;
            if (Math.Abs(highestMz - precurosrMz) > masstol)
                monoIsotopicPeaklist.Insert(0, new SpectrumPeak() { Mass = precurosrMz, Intensity = 1, Comment = "Insearted precursor" });

            for (int i = 0; i < monoIsotopicPeaklist.Count; i++)
            {
                for (int j = i + 1; j < monoIsotopicPeaklist.Count; j++)
                {
                    if (j > monoIsotopicPeaklist.Count - 1) break;
                    if (monoIsotopicPeaklist[i].Mass - monoIsotopicPeaklist[j].Mass < 12 - masstol) continue;

                    neutralLosslist.Add(new NeutralLoss()
                    {
                        MassLoss = monoIsotopicPeaklist[i].Mass - monoIsotopicPeaklist[j].Mass,
                        PrecursorMz = monoIsotopicPeaklist[i].Mass,
                        ProductMz = monoIsotopicPeaklist[j].Mass,
                        PrecursorIntensity = monoIsotopicPeaklist[i].Intensity,
                        ProductIntensity = monoIsotopicPeaklist[j].Intensity
                    });
                }
            }

            if (maxNeutralLoss < neutralLosslist.Count) {
                neutralLosslist = neutralLosslist.OrderByDescending(n => n.ProductIntensity).ToList();
                var filteredList = new List<NeutralLoss>();
                foreach(var peak in neutralLosslist) {
                    filteredList.Add(peak);
                    if (filteredList.Count > maxNeutralLoss) return filteredList.OrderByDescending(n => n.PrecursorMz).ToList();
                }
            }

            return neutralLosslist;
        }

        /// <summary>
        /// peaklist should be centroid.
        /// </summary>
        /// <param name="peaklist"></param>
        /// <param name="analysisParam"></param>
        /// <param name="precursorMz"></param>
        /// <returns></returns>
        public static List<SpectrumPeak> GetRefinedPeaklist(
            List<SpectrumPeak> peaklist, 
            double relativeAbundanceCutOff, 
            double absoluteAbundanceCutOff,
            double precursorMz,
            double ms2Tol, 
            MassToleranceType massTolType, 
            int peakListMax = 1000,
            bool isRemoveIsotopes = false, 
            bool removeAfterPrecursor = true)
        {
            if (peaklist == null || peaklist.Count == 0) return new List<SpectrumPeak>();
            double maxIntensity = getMaxIntensity(peaklist);
            var refinedPeaklist = new List<SpectrumPeak>();

            foreach (var peak in peaklist)
            {
                if (removeAfterPrecursor && peak.Mass > precursorMz + 0.01) break;
                if (peak.Intensity < absoluteAbundanceCutOff) continue;
                if (peak.Intensity >= maxIntensity * relativeAbundanceCutOff * 0.01) { 
                    refinedPeaklist.Add(new SpectrumPeak() { Mass = peak.Mass, Intensity = peak.Intensity, Comment = string.Empty });
                }
            }

            refinedPeaklist = isotopicPeakAssignmnet(refinedPeaklist, ms2Tol, massTolType);

            if (isRemoveIsotopes == false) {
                if (refinedPeaklist.Count > peakListMax) {
                    refinedPeaklist = refinedPeaklist.OrderByDescending(n => n.Intensity).ToList();
                    var filteredList = new List<SpectrumPeak>();
                    foreach (var peak in refinedPeaklist) {
                        filteredList.Add(peak);
                        if (filteredList.Count > peakListMax) return filteredList.OrderBy(n => n.Mass).ToList();
                    }
                }
                else {
                    return refinedPeaklist;
                }
            }
            else {
                var isotopeRemovedRefinedPeaklist = new List<SpectrumPeak>();

                foreach (var peak in refinedPeaklist) {
                    if (peak.Comment == "M")
                        isotopeRemovedRefinedPeaklist.Add(peak);
                }

                if (isotopeRemovedRefinedPeaklist.Count > peakListMax) {
                    isotopeRemovedRefinedPeaklist = isotopeRemovedRefinedPeaklist.OrderByDescending(n => n.Intensity).ToList();
                    var filteredList = new List<SpectrumPeak>();
                    foreach (var peak in isotopeRemovedRefinedPeaklist) {
                        filteredList.Add(peak);
                        if (filteredList.Count > peakListMax) {
                            return filteredList.OrderBy(n => n.Mass).ToList();
                        }
                    }
                }
                else {
                    return isotopeRemovedRefinedPeaklist;
                }
            }
            return refinedPeaklist;
        }

        public static List<SpectrumPeak> GetRefinedPeaklist(List<SpectrumPeak> peaklist, double precursorMz) {
            if (peaklist == null || peaklist.Count == 0) return new List<SpectrumPeak>();
            var refinedPeaklist = new List<SpectrumPeak>();

            foreach (var peak in peaklist) {
                if (peak.Mass > precursorMz + 0.01) break;
                refinedPeaklist.Add(new SpectrumPeak() { Mass = peak.Mass, Intensity = peak.Intensity, Comment = peak.Comment });
            }
            return refinedPeaklist;
        }

        /// <summary>
        /// peaklist should be centroid.
        /// </summary>
        /// <param name="peaklist"></param>
        /// <param name="analysisParam"></param>
        /// <param name="precursorMz"></param>
        /// <returns></returns>
        public static List<SpectrumPeak> GetRefinedPeaklist(List<SpectrumPeak> peaklist, double relativeAbundanceCutOff, double absoluteAbundanceCutOff,
            double precursorMz, double ms2Tol, MassToleranceType massTolType)
        {
            if (peaklist == null || peaklist.Count == 0) return new List<SpectrumPeak>();
            double maxIntensity = getMaxIntensity(peaklist);
            var refinedPeaklist = new List<SpectrumPeak>();

            foreach (var peak in peaklist) {
                if (peak.Mass > precursorMz + 0.01) break;
                if (peak.Intensity >= maxIntensity * relativeAbundanceCutOff * 0.01 && peak.Intensity > absoluteAbundanceCutOff) { 
                    refinedPeaklist.Add(new SpectrumPeak() { Mass = peak.Mass, Intensity = peak.Intensity, Comment = string.Empty }); }
            }

            refinedPeaklist = isotopicPeakAssignmnet(refinedPeaklist, ms2Tol, massTolType);
            return refinedPeaklist;
        }

        /// <summary>
        /// get centroid spectrum: judge if the type is profile or centroid
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static List<SpectrumPeak> GetCentroidMsMsSpectrum(RawData rawData)
        {
            List<SpectrumPeak> ms2Peaklist;

            if (rawData.SpectrumType == MSDataType.Centroid) ms2Peaklist = rawData.Ms2Spectrum;
            else ms2Peaklist = SpectralCentroiding.Centroid(rawData.Ms2Spectrum);

            if (ms2Peaklist == null) return new List<SpectrumPeak>();

            return ms2Peaklist;
        }

        /// <summary>
        /// get centroid spectrum: judge if the type is profile or centroid
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static List<SpectrumPeak> GetCentroidMs1Spectrum(RawData rawData)
        {
            List<SpectrumPeak> ms1Peaklist;

            if (rawData.SpectrumType == MSDataType.Centroid) ms1Peaklist = rawData.Ms1Spectrum;
            else ms1Peaklist = SpectralCentroiding.Centroid(rawData.Ms1Spectrum);

            if (ms1Peaklist == null) return new List<SpectrumPeak>();

            return ms1Peaklist;
        }

        /// <summary>
        /// get centroid spectrum: judge if the type is profile or centroid
        /// </summary>
        /// <param name="peaklist"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public static List<SpectrumPeak> GetCentroidSpectrum(List<SpectrumPeak> peaklist, MSDataType dataType, double threshold)
        {
            List<SpectrumPeak> cPeaklist;

            if (dataType == MSDataType.Centroid) cPeaklist = peaklist;
            else cPeaklist = SpectralCentroiding.Centroid(peaklist, threshold);

            if (cPeaklist == null) return new List<SpectrumPeak>();

            return cPeaklist;
        }

        //private static double hMass = 1.00782503207;
        //private static double cMass = 12;
        //private static double nMass = 14.0030740048;
        //private static double oMass = 15.99491461956;
        //private static double fMass = 18.99840322000;
        //private static double siMass = 27.97692653250;
        //private static double pMass = 30.97376163;
        //private static double sMass = 31.972071;
        //private static double clMass = 34.96885268000;
        //private static double brMass = 78.91833710000;
        //private static double iMass = 126.90447300000;
        
        private static List<Formula> getValenceCheckedFragmentFormulaList(Formula formula, IonMode ionMode, double mass, double massTol)
        {
            var fragmentFormulas = new List<Formula>();
            var hydrogen = 1; if (ionMode == IonMode.Negative) hydrogen = -1;

            double maxHmass = AtomMass.hMass * (formula.Hnum + hydrogen);
            double maxCHmass = maxHmass + AtomMass.cMass * formula.Cnum;
            double maxNCHmass = maxCHmass + AtomMass.nMass * formula.Nnum;
            double maxONCHmass = maxNCHmass + AtomMass.oMass * formula.Onum;
            double maxFONCHmass = maxONCHmass + AtomMass.fMass * formula.Fnum;
            double maxSiFONCHmass = maxFONCHmass + AtomMass.siMass * formula.Sinum;
            double maxPSiFONCHmass = maxSiFONCHmass + AtomMass.pMass * formula.Pnum;
            double maxSPSiFONCHmass = maxPSiFONCHmass + AtomMass.sMass * formula.Snum;
            double maxClSPSiFONCHmass = maxSPSiFONCHmass + AtomMass.clMass * formula.Clnum;
            double maxBrClSPSiFONCHmass = maxClSPSiFONCHmass + AtomMass.brMass * formula.Brnum;

            int fhnum = formula.Hnum;
            for (int inum = 0; inum <= formula.Inum; inum++)
            {
                if ((double)inum * AtomMass.iMass + maxBrClSPSiFONCHmass < mass - massTol) continue;
                if ((double)inum * AtomMass.iMass > mass + massTol) break;
                var uImass = (double)inum * AtomMass.iMass;
                
                for (int brnum = 0; brnum <= formula.Brnum; brnum++)
                {
                    if (uImass + (double)brnum * AtomMass.brMass + maxClSPSiFONCHmass < mass - massTol) continue;
                    if (uImass + (double)brnum * AtomMass.brMass > mass + massTol) break;
                    var uBrmass = uImass + (double)brnum * AtomMass.brMass;

                    for (int clnum = 0; clnum <= formula.Clnum; clnum++)
                    {
                        if (uBrmass + (double)clnum * AtomMass.clMass + maxSPSiFONCHmass < mass - massTol) continue;
                        if (uBrmass + (double)clnum * AtomMass.clMass > mass + massTol) break;
                        var uClmass = uBrmass + (double)clnum * AtomMass.clMass;

                        for (int snum = 0; snum <= formula.Snum; snum++)
                        {
                            if (uClmass + (double)snum * AtomMass.sMass + maxPSiFONCHmass < mass - massTol) continue;
                            if (uClmass + (double)snum * AtomMass.sMass > mass + massTol) break;
                            var uSmass = uClmass + (double)snum * AtomMass.sMass;

                            for (int pnum = 0; pnum <= formula.Pnum; pnum++)
                            {
                                if (uSmass + (double)pnum * AtomMass.pMass + maxSiFONCHmass < mass - massTol) continue;
                                if (uSmass + (double)pnum * AtomMass.pMass > mass + massTol) break;
                                var uPmass = uSmass + (double)pnum * AtomMass.pMass;

                                for (int sinum = 0; sinum <= formula.Sinum; sinum++)
                                {
                                    if (uPmass + (double)sinum * AtomMass.siMass + maxFONCHmass < mass - massTol) continue;
                                    if (uPmass + (double)sinum * AtomMass.siMass > mass + massTol) break;
                                    var uSimass = uPmass + (double)sinum * AtomMass.siMass;

                                    for (int fnum = 0; fnum <= formula.Fnum; fnum++)
                                    {
                                        if (uSimass + (double)fnum * AtomMass.fMass + maxONCHmass < mass - massTol) continue;
                                        if (uSimass + (double)fnum * AtomMass.fMass > mass + massTol) break;
                                        var uFmass = uSimass + (double)fnum * AtomMass.fMass;

                                        for (int onum = 0; onum <= formula.Onum; onum++)
                                        {
                                            if (uFmass + (double)onum * AtomMass.oMass + maxNCHmass < mass - massTol) continue;
                                            if (uFmass + (double)onum * AtomMass.oMass > mass + massTol) break;
                                            var uOmass = uFmass + (double)onum * AtomMass.oMass;

                                            for (int nnum = 0; nnum <= formula.Nnum; nnum++)
                                            {
                                                if (uOmass + (double)nnum * AtomMass.nMass + maxCHmass < mass - massTol) continue;
                                                if (uOmass + (double)nnum * AtomMass.nMass > mass + massTol) break;
                                                var uNmass = uOmass + (double)nnum * AtomMass.nMass;

                                                for (int cnum = 0; cnum <= formula.Cnum; cnum++)
                                                {
                                                    if (uNmass + (double)cnum * AtomMass.cMass + maxHmass < mass - massTol) continue;
                                                    if (uNmass + (double)cnum * AtomMass.cMass > mass + massTol) break;
                                                    var uCmass = uNmass + (double)cnum * AtomMass.cMass;

                                                    for (int hnum = 0; hnum <= fhnum + hydrogen; hnum++)
                                                    {
                                                        if (uCmass + (double)hnum * AtomMass.hMass < mass - massTol) continue;
                                                        if (uCmass + (double)hnum * AtomMass.hMass > mass + massTol) break;
                                                        
                                                        var fragmentFormula = new Formula(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum);
                                                        if (SevenGoldenRulesCheck.ValenceCheckByHydrogenShift(fragmentFormula)) fragmentFormulas.Add(fragmentFormula);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            fragmentFormulas = fragmentFormulas.OrderBy(n => n.Mass).ToList();
            return fragmentFormulas;
        }

        private static List<SpectrumPeak> isotopicPeakAssignmnet(List<SpectrumPeak> peaklist, double massTol, MassToleranceType massTolType)
        {
            var c13_c12_Ratio = 0.010815728;
            
            for (int i = 0; i < peaklist.Count; i++)
            {
                var mass = peaklist[i].Mass;
                var m1Intensity = peaklist[i].Intensity;
                
                var comment = peaklist[i].Comment;
                var maxCarbon = mass % 12;
                var m1TheoreticalCarbonInt = m1Intensity * c13_c12_Ratio * maxCarbon;
                var m2TheoreticalCarbonInt = m1Intensity * maxCarbon * (maxCarbon - 1) * 0.5 * Math.Pow(c13_c12_Ratio, 2);
                var ms2Tol = massTol;
                if (massTolType == MassToleranceType.Ppm) ms2Tol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, massTol);

                if (comment != string.Empty) continue;
                else
                {
                    peaklist[i].Comment = "M";
                    if (i == peaklist.Count - 1) break;
                    var m2Intensity = 0.0;
                    for (int j = i; j < peaklist.Count; j++)
                    {
                        if (mass + MassDiffDictionary.C13_C12_Plus_C13_C12 + ms2Tol <= peaklist[j].Mass) break;

                        //if (mass + MassDiffDictionary.C13_C12 - ms2Tol < peaklist[j].Mz && peaklist[j].Mz < mass + MassDiffDictionary.C13_C12 + ms2Tol && 
                        //    m1TheoreticalCarbonInt * 5.0 > peaklist[j].Intensity) peaklist[j].Comment = "M+1";
                        if (mass + MassDiffDictionary.C13_C12 - ms2Tol < peaklist[j].Mass && 
                            peaklist[j].Mass < mass + MassDiffDictionary.C13_C12 + ms2Tol &&
                            m1Intensity > peaklist[j].Intensity) {
                            peaklist[j].Comment = "M+1";
                            m2Intensity += peaklist[j].Intensity;
                        }

                        if (mass + MassDiffDictionary.N15_N14 - ms2Tol < peaklist[j].Mass &&
                            peaklist[j].Mass < mass + MassDiffDictionary.N15_N14 + ms2Tol &&
                            m1TheoreticalCarbonInt > peaklist[j].Intensity) {
                            peaklist[j].Comment = "M+1";
                            m2Intensity += peaklist[j].Intensity;
                        }

                        //if (mass + MassDiffDictionary.C13_C12_Plus_C13_C12 - ms2Tol < peaklist[j].Mz && peaklist[j].Mz < mass + MassDiffDictionary.C13_C12_Plus_C13_C12 + ms2Tol && 
                        //    m2TheoreticalCarbonInt * 1.3 > peaklist[j].Intensity) peaklist[j].Comment = "M+2";
                        if (m2Intensity > 0.0 && 
                            mass + MassDiffDictionary.C13_C12_Plus_C13_C12 - ms2Tol < peaklist[j].Mass && 
                            peaklist[j].Mass < mass + MassDiffDictionary.C13_C12_Plus_C13_C12 + ms2Tol &&
                            m2Intensity > peaklist[j].Intensity) {
                            peaklist[j].Comment = "M+2";
                        }

                        if (mass + MassDiffDictionary.S34_S32 - ms2Tol < peaklist[j].Mass && 
                            peaklist[j].Mass < mass + MassDiffDictionary.S34_S32 + ms2Tol && 
                            m1Intensity * 0.3 > peaklist[j].Intensity)
                            peaklist[j].Comment = "M+2";

                        if (mass + MassDiffDictionary.O18_O16 - ms2Tol < peaklist[j].Mass && 
                            peaklist[j].Mass < mass + MassDiffDictionary.O18_O16 + ms2Tol && 
                            m1Intensity * 0.1 > peaklist[j].Intensity)
                            peaklist[j].Comment = "M+2";
                        //if (mass + MassDiffDictionary.Cl37_Cl35 - ms2Tol < peaklist[j].Mz &&
                        //    peaklist[j].Mz < mass + MassDiffDictionary.Cl37_Cl35 + ms2Tol && 
                        //    m1Intensity * 0.8 > peaklist[j].Intensity)
                        //    peaklist[j].Comment = "M+2";
                        //if (mass + MassDiffDictionary.Br81_Br79 - ms2Tol < peaklist[j].Mz && 
                        //    peaklist[j].Mz < mass + MassDiffDictionary.Br81_Br79 + ms2Tol && 
                        //    m1Intensity * 1.3 > peaklist[j].Intensity)
                        //    peaklist[j].Comment = "M+2";
                    }
                }
            }
            return peaklist;
        }


        public static List<AnnotatedIon> GetAnnotatedIon(List<SpectrumPeak> peaklist, AdductIon mainAdduct, 
            List<AdductIon> referenceAdductTypeList, double precursorMz, double massTol, MassToleranceType massTolType) {
            var annotations = new List<AnnotatedIon>();
            foreach(var peak in peaklist) {
                annotations.Add(new AnnotatedIon() { AccurateMass = peak.Mass });
            }
            AnnotateIsotopes(peaklist, annotations, massTol, massTolType);
            AnnotateAdducts(peaklist, annotations, mainAdduct, referenceAdductTypeList, precursorMz, massTol, massTolType);
            return annotations;
        }

        public static void AnnotateIsotopes(List<SpectrumPeak> peaklist, List<AnnotatedIon> annotations, double massTol, MassToleranceType massTolType) {
            var c13_c12_Ratio = 0.010815728;
            for (int i = 0; i < peaklist.Count; i++) {
                var mass = peaklist[i].Mass;
                var m1Intensity = peaklist[i].Intensity;

                var maxCarbon = mass % 12;
                var m1TheoreticalCarbonInt = m1Intensity * c13_c12_Ratio * maxCarbon;
                var m2TheoreticalCarbonInt = m1Intensity * maxCarbon * (maxCarbon - 1) * 0.5 * Math.Pow(c13_c12_Ratio, 2);
                var ms2Tol = massTol;
                if (massTolType == MassToleranceType.Ppm) ms2Tol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, massTol);
                if (annotations[i].PeakType == AnnotatedIon.AnnotationType.Product) {
                    if (i == peaklist.Count - 1) break;
                    var m2Intensity = 0.0;
                    var m2mass = 0.0;
                    for (int j = i; j < peaklist.Count; j++) {
                        if (mass + MassDiffDictionary.C13_C12_Plus_C13_C12 + ms2Tol <= peaklist[j].Mass) break;
                        if (m2mass > 0 && m2mass + MassDiffDictionary.C13_C12_Plus_C13_C12 + ms2Tol <= peaklist[j].Mass) m2Intensity = 0;

                        if (mass + MassDiffDictionary.C13_C12 - ms2Tol < peaklist[j].Mass &&
                            peaklist[j].Mass < mass + MassDiffDictionary.C13_C12 + ms2Tol &&
                            m1Intensity > peaklist[j].Intensity) {
                            annotations[j].SetIsotope(mass, peaklist[j].Intensity, m1Intensity, "C-13", 1);
                            m2Intensity = peaklist[j].Intensity;
                            m2mass = peaklist[j].Mass;
                        }

                        else if (mass + MassDiffDictionary.N15_N14 - ms2Tol < peaklist[j].Mass &&
                            peaklist[j].Mass < mass + MassDiffDictionary.N15_N14 + ms2Tol &&
                            m1Intensity > peaklist[j].Intensity) {
                            annotations[j].SetIsotope(mass, peaklist[j].Intensity, m1Intensity, "N-14", 1);
                            m2Intensity = peaklist[j].Intensity;
                            m2mass = peaklist[j].Mass;
                        }

                        if (m2Intensity > 0.0 &&
                            mass + MassDiffDictionary.C13_C12_Plus_C13_C12 - ms2Tol < peaklist[j].Mass &&
                            peaklist[j].Mass < mass + MassDiffDictionary.C13_C12_Plus_C13_C12 + ms2Tol &&
                            m2Intensity > peaklist[j].Intensity) {
                            annotations[j].SetIsotope(mass, peaklist[j].Intensity, m1Intensity, "2C-13", 2);
                        }

                        else if (mass + MassDiffDictionary.S34_S32 - ms2Tol < peaklist[j].Mass &&
                            peaklist[j].Mass < mass + MassDiffDictionary.S34_S32 + ms2Tol &&
                            m1Intensity * 0.3 > peaklist[j].Intensity) {
                            annotations[j].SetIsotope(mass, peaklist[j].Intensity, m1Intensity, "S34", 2);
                        }

                        else if (mass + MassDiffDictionary.O18_O16 - ms2Tol < peaklist[j].Mass &&
                            peaklist[j].Mass < mass + MassDiffDictionary.O18_O16 + ms2Tol &&
                            m1Intensity * 0.1 > peaklist[j].Intensity) {
                            annotations[j].SetIsotope(mass, peaklist[j].Intensity, m1Intensity, "S34", 2);
                        }
                    }
                }
            }
        }

        
        public static void AnnotateAdducts(List<SpectrumPeak> peaklist, List<AnnotatedIon> annotations,
            AdductIon mainAdduct, List<AdductIon> referenceAdductTypeList, double precursorMz, double massTol, MassToleranceType massTolType) {

            for (var i = 0; i < peaklist.Count; i++) {
                var peak = peaklist[i];
                if (annotations[i].PeakType != AnnotatedIon.AnnotationType.Product) continue;
                var centralExactMass = mainAdduct.ConvertToExactMass(peak.Mass);
                var ppm = massTol;
                if (massTolType != MassToleranceType.Ppm)
                    ppm = MolecularFormulaUtility.PpmCalculator(200, 200 + massTol);

                var massTol2 = MolecularFormulaUtility.ConvertPpmToMassAccuracy(precursorMz, ppm);

                var referenceAdductIons = new List<AnnotatedIon>();
                foreach (var targetAdduct in referenceAdductTypeList) {
                    if (mainAdduct.AdductIonName == targetAdduct.AdductIonName) continue;
                    var targetMz = targetAdduct.ConvertToMz(centralExactMass);
                    referenceAdductIons.Add(new AnnotatedIon() { AccurateMass = targetMz, AdductIon = targetAdduct });
                }

                for (var j = 0; j < peaklist.Count; j ++) {
                    if (j == i) continue;
                    var targetPeak = peaklist[j];
                    var targetAnnotation = annotations[j];
                    if (targetAnnotation.PeakType != AnnotatedIon.AnnotationType.Product) continue;

                    foreach (var targetAdduct in referenceAdductIons) {
                        if (Math.Abs(peak.Mass - precursorMz) > massTol2) {
                            if (targetAdduct.AdductIon.AdductIonXmer > 1) continue;
                        }

                        var adductTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(targetPeak.Mass, ppm);
                        
                        if (Math.Abs(targetPeak.Mass - targetAdduct.AccurateMass) < adductTol) {
                            var searchedAdduct = targetAdduct.AdductIon;

                            targetAnnotation.LinkedAccurateMass = peak.Mass;
                            targetAnnotation.AdductIon = targetAdduct.AdductIon;
                            targetAnnotation.PeakType = AnnotatedIon.AnnotationType.Adduct;
                            break;
                        }
                    }
                }
            }
        }

        private static double getMaxIntensity(List<SpectrumPeak> peaklist)
        {
            double maxInt = double.MinValue;
            foreach (var peak in peaklist)
            {
                if (peak.Intensity > maxInt) maxInt = peak.Intensity;
            }
            return maxInt;
        }

        private static bool isFormulaComposition(Formula nlFormula, Formula originFormula)
        {
            if (nlFormula.Cnum <= originFormula.Cnum && nlFormula.Hnum <= originFormula.Hnum && nlFormula.Nnum <= originFormula.Nnum
                && nlFormula.Onum <= originFormula.Onum && nlFormula.Pnum <= originFormula.Pnum && nlFormula.Snum <= originFormula.Snum
                && nlFormula.Fnum <= originFormula.Fnum && nlFormula.Clnum <= originFormula.Clnum && nlFormula.Brnum <= originFormula.Brnum
                && nlFormula.Inum <= originFormula.Inum && nlFormula.Sinum <= originFormula.Sinum)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
