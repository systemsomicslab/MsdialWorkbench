using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Statistics.Testing;
using Accord.Statistics.Analysis;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Parameter;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Ion;

namespace CompMs.Common.FormulaGenerator.Function {
    
    public class ChemicalOntologyResultTemp {
        public int ID { get; set; }
        public double Score { get; set; }
    }
    
    //https://www.slideshare.net/h_yama2396/ss-54462336 page 62
    //https://en.wikipedia.org/wiki/Jaccard_index
    public sealed class ChemicalOntologyAnnotation {

        public const int TotalFragmentOntologyCount = 459;

        public static void ProcessByJaccard(List<FormulaResult> results, List<ChemicalOntology> chemicalOntologies, IonMode ionmode, double cutoff) {
            Parallel.ForEach(results, result => {

                var maxJaccard = 0.0;
                var maxID = -1;

                SeekPutativeChemicalOntologyByJaccard(result, chemicalOntologies, ionmode, cutoff, out maxJaccard, out maxID);

                //if (maxID >= 0) {
                //    result.ChemicalOntologyID = chemicalOntologies[maxID].OntologyID;
                //    result.ChemicalOntologyDescription = chemicalOntologies[maxID].OntologyDescription;
                //    result.ChemicalOntologyScore = Math.Round(maxJaccard, 3);
                //}
            });
        }

        public static void ProcessByOverRepresentationAnalysis(List<FormulaResult> results, List<ChemicalOntology> chemicalOntologies, 
            IonMode ionmode, AnalysisParamOfMsfinder param, AdductIon adduct,
            List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB) {
            Parallel.ForEach(results, result => {

                var chemResults = GetSignificantChemicalOntologies(result, chemicalOntologies, ionmode, param, adduct, productIonDB, neutralLossDB);

                if (chemResults.Count > 0) {
                    for (int i = 0; i < chemResults.Count; i++) {
                        var score = chemResults[i].Score;
                        var chemOnt = chemicalOntologies[chemResults[i].ID];

                        if (result.ChemicalOntologyIDs.Contains(chemOnt.OntologyID)) continue;

                        result.ChemicalOntologyIDs.Add(chemOnt.OntologyID);
                        result.ChemicalOntologyDescriptions.Add(chemOnt.OntologyDescription);
                        result.ChemicalOntologyScores.Add(score);
                        result.ChemicalOntologyRepresentativeInChIKeys.Add(chemOnt.RepresentativeInChIKey);
                    }
                }
            });
        }

        public static void SeekPutativeChemicalOntologyByJaccard(FormulaResult result, List<ChemicalOntology> chemicalOntologies, 
            IonMode ionMode, double cutoff, out double maxJaccard, out int maxID) {

            maxJaccard = 0.0;
            maxID = -1;

            for (int i = 0; i < chemicalOntologies.Count; i++) {
                var chemOnt = chemicalOntologies[i];
                if (chemOnt.IonMode != ionMode)
                    continue;
                if (!isFormulaElementCorrect(result.Formula, chemOnt.Formula))
                    continue;

                var jaccard = CalculateJaccardValue(result, chemOnt);
                if (jaccard > cutoff && jaccard > maxJaccard) {
                    maxJaccard = jaccard;
                    maxID = i;
                }
            }
        }

