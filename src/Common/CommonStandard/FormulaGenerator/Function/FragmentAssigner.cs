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
        public static List<ProductIon> FastFragmentAssigner(List<SpectrumPeak> peaklist, List<ProductIon> productIonDB,
            Formula formula, double ms2Tol, MassToleranceType massTolType, AdductIon adductIon)
        {
            return FastFragmentAssigner(PrepareProductIonQuery(peaklist, productIonDB, ms2Tol, massTolType, adductIon), formula);
        }

        /// <summary>
        /// Prepares the part of product-ion assignment that does not depend on the candidate formula: the monoisotopic
        /// peaks, their mass tolerances, and which product-ion library entries the library search and the annotation
        /// step look at for each peak. Formula prediction assigns the same spectrum to many candidate formulas, so it
        /// prepares this once per spectrum and passes it to <see cref="FastFragmentAssigner(ProductIonQuery, Formula)"/>.
        /// </summary>
        public static ProductIonQuery PrepareProductIonQuery(List<SpectrumPeak> peaklist, List<ProductIon> productIonDB,
            double ms2Tol, MassToleranceType massTolType, AdductIon adductIon)
        {
            double eMass = electron; if (adductIon.IonMode == IonMode.Negative) eMass = -1.0 * electron;
            var peaks = new List<ProductIonQuery.QueryPeak>();

            foreach (var peak in peaklist)
            {
                if (peak.Comment != "M") continue;

                double mass = peak.Mass + eMass;
                double massTol = ms2Tol;
                if (massTolType == MassToleranceType.Ppm)
                    massTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ms2Tol);

                peaks.Add(new ProductIonQuery.QueryPeak(peak, mass, massTol,
                    getLibrarySearchIndices(adductIon.IonMode, peak.Mass, massTol, productIonDB),
                    getAnnotationIndices(adductIon.IonMode, peak.Mass, productIonDB)));
            }

            return new ProductIonQuery(productIonDB, adductIon, peaks.ToArray());
        }

        /// <summary>
        /// Assigns a fragment formula of <paramref name="formula"/> to every monoisotopic peak of the prepared spectrum.
        /// Gives the same result as <see cref="FastFragmentAssigner(List{SpectrumPeak}, List{ProductIon}, Formula, double, MassToleranceType, AdductIon)"/>.
        /// </summary>
        public static List<ProductIon> FastFragmentAssigner(ProductIonQuery query, Formula formula)
        {
            var productIons = new List<ProductIon>();
            var adductIon = query.AdductIon;
            var productIonDB = query.ProductIonDB;

            foreach (var queryPeak in query.Peaks)
            {
                var peak = queryPeak.Peak;
                double mass = queryPeak.Mass;
                double massTol = queryPeak.MassTol;
                ProductIon productIon;

                //for precursor annotation
                if (Math.Abs(mass - formula.Mass - adductIon.AdductIonAccurateMass) < massTol)
                {
                    var nFormula = MolecularFormulaUtility.ConvertFormulaAdductPairToPrecursorAdduct(formula, adductIon);
                    productIon = new ProductIon() { Formula = nFormula, Mass = peak.Mass, MassDiff = formula.Mass + adductIon.AdductIonAccurateMass - mass, Intensity = peak.Intensity };
                }
                else
                {
                    //library search: the closest library formula that fits in the candidate formula, the first one on ties
                    Formula fragmentFormula = null;
                    double minDiff = double.MaxValue;
                    foreach (var i in queryPeak.LibrarySearchIndices)
                    {
                        var libraryFormula = productIonDB[i].Formula;
                        if (!isFormulaComposition(libraryFormula, formula)) continue;
                        if (minDiff > Math.Abs(mass - libraryFormula.Mass))
                        {
                            fragmentFormula = libraryFormula;
                            minDiff = Math.Abs(mass - libraryFormula.Mass);
                        }
                    }

                    //otherwise the closest valence-checked sub-formula
                    if (fragmentFormula == null)
                    {
                        FragmentCounts counts;
                        if (!searchValenceCheckedFragmentFormula(formula, adductIon.IonMode, peak.Mass, massTol, mass, false, out counts, null)) continue;
                        fragmentFormula = new Formula(counts.C, counts.H, counts.N, counts.O, counts.P, counts.S, counts.F, counts.Cl, counts.Br, counts.I, counts.Si);
                    }

                    productIon = new ProductIon() {
                        Formula = fragmentFormula,
                        Mass = peak.Mass,
                        MassDiff = fragmentFormula.Mass - mass,
                        Intensity = peak.Intensity
                    };
                }
                productIons.Add(productIon);

                foreach (var i in queryPeak.AnnotationIndices) {
                    var ionQuery = productIonDB[i];
                    if (isFormulaComposition(productIon.Formula, ionQuery.Formula)) {
                        productIon.CandidateInChIKeys = ionQuery.CandidateInChIKeys;
                        productIon.CandidateOntologies = ionQuery.CandidateOntologies;
                        productIon.Frequency = ionQuery.Frequency;
                        break;
                    }
                }
            }

            return productIons;
        }

        /// <summary>
        /// The number of product ions <see cref="FastFragmentAssigner(ProductIonQuery, Formula)"/> returns for
        /// <paramref name="formula"/>, without building them. A peak gets an ion when it matches the precursor, when a
        /// library formula fits in the candidate, or when any valence-checked sub-formula is within tolerance, so this
        /// only has to find one of them per peak.
        /// </summary>
        public static int CountProductIons(ProductIonQuery query, Formula formula)
        {
            var count = 0;
            var adductIon = query.AdductIon;
            var productIonDB = query.ProductIonDB;
            double[] maxRemainingMasses = null;

            foreach (var queryPeak in query.Peaks)
            {
                if (Math.Abs(queryPeak.Mass - formula.Mass - adductIon.AdductIonAccurateMass) < queryPeak.MassTol)
                {
                    count++;
                    continue;
                }

                var isLibraryHit = false;
                foreach (var i in queryPeak.LibrarySearchIndices)
                {
                    if (isFormulaComposition(productIonDB[i].Formula, formula))
                    {
                        isLibraryHit = true;
                        break;
                    }
                }

                if (isLibraryHit || hasValenceCheckedFragmentFormula(queryPeak, formula, adductIon.IonMode, ref maxRemainingMasses))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Whether <see cref="searchValenceCheckedFragmentFormula"/> finds a sub-formula of <paramref name="formula"/> for
        /// the peak, answered from sub-formulas found for earlier candidates when one of them fits.
        /// </summary>
        /// <remarks>
        /// The enumeration emits a sub-formula when its counts are within the candidate's (hydrogen: one more in
        /// positive mode, one fewer in negative mode), its mass is in the window, it passes the valence check, and no
        /// level skips it, a level skipping when its partial mass plus the candidate's largest remaining mass is below
        /// the window. Only the counts and the skip tests depend on the candidate, so a sub-formula found for an
        /// earlier candidate (a witness) answers for this one when its counts fit and the skip tests, evaluated with
        /// the same values as the loops, pass.
        /// </remarks>
        private static bool hasValenceCheckedFragmentFormula(ProductIonQuery.QueryPeak queryPeak, Formula formula, IonMode ionMode,
            ref double[] maxRemainingMasses)
        {
            var hydrogen = 1; if (ionMode == IonMode.Negative) hydrogen = -1;
            var cache = queryPeak.ExistenceCache;
            var lowerMass = queryPeak.Peak.Mass - queryPeak.MassTol;

            for (int k = 0; k < cache.WitnessCount; k++)
            {
                var w = cache.WitnessCounts[k];
                if (w.C > formula.Cnum || w.H > formula.Hnum + hydrogen || w.N > formula.Nnum || w.O > formula.Onum || w.P > formula.Pnum
                    || w.S > formula.Snum || w.F > formula.Fnum || w.Cl > formula.Clnum || w.Br > formula.Brnum || w.I > formula.Inum || w.Si > formula.Sinum)
                    continue;
                if (maxRemainingMasses == null) maxRemainingMasses = getMaxRemainingMasses(formula, hydrogen);
                var partialMasses = cache.WitnessPartialMasses[k];
                var isSkipped = false;
                for (int level = 0; level < FragmentExistenceCache.LevelCount; level++)
                {
                    if (partialMasses[level] + maxRemainingMasses[level] < lowerMass)
                    {
                        isSkipped = true;
                        break;
                    }
                }
                if (!isSkipped) return true;
            }

            FragmentCounts counts;
            if (searchValenceCheckedFragmentFormula(formula, ionMode, queryPeak.Peak.Mass, queryPeak.MassTol, queryPeak.Mass, true, out counts, cache.PartialMassBuffer))
            {
                cache.AddWitness(counts);
                return true;
            }
            return false;
        }

        /// <summary>
        /// The largest mass the elements after each level (I, Br, Cl, S, P, Si, F, O, N, C) can add, as the
        /// enumeration computes it for <paramref name="formula"/>.
        /// </summary>
        private static double[] getMaxRemainingMasses(Formula formula, int hydrogen)
        {
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
            return new[] {
                maxBrClSPSiFONCHmass, maxClSPSiFONCHmass, maxSPSiFONCHmass, maxPSiFONCHmass, maxSiFONCHmass,
                maxFONCHmass, maxONCHmass, maxNCHmass, maxCHmass, maxHmass,
            };
        }

        /// <summary>
        /// The product-ion library entries that the library search reaches the composition check for, in scan order.
        /// </summary>
        private static int[] getLibrarySearchIndices(IonMode ionMode, double mz, double massTol, List<ProductIon> productIonDB) {
            var indices = new List<int>();
            var startIndex = getStartIndex(mz, massTol, productIonDB);
            for (int i = startIndex; i < productIonDB.Count; i++) {
                var ionQuery = productIonDB[i];
                if (ionQuery.IonMode != ionMode) continue;
                if (ionQuery.Formula.Mass < mz - massTol) continue;
                if (ionQuery.Formula.Mass > mz + massTol) break;
                indices.Add(i);
            }
            return indices.ToArray();
        }

        /// <summary>
        /// The product-ion library entries that annotating an ion assigned at <paramref name="mz"/> looks at, in scan order.
        /// </summary>
        private static int[] getAnnotationIndices(IonMode ionMode, double mz, List<ProductIon> productIonDB) {
            var indices = new List<int>();
            var startIndex = getStartIndex(mz, 0.1, productIonDB);
            for (int i = startIndex; i < productIonDB.Count; i++) {
                var ionQuery = productIonDB[i];
                if (ionQuery.IonMode != ionMode) continue;
                if (ionQuery.Formula.Mass > mz + 0.1) break;
                indices.Add(i);
            }
            return indices.ToArray();
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
            return FastNeutralLossAssigner(PrepareNeutralLossQuery(neutralLosslist, neutralLossDB, ms2Tol, massTolType, adductIon), originalFormula);
        }

        /// <summary>
        /// Prepares the part of neutral-loss assignment that does not depend on the candidate formula: for every observed
        /// neutral loss, which neutral-loss library entries are within its mass tolerance and of the right ion mode.
        /// </summary>
        public static NeutralLossQuery PrepareNeutralLossQuery(List<NeutralLoss> neutralLosslist, List<NeutralLoss> neutralLossDB,
            double ms2Tol, MassToleranceType massTolType, AdductIon adductIon)
        {
            // Losses without any library entry in range can never be assigned, so they are left out.
            var entries = new List<NeutralLossQuery.QueryLoss>();
            for (int n = 0; n < neutralLosslist.Count; n++)
            {
                var nloss = neutralLosslist[n];
                double mass = nloss.MassLoss;
                double massTol = ms2Tol; if (massTolType == MassToleranceType.Ppm) massTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ms2Tol);

                var indices = new List<int>();
                var startIndex = getStartIndex(mass, 0.1, neutralLossDB);
                for (int i = startIndex; i < neutralLossDB.Count; i++)
                {
                    if (mass - massTol > neutralLossDB[i].Formula.Mass) continue;
                    if (adductIon.IonMode != neutralLossDB[i].Iontype) continue;
                    if (mass + massTol < neutralLossDB[i].Formula.Mass) break;
                    indices.Add(i);
                }
                if (indices.Count > 0)
                    entries.Add(new NeutralLossQuery.QueryLoss(nloss, indices.ToArray()));
            }
            return new NeutralLossQuery(neutralLossDB, entries.ToArray());
        }

        /// <summary>
        /// The number of distinct formulas, hydrogen ignored, among the neutral losses
        /// <see cref="FastNeutralLossAssigner(NeutralLossQuery, Formula)"/> assigns to <paramref name="originalFormula"/>:
        /// the neutral-loss hit count of formula scoring, without building the assignments.
        /// </summary>
        public static int CountUniqueNeutralLossFormulas(NeutralLossQuery query, Formula originalFormula)
        {
            var neutralLossDB = query.NeutralLossDB;
            List<Formula> uniqueFormulas = null;

            foreach (var queryLoss in query.Losses)
            {
                double mass = queryLoss.NeutralLoss.MassLoss;
                double minDiff = double.MaxValue;
                int minID = -1;

                foreach (var i in queryLoss.LibraryIndices)
                {
                    if (isFormulaComposition(neutralLossDB[i].Formula, originalFormula))
                    {
                        if (minDiff > Math.Abs(mass - neutralLossDB[i].Formula.Mass))
                        {
                            minDiff = Math.Abs(mass - neutralLossDB[i].Formula.Mass);
                            minID = i;
                        }
                    }
                }
                if (minID < 0) continue;

                // Formulas are the same when every count but hydrogen matches; that grouping does not depend on
                // the order of the losses, so counting here equals counting the sorted assignment list.
                var lossFormula = neutralLossDB[minID].Formula;
                if (uniqueFormulas == null) uniqueFormulas = new List<Formula>();
                var isKnown = false;
                foreach (var known in uniqueFormulas)
                {
                    if (MolecularFormulaUtility.isFormulaMatch(known, lossFormula, true))
                    {
                        isKnown = true;
                        break;
                    }
                }
                if (!isKnown) uniqueFormulas.Add(lossFormula);
            }
            return uniqueFormulas == null ? 0 : uniqueFormulas.Count;
        }

        /// <summary>
        /// Assigns library neutral losses that fit in <paramref name="originalFormula"/> to the prepared neutral losses.
        /// Gives the same result as <see cref="FastNeutralLossAssigner(List{NeutralLoss}, List{NeutralLoss}, Formula, double, MassToleranceType, AdductIon)"/>.
        /// </summary>
        public static List<NeutralLoss> FastNeutralLossAssigner(NeutralLossQuery query, Formula originalFormula)
        {
            var neutralLossResult = new List<NeutralLoss>();
            var neutralLossDB = query.NeutralLossDB;

            foreach (var queryLoss in query.Losses)
            {
                var nloss = queryLoss.NeutralLoss;
                double mass = nloss.MassLoss;
                double minDiff = double.MaxValue;
                int minID = -1;

                foreach (var i in queryLoss.LibraryIndices)
                {
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
        
        /// <summary>
        /// Enumerates the sub-formulas of <paramref name="formula"/> (one hydrogen more in positive mode, one fewer in
        /// negative mode) whose mass is within <paramref name="massTol"/> of <paramref name="mass"/> and that pass the
        /// valence check, and finds the one closest to <paramref name="targetMass"/>. Returns false when there is none.
        /// With <paramref name="anyIsEnough"/>, it returns at the first such formula, for callers that only need to
        /// know whether one exists.
        /// </summary>
        /// <remarks>
        /// This used to build every such formula, sort them by mass and take the first with the smallest difference.
        /// It now keeps only the best one while enumerating: the smallest difference, then the smaller mass, then the
        /// earlier enumerated formula, which is the formula the sorted list would have given first. The loops start
        /// at the first count that can still reach the mass window (see <see cref="EnumerationBound.FirstNotBelow"/>),
        /// and no <see cref="Formula"/> is built here.
        /// </remarks>
        private static bool searchValenceCheckedFragmentFormula(Formula formula, IonMode ionMode, double mass, double massTol, double targetMass,
            bool anyIsEnough, out FragmentCounts best, double[] witnessPartialMasses)
        {
            best = default(FragmentCounts);
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

            double lowerMass = mass - massTol;
            double upperMass = mass + massTol;
            int maxHnum = formula.Hnum + hydrogen;

            var found = false;
            double bestDiff = 0.0, bestMass = 0.0;

            for (int inum = EnumerationBound.FirstNotBelow(0.0, AtomMass.iMass, maxBrClSPSiFONCHmass, lowerMass, formula.Inum); inum <= formula.Inum; inum++)
            {
                if ((double)inum * AtomMass.iMass > upperMass) break;
                var uImass = (double)inum * AtomMass.iMass;

                for (int brnum = EnumerationBound.FirstNotBelow(uImass, AtomMass.brMass, maxClSPSiFONCHmass, lowerMass, formula.Brnum); brnum <= formula.Brnum; brnum++)
                {
                    if (uImass + (double)brnum * AtomMass.brMass > upperMass) break;
                    var uBrmass = uImass + (double)brnum * AtomMass.brMass;

                    for (int clnum = EnumerationBound.FirstNotBelow(uBrmass, AtomMass.clMass, maxSPSiFONCHmass, lowerMass, formula.Clnum); clnum <= formula.Clnum; clnum++)
                    {
                        if (uBrmass + (double)clnum * AtomMass.clMass > upperMass) break;
                        var uClmass = uBrmass + (double)clnum * AtomMass.clMass;

                        for (int snum = EnumerationBound.FirstNotBelow(uClmass, AtomMass.sMass, maxPSiFONCHmass, lowerMass, formula.Snum); snum <= formula.Snum; snum++)
                        {
                            if (uClmass + (double)snum * AtomMass.sMass > upperMass) break;
                            var uSmass = uClmass + (double)snum * AtomMass.sMass;

                            for (int pnum = EnumerationBound.FirstNotBelow(uSmass, AtomMass.pMass, maxSiFONCHmass, lowerMass, formula.Pnum); pnum <= formula.Pnum; pnum++)
                            {
                                if (uSmass + (double)pnum * AtomMass.pMass > upperMass) break;
                                var uPmass = uSmass + (double)pnum * AtomMass.pMass;

                                for (int sinum = EnumerationBound.FirstNotBelow(uPmass, AtomMass.siMass, maxFONCHmass, lowerMass, formula.Sinum); sinum <= formula.Sinum; sinum++)
                                {
                                    if (uPmass + (double)sinum * AtomMass.siMass > upperMass) break;
                                    var uSimass = uPmass + (double)sinum * AtomMass.siMass;

                                    for (int fnum = EnumerationBound.FirstNotBelow(uSimass, AtomMass.fMass, maxONCHmass, lowerMass, formula.Fnum); fnum <= formula.Fnum; fnum++)
                                    {
                                        if (uSimass + (double)fnum * AtomMass.fMass > upperMass) break;
                                        var uFmass = uSimass + (double)fnum * AtomMass.fMass;

                                        for (int onum = EnumerationBound.FirstNotBelow(uFmass, AtomMass.oMass, maxNCHmass, lowerMass, formula.Onum); onum <= formula.Onum; onum++)
                                        {
                                            if (uFmass + (double)onum * AtomMass.oMass > upperMass) break;
                                            var uOmass = uFmass + (double)onum * AtomMass.oMass;

                                            for (int nnum = EnumerationBound.FirstNotBelow(uOmass, AtomMass.nMass, maxCHmass, lowerMass, formula.Nnum); nnum <= formula.Nnum; nnum++)
                                            {
                                                if (uOmass + (double)nnum * AtomMass.nMass > upperMass) break;
                                                var uNmass = uOmass + (double)nnum * AtomMass.nMass;

                                                for (int cnum = EnumerationBound.FirstNotBelow(uNmass, AtomMass.cMass, maxHmass, lowerMass, formula.Cnum); cnum <= formula.Cnum; cnum++)
                                                {
                                                    if (uNmass + (double)cnum * AtomMass.cMass > upperMass) break;
                                                    var uCmass = uNmass + (double)cnum * AtomMass.cMass;

                                                    for (int hnum = EnumerationBound.FirstNotBelow(uCmass, AtomMass.hMass, 0.0, lowerMass, maxHnum); hnum <= maxHnum; hnum++)
                                                    {
                                                        if (uCmass + (double)hnum * AtomMass.hMass > upperMass) break;
                                                        if (!SevenGoldenRulesCheck.ValenceCheckByHydrogenShift(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum)) continue;

                                                        // The mass a Formula would carry, which is what the candidates were sorted and compared by.
                                                        var exactMass = FormulaCalculateUtility.GetExactMass(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum);
                                                        var diff = Math.Abs(targetMass - exactMass);
                                                        if (!found || diff < bestDiff || (diff == bestDiff && exactMass < bestMass))
                                                        {
                                                            found = true;
                                                            bestDiff = diff; bestMass = exactMass;
                                                            best.C = cnum; best.H = hnum; best.N = nnum; best.O = onum; best.P = pnum; best.S = snum;
                                                            best.F = fnum; best.Cl = clnum; best.Br = brnum; best.I = inum; best.Si = sinum;
                                                            if (anyIsEnough)
                                                            {
                                                                if (witnessPartialMasses != null)
                                                                {
                                                                    witnessPartialMasses[0] = uImass; witnessPartialMasses[1] = uBrmass; witnessPartialMasses[2] = uClmass;
                                                                    witnessPartialMasses[3] = uSmass; witnessPartialMasses[4] = uPmass; witnessPartialMasses[5] = uSimass;
                                                                    witnessPartialMasses[6] = uFmass; witnessPartialMasses[7] = uOmass; witnessPartialMasses[8] = uNmass;
                                                                    witnessPartialMasses[9] = uCmass;
                                                                }
                                                                return true;
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
                }
            }

            return found;
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
