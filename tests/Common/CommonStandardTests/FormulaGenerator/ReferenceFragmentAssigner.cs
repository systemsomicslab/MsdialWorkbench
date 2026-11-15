// Generated from the CommonStandard FragmentAssigner before the formula prediction speed-up
// (MsdialWorkbench 4ff7e299b). Kept verbatim as the reference the optimised code is compared with.
using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.FormulaGenerator.Tests
{
    internal static class ReferenceFragmentAssigner
    {
        private static double electron = 0.0005485799;

        public static List<ProductIon> FastFragmentAssigner(List<SpectrumPeak> peaklist, List<ProductIon> productIonDB, 
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
                                                        if (ReferenceGoldenRules.ValenceCheckByHydrogenShift(fragmentFormula)) fragmentFormulas.Add(fragmentFormula);
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