        public static List<ChemicalOntologyResultTemp> GetSignificantChemicalOntologies(FormulaResult result, List<ChemicalOntology> chemicalOntologies,
            IonMode ionMode, AnalysisParamOfMsfinder param, AdductIon adduct,
            List<ProductIon> productIonDB, List<NeutralLoss> neutralLossDB) {

            var chemResults = new List<ChemicalOntologyResultTemp>();

            var revSpectrum = GetReversedMsmsSpectrum(result.ProductIonResult);
            var revNeutralLosses = GetReversedNeutralLosses(revSpectrum);
            var revProductIons = FragmentAssigner.FastFragmnetAssigner(revSpectrum, productIonDB, result.Formula, param.Mass2Tolerance, param.MassTolType, adduct);
            var revAssignedNeutrallosses = FragmentAssigner.FastNeutralLossAssigner(revNeutralLosses, neutralLossDB, result.Formula, param.Mass2Tolerance, param.MassTolType, adduct);

            var revFormulaResult = new FormulaResult() {
                Formula = result.Formula,
                ProductIonResult = revProductIons,
                NeutralLossResult = revAssignedNeutrallosses
            };

            for (int i = 0; i < chemicalOntologies.Count; i++) {

                var chemOnt = chemicalOntologies[i];
                if (chemOnt.IonMode != ionMode)
                    continue;
                if (!isFormulaElementCorrect(result.Formula, chemOnt.Formula))
                    continue;

                var oraPvalue = CalculateOverRepresentationAnalysisValue(result, chemOnt, ionMode, param.FseanonsignificantDef, revFormulaResult);
                //Debug.WriteLine(oraPvalue);
                if (oraPvalue < param.FseaPvalueCutOff * 0.01) {
                    chemResults.Add(new ChemicalOntologyResultTemp() { ID = i, Score = oraPvalue });
                }
            }

            if (chemResults.Count > 0)
                chemResults = chemResults.OrderBy(n => n.Score).ToList();
            return chemResults;
        }

        private static List<NeutralLoss> GetReversedNeutralLosses(List<SpectrumPeak> revSpectrum) {
            if (revSpectrum == null || revSpectrum.Count == 0) return new List<NeutralLoss>();
            var neutralLosses = new List<NeutralLoss>();
            for (int i = 0; i < revSpectrum.Count; i++) {
                for (int j = 1; j < revSpectrum.Count; j++) {
                    if (j > revSpectrum.Count - 1) break;
                    if (revSpectrum[i].Mass - revSpectrum[j].Mass < 13) continue;

                    neutralLosses.Add(new NeutralLoss() {
                        MassLoss = revSpectrum[i].Mass - revSpectrum[j].Mass,
                        PrecursorMz = revSpectrum[i].Mass,
                        ProductMz = revSpectrum[j].Mass,
                        PrecursorIntensity = revSpectrum[i].Intensity,
                        ProductIntensity = revSpectrum[j].Intensity
                    });
                }
            }
            return neutralLosses;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="productIons"></param>
        /// <returns></returns>
        public static List<SpectrumPeak> GetReversedMsmsSpectrum(List<ProductIon> productIons) {
            if (productIons.Count == 0) return new List<SpectrumPeak>();

            var shiftMass = 5;
            var maxMz = (int)productIons.Max(n => n.Mass);
            var minMz = (int)productIons.Min(n => n.Mass);
            var centerMz = (int)((maxMz - minMz) % 0.5);

            var revSpectrum = new List<SpectrumPeak>();

            foreach (var ion in productIons) {
                var revMz = (double)(ion.Mass % 1) + (double)(centerMz + (centerMz - ion.Mass) + shiftMass);
                var peak = new SpectrumPeak() { Mass = revMz, Intensity = ion.Intensity, Comment = "M" };
                revSpectrum.Add(peak);
            }
            return revSpectrum;
        }

        public static double CalculateOverRepresentationAnalysisValue(FormulaResult result, ChemicalOntology chemOntology, 
            IonMode ionMode, FseaNonsignificantDef def, FormulaResult revResult = null) {

            //to make 'cross matrix'
            var a11 = 0; //k -> kvalue
            var a12 = 0;
            var a22 = 0; //d -> dValue
            var a21 = 0; //n - k -> nkValue

            //checking the ontologies count of result found in this chemOntology
            var a11Ontologies = new List<string>();
            var a21Ontoloties = new List<string>();
            FitSignificantOntologiesToFragmentSet(result, chemOntology, out a11Ontologies, out a21Ontoloties);

            var a12Ontologies = new List<string>();
            var a22Ontoloties = new List<string>();

            a11 = a11Ontologies.Count;
            a21 = a21Ontoloties.Count;

            if (def == FseaNonsignificantDef.ReverseSpectrum) {
                FitSignificantOntologiesToFragmentSet(revResult, chemOntology, out a12Ontologies, out a22Ontoloties);
                a12 = a12Ontologies.Count;
                a22 = a22Ontoloties.Count;
            }
            else if (def == FseaNonsignificantDef.LowAbundantIons) {
                FitNotsignificantOntologiesToFragmentSet(result, a11Ontologies, chemOntology, out a12Ontologies, out a22Ontoloties);
                a12 = a12Ontologies.Count;
                a22 = a22Ontoloties.Count;
            }
            else {
                a12 = chemOntology.FragmentOntologies.Count - a11;
                a22 = TotalFragmentOntologyCount - a11 - a12 - a21;
            }

            if (a22 <= 0) {
                a12 = chemOntology.FragmentOntologies.Count - a11;
                a22 = TotalFragmentOntologyCount - a11 - a12 - a21;
            }

            var isValidated = IsRequiredFragmentOntology(chemOntology.OntologyDescription, a11Ontologies, ionMode);
            if (isValidated == false) return 1.0;

            if (a11 < 0) a11 = 0;
            if (a12 < 0) a12 = 0;
            if (a21 < 0) a21 = 0;
            if (a22 < 0) a22 = 0;

            if (a11 == 0) return 1.0;

            var confMatrix = new ConfusionMatrix(new int[2, 2] {
                { a11, a12 },
                { a21, a22 }
            });
            var fisherTest = new FisherExactTest(confMatrix, OneSampleHypothesis.ValueIsGreaterThanHypothesis);

            return fisherTest.PValue;
        }

        private static bool IsRequiredFragmentOntology(string chemOntology, List<string> detectedOntologies, IonMode ionMode) {

            var lowerOnt = chemOntology.ToLower();
            if (lowerOnt.Contains("phosphate") && !lowerOnt.Contains("thiophosphate")) {

                var detectedMarker = detectedOntologies.Count(n => n.Contains("PO3") || n.Contains("PO4"));
                if (detectedMarker > 0) return true;
                else return false;
            }
            else if (lowerOnt.Contains("glucosinolate")) {

                var detectedMarker = detectedOntologies.Count(n => n.Contains("SO4") || n.Contains("Alkylthiols"));
                if (detectedMarker > 0) return true;
                else return false;
            }
            else if (lowerOnt.Contains("pc lipid")) {

                var detectedMarker = detectedOntologies.Count(n => n.Contains("PC-lipid head") || n.Contains("Alkanes") || n.Contains("Phosphocholines"));
                if (detectedMarker > 0) return true;
                else return false;
            }
            else if (lowerOnt.Contains("pi lipid")) {

                var detectedMarker = detectedOntologies.Count(n => n.Contains("PI-lipid head"));
                if (detectedMarker > 0) return true;
                else return false;
            }
            else if (lowerOnt.Contains("ps lipid")) {

                var detectedMarker = detectedOntologies.Count(n => n.Contains("PS-lipid head") || n.Contains("Alanine and derivatives"));
                if (detectedMarker > 0) return true;
                else return false;
            }
            else if (lowerOnt.Contains("pg lipid")) {

                var detectedMarker = detectedOntologies.Count(n => n.Contains("PG-lipid head") || n.Contains("Fatty aldehydes"));
                if (detectedMarker > 0) return true;
                else return false;
            }
            else if (lowerOnt.Contains("pa lipid")) {

                var detectedMarker = detectedOntologies.Count(n => n.Contains("Fatty aldehydes"));
                if (detectedMarker > 0) return true;
                else return false;
            }
            else if (lowerOnt.Contains("pe lipid")) {

                var detectedMarker = detectedOntologies.Count(n => n.Contains("PE-lipid head") || n.Contains("Fatty aldehydes") || n.Contains("Medium-chain aldehydes"));
                if (detectedMarker > 0) return true;
                else return false;
            }
            else if (lowerOnt.Contains("sqdg lipid")) {

                var detectedMarker = detectedOntologies.Count(n => n.Contains("SQDG-lipid head") || n.Contains("fatty acids"));
                if (detectedMarker > 0) return true;
                else return false;
            }
            else if (lowerOnt.Contains("dgdg lipid")) {

                var detectedMarker = detectedOntologies.Count(n => n.Contains("DGDG-lipid head"));
                if (detectedMarker > 0) return true;
                else return false;
            }
            else if (lowerOnt.Contains("mgdg lipid")) {

                var detectedMarker = detectedOntologies.Count(n => n.Contains("Glycosyl") || n.Contains("O-glycosyl"));
                if (detectedMarker > 0) return true;
                else return false;
            }
            else {
                return true;
            }

        }

        /// <summary>
        /// currently, 'significant peak' is determined as the peak having >=1% relative abundance from the base peak
        /// </summary>
        public static void FitSignificantOntologiesToFragmentSet(FormulaResult result, ChemicalOntology chemOntology, 
            out List<string> a11Ontologies, out List<string> a21Ontologies) {

            a11Ontologies = new List<string>();
            a21Ontologies = new List<string>();
            if (result.ProductIonResult != null && result.ProductIonResult.Count > 0) {
                var maxIntensity = result.ProductIonResult.Max(n => n.Intensity);
                foreach (var ion in result.ProductIonResult) {
                    if (ion.Intensity >= maxIntensity * 0.05) {

                        if (ion.CandidateOntologies != null && ion.CandidateOntologies.Count > 0) {

                            var flg = false;
                            foreach (var ontology in ion.CandidateOntologies) {
                                if (chemOntology.FragmentOntologies.Contains(ontology)) {
                                    if (!a11Ontologies.Contains(ontology)) a11Ontologies.Add(ontology);
                                    flg = true;
                                    break;
                                }
                            }

                            if (flg == false) { //if false, we have to register the significant ion which does not be included in the fragment set.
                                var candidateString = string.Empty;
                                var isAleardyDetected = false;
                                foreach (var ontology in ion.CandidateOntologies) {
                                    foreach (var a21Ont in a21Ontologies) {
                                        if (a21Ont.Contains(ontology)) {
                                            isAleardyDetected = true;
                                            break;
                                        }
                                    }
                                    if (isAleardyDetected) break;
                                    candidateString += ontology + ";";
                                }
                                if (isAleardyDetected == false)
                                    a21Ontologies.Add(candidateString);
                            }
                        }
                    }
                }
            }

            if (result.NeutralLossResult != null && result.NeutralLossResult.Count > 0) {
                var maxIntensity = result.NeutralLossResult.Max(n => n.ProductIntensity);
                foreach (var ion in result.NeutralLossResult) {
                    if (ion.ProductIntensity >= maxIntensity * 0.05) {
                        if (ion.CandidateOntologies != null && ion.CandidateOntologies.Count > 0) {

                            var flg = false;
                            foreach (var ontology in ion.CandidateOntologies) {
                                if (chemOntology.FragmentOntologies.Contains(ontology)) {
                                    if (!a11Ontologies.Contains(ontology)) a11Ontologies.Add(ontology);
                                    flg = true;
                                    break;
                                }
                            }

                            if (flg == false) { //if false, we have to register the significant ion which does not be included in the fragment set.
                                var candidateString = string.Empty;
                                var isAleardyDetected = false;
                                foreach (var ontology in ion.CandidateOntologies) {
                                    foreach (var a21Ont in a21Ontologies) {
                                        if (a21Ont.Contains(ontology)) {
                                            isAleardyDetected = true;
                                            break;
                                        }
                                    }
                                    if (isAleardyDetected) break;
                                    candidateString += ontology + ";";
                                }
                                if (isAleardyDetected == false)
                                    a21Ontologies.Add(candidateString);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// currently, 'not significant peak' is determined as the peak having smaller than 5% relative abundance from the base peak
        /// </summary>
        public static List<string> FitNotsignificantOntologiesToFragmentSet(FormulaResult result,
            List<string> a11Ontologies, ChemicalOntology chemOntology, out List<string> a12Ontologies, out List<string> a22Ontologies) {

            a12Ontologies = new List<string>();
            a22Ontologies = new List<string>();
            if (result.ProductIonResult != null && result.ProductIonResult.Count > 0) {
                var maxIntensity = result.ProductIonResult.Max(n => n.Intensity);
                foreach (var ion in result.ProductIonResult) {
                    if (ion.Intensity < maxIntensity * 0.05) {
                        if (ion.CandidateOntologies != null && ion.CandidateOntologies.Count > 0) {

                            var flg = false;
                            foreach (var ontology in ion.CandidateOntologies) {
                                if (chemOntology.FragmentOntologies.Contains(ontology)) {
                                    if (!a12Ontologies.Contains(ontology) && !a11Ontologies.Contains(ontology))
                                        a12Ontologies.Add(ontology);
                                    flg = true;
                                    break;
                                }
                            }

                            if (flg == false) { //if false, we have to register the not significant ion which does not be included in the fragment set.
                                var candidateString = string.Empty;
                                var isAleardyDetected = false;
                                foreach (var ontology in ion.CandidateOntologies) {
                                    foreach (var a22Ont in a22Ontologies) {
                                        if (a22Ont.Contains(ontology)) {
                                            isAleardyDetected = true;
                                            break;
                                        }
                                    }
                                    if (isAleardyDetected) break;
                                    candidateString += ontology + ";";
                                }
                                if (isAleardyDetected == false)
                                    a22Ontologies.Add(candidateString);
                            }
                        }
                    }
                }
            }

            if (result.NeutralLossResult != null && result.NeutralLossResult.Count > 0) {
                var maxIntensity = result.NeutralLossResult.Max(n => n.ProductIntensity);
                foreach (var ion in result.NeutralLossResult) {
                    if (ion.ProductIntensity < maxIntensity * 0.05) {
                        if (ion.CandidateOntologies != null && ion.CandidateOntologies.Count > 0) {

                            var flg = false;
                            foreach (var ontology in ion.CandidateOntologies) {
                                if (chemOntology.FragmentOntologies.Contains(ontology)) {
                                    if (!a12Ontologies.Contains(ontology) && !a11Ontologies.Contains(ontology))
                                        a12Ontologies.Add(ontology);
                                    flg = true;
                                    break;
                                }
                            }

                            if (flg == false) { //if false, we have to register the not significant ion which does not be included in the fragment set.
                                var candidateString = string.Empty;
                                var isAleardyDetected = false;
                                foreach (var ontology in ion.CandidateOntologies) {
                                    foreach (var a22Ont in a22Ontologies) {
                                        if (a22Ont.Contains(ontology)) {
                                            isAleardyDetected = true;
                                            break;
                                        }
                                    }
                                    if (isAleardyDetected) break;
                                    candidateString += ontology + ";";
                                }
                                if (isAleardyDetected == false)
                                    a22Ontologies.Add(candidateString);
                            }
                        }
                    }
                }
            }

            return a12Ontologies;
        }

        public static double CalculateJaccardValue(FormulaResult result, ChemicalOntology chemOntology) {
            
            var expOntologies = new List<string>();
            var intersections = new List<string>();
            

            foreach (var ion in result.ProductIonResult) {
                if (ion.CandidateOntologies != null && ion.CandidateOntologies.Count > 0) {

                    var flg = false;
                    var repOntology = string.Empty;
                    foreach (var ontology in ion.CandidateOntologies) {
                        if (chemOntology.FragmentOntologies.Contains(ontology)) {
                            flg = true;
                            repOntology = ontology;
                            break;
                        }
                    }

                    if (flg) {
                        if (!intersections.Contains(repOntology))
                            intersections.Add(repOntology);
                        if (!expOntologies.Contains(repOntology))
                            expOntologies.Add(repOntology);
                    }
                    else {

                        var expFlg = false;
                        foreach (var ontology in ion.CandidateOntologies) {
                            if (expOntologies.Contains(ontology)) {
                                expFlg = true;
                                break;
                            }
                        }

                        if (!expFlg)
                            expOntologies.Add(ion.CandidateOntologies[0]);

                    }
                }
            }

            foreach (var loss in result.NeutralLossResult) {
                if (loss.CandidateOntologies != null && loss.CandidateOntologies.Count > 0) {

                    var flg = false;
                    var repOntology = string.Empty;
                    foreach (var ontology in loss.CandidateOntologies) {
                        if (chemOntology.FragmentOntologies.Contains(ontology)) {
                            flg = true;
                            repOntology = ontology;
                            break;
                        }
                    }

                    if (flg) {
                        if (!intersections.Contains(repOntology))
                            intersections.Add(repOntology);
                        if (!expOntologies.Contains(repOntology))
                            expOntologies.Add(repOntology);
                    }
                    else {

                        var expFlg = false;
                        foreach (var ontology in loss.CandidateOntologies) {
                            if (expOntologies.Contains(ontology)) {
                                expFlg = true;
                                break;
                            }
                        }

                        if (!expFlg)
                            expOntologies.Add(loss.CandidateOntologies[0]);
                    }
                }
            }

            var expSize = expOntologies.Count; //size of result
            var refSize = chemOntology.FragmentOntologies.Count; //size of chemOntology
            var intSize = intersections.Count; //intersection

            if (expSize + refSize - intSize < 0)
                return 0.0;
            else {

                var jaccard = (double)intSize / (double)(expSize + refSize - intSize);
                //Debug.WriteLine(jaccard + "\t" + intSize + "\t" + expSize + "\t" + refSize);

                return jaccard;
            }
        }

        private static bool isFormulaElementCorrect(Formula formula1, Formula formula2) {
            if ((formula1.Cnum > 0 && formula2.Cnum <= 0) || (formula1.Cnum <= 0 && formula2.Cnum > 0)) return false;
            if ((formula1.Nnum > 0 && formula2.Nnum <= 0) || (formula1.Nnum <= 0 && formula2.Nnum > 0)) return false;
            if ((formula1.Onum > 0 && formula2.Onum <= 0) || (formula1.Onum <= 0 && formula2.Onum > 0)) return false;
            if ((formula1.Snum > 0 && formula2.Snum <= 0) || (formula1.Snum <= 0 && formula2.Snum > 0)) return false;
            if ((formula1.Pnum > 0 && formula2.Pnum <= 0) || (formula1.Pnum <= 0 && formula2.Pnum > 0)) return false;
            if ((formula1.Fnum > 0 && formula2.Fnum <= 0) || (formula1.Fnum <= 0 && formula2.Fnum > 0)) return false;
            if ((formula1.Clnum > 0 && formula2.Clnum <= 0) || (formula1.Clnum <= 0 && formula2.Clnum > 0)) return false;
            if ((formula1.Brnum > 0 && formula2.Brnum <= 0) || (formula1.Brnum <= 0 && formula2.Brnum > 0)) return false;
            if ((formula1.Inum > 0 && formula2.Inum <= 0) || (formula1.Inum <= 0 && formula2.Inum > 0)) return false;

            return true;
        }
    }
}
