using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.StructureFinder.Property;
using Riken.Metabolomics.StructureFinder.Result;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Riken.Metabolomics.StructureFinder.Utility;

namespace Riken.Metabolomics.StructureFinder.Parser
{
    public sealed class FragmenterResultParcer
    {
        public static void FragmenterResultWriter(string outputFilePath, List<FragmenterResult> fragmenterResults, bool append)
        {
            if (fragmenterResults == null || fragmenterResults.Count == 0) return;

            using (StreamWriter sw = new StreamWriter(outputFilePath, append, Encoding.ASCII)) {
                foreach (var result in fragmenterResults) {
                    resultStreamWriter(sw, result);
                }
            }
        }

        public static void InsilicoFragmentResultWriter(string output, string inputSmiles, List<Fragment> fragments) {
            using (StreamWriter sw = new StreamWriter(output, true, Encoding.ASCII)) {
                sw.WriteLine("Input SMILES\t" + inputSmiles);
                sw.WriteLine("Tree depth\tSMILES\tExact mass");
                if (fragments != null && fragments.Count > 0)
                    fragments = fragments.OrderBy(n => n.TreeDepth).ThenByDescending(n => n.ExactMass).ToList();

                var smilesList = new List<string>();
                foreach (var frag in fragments) {
                    var fragContainer = MoleculeConverter.DictionaryToAtomContainer(frag.AtomDictionary, frag.BondDictionary);
                    var fragSmiles = MoleculeConverter.AtomContainerToSmiles(fragContainer);

                    if (smilesList.Contains(fragSmiles)) continue;

                    sw.WriteLine(frag.TreeDepth + "\t" + fragSmiles + "\t" + frag.ExactMass);
                    smilesList.Add(fragSmiles);
                }
                sw.WriteLine();
            }
        }

        private static void resultStreamWriter(StreamWriter sw, FragmenterResult result)
        {
            // meta data
            sw.WriteLine("NAME: " + result.Title);
            sw.WriteLine("ID: " + result.ID);
            sw.WriteLine("IsSpectrumSearch: " + result.IsSpectrumSearchResult);
            sw.WriteLine("INCHIKEY: " + result.Inchikey);
            sw.WriteLine("SMILES: " + result.Smiles);
            sw.WriteLine("RESOURCES: " + result.Resources);
            sw.WriteLine("SubstructureInChIKeys: " + result.SubstructureInChIKeys);
            sw.WriteLine("SubstructureOntologies: " + result.SubstructureOntologies);
            sw.WriteLine("Ontology: " + result.Ontology);
            sw.WriteLine("OntologyID: " + result.OntologyID);
            sw.WriteLine("RETENTIONTIME: " + result.RetentionTime);
            sw.WriteLine("RETENTIONINDEX: " + result.RetentionIndex);
            sw.WriteLine("CCS: " + result.Ccs);
            sw.WriteLine("TotalBondEnergy: " + result.BondEnergyOfUnfragmentedMolecule);
            sw.WriteLine("TotalScore: " + Math.Round(result.TotalScore, 4)); //0 <= score <= 1 
            sw.WriteLine("TotalHrRulesScore: " + Math.Round(result.TotalHrLikelihood, 4)); //0 <= score <= 1 
            sw.WriteLine("TotalBondCleavageScore: " + Math.Round(result.TotalBcLikelihood, 4)); //0 <= score <= 1 
            sw.WriteLine("TotalMassAccuracyScore: " + Math.Round(result.TotalMaLikelihood, 4)); //0 <= score <= 1 
            sw.WriteLine("TotalFragmentLinkageScore: " + Math.Round(result.TotalFlLikelihood, 4)); //0 <= score <= 1 
            sw.WriteLine("TotalBondDissociationEnergyScore: " + Math.Round(result.TotalBeLikelihood, 4)); //0 <= score <= 1 
            sw.WriteLine("DatabaseScore: " + Math.Round(result.DatabaseScore, 4)); //0 <= score <= 1 
            sw.WriteLine("SubstructureAssignmentScore: " + Math.Round(result.SubstructureAssignmentScore, 4)); //0 <= score <= 1 
            sw.WriteLine("RtSimilarityScore: " + Math.Round(result.RtSimilarityScore, 4)); //0 <= score <= 1 
            sw.WriteLine("RiSimilarityScore: " + Math.Round(result.RiSimilarityScore, 4)); //0 <= score <= 1 
            sw.WriteLine("CcsSimilarityScore: " + Math.Round(result.CcsSimilarityScore, 4)); //0 <= score <= 1 


            if (result.IsSpectrumSearchResult == true)
                writeSpectrumDbSearchResult(sw, result);
            else
                writeInSilicoFragmenterSearchResult(sw, result);

            sw.WriteLine();
        }

        private static void writeSpectrumDbSearchResult(StreamWriter sw, FragmenterResult result)
        {
            sw.Write("Num Peaks: ");
            sw.WriteLine(result.ReferenceSpectrum.Count);

            for (int i = 0; i < result.ReferenceSpectrum.Count; i++) {
                var mz = Math.Round(result.ReferenceSpectrum[i].Mz, 5);
                var intensity = Math.Round(result.ReferenceSpectrum[i].Intensity, 0);
                var comment = result.ReferenceSpectrum[i].Comment;

                if (comment == null || comment == string.Empty)
                    sw.WriteLine(mz + "\t" + intensity);
                else
                    sw.WriteLine(mz + "\t" + intensity + "\t" + "\"" + comment + "\"");
            }
        }

        private static void writeInSilicoFragmenterSearchResult(StreamWriter sw, FragmenterResult result)
        {
            // fragment
            var peakCount = 0;
            if (result.FragmentPics != null && result.FragmentPics.Count != 0)
                peakCount = result.FragmentPics.Count;

            sw.WriteLine("Num Fragment VS2 (M/Z Intensity MatchedExactMass SaturatedExactMass Formula RearrangedHydrogen " +
            "PPM MassDiff_mDa IsEvenElectronRule IsHrRule IsSolventAdductFragment AssignedAdductMass AdductString BondDissociationEnergy TreeDepth SMILES " +
            "TotalScore HrLikelihood BcLikelihood MaLikelihood FlLikelihood BeLikelihood): " +
            peakCount);

            if (peakCount == 0) return;

            foreach (var frag in result.FragmentPics) {

                var peak = frag.Peak;
                var matchedInfo = frag.MatchedFragmentInfo;

                sw.Write(peak.Mz + "\t");
                sw.Write(peak.Intensity + "\t");
                sw.Write(matchedInfo.MatchedMass + "\t");
                sw.Write(matchedInfo.SaturatedMass + "\t");
                sw.Write(matchedInfo.Formula + "\t");
                sw.Write(matchedInfo.RearrangedHydrogen + "\t");
                sw.Write(Math.Round(matchedInfo.Ppm, 2) + "\t");
                sw.Write(Math.Round(matchedInfo.Massdiff * 1000, 2) + "\t");
                sw.Write(matchedInfo.IsEeRule + "\t");
                sw.Write(matchedInfo.IsHrRule + "\t");
                sw.Write(matchedInfo.IsSolventAdductFragment + "\t");
                sw.Write(matchedInfo.AssignedAdductMass + "\t");
                sw.Write(matchedInfo.AssignedAdductString + "\t");
                sw.Write(matchedInfo.BdEnergy + "\t");
                sw.Write(matchedInfo.TreeDepth + "\t");
                sw.Write(matchedInfo.Smiles + "\t");
                sw.Write(matchedInfo.TotalLikelihood + "\t");
                sw.Write(matchedInfo.HrLikelihood + "\t");
                sw.Write(matchedInfo.BcLikelihood + "\t");
                sw.Write(matchedInfo.MaLikelihood + "\t");
                sw.Write(matchedInfo.FlLikelihood + "\t");
                sw.WriteLine(matchedInfo.BeLikelihood);
            }
        }

        public static List<FragmenterResult> FragmenterResultReader(string filePath)
        {
            var fragmenterResults = new List<FragmenterResult>();
            var fragments = new List<PeakFragmentPair>();
            var spectum = new List<Peak>();
            var result = new FragmenterResult();

            var isSpectrumSearch = false;

            var wkstr = string.Empty;
            var firstFlg = false;

            var id = string.Empty;
            var title = string.Empty;
            var inchikey = string.Empty;
            var smiles = string.Empty;
            var resources = string.Empty;
            var substructureShortInChIKeys = string.Empty;
            var substructureOntologies = string.Empty;
            var ontology = string.Empty;
            var ontologyID = string.Empty;

            var totalScore = -1.0;
            var totalHrLikelihood = -1.0;
            var totalBcLikelihood = -1.0;
            var totalMaLikelihood = -1.0;
            var totalFlLikelihood = -1.0;
            var totalBeLikelihood = -1.0;
            var substructureAssignmentScore = -1.0;
            var databaseScore = -1.0;
            var rtSimilarityScore = -1.0;
            var riSimilarityScore = -1.0;
            var ccsSimilarityScore = -1.0;
            var bondEnergy = -1.0;
            var retentionTime = -1.0;
            var retentionIndex = -1.0;
            var ccs = -1.0;

            using (var sr = new StreamReader(filePath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    wkstr = sr.ReadLine();
                    
                    double doubleValue = 0;
                    float floatValue = -1.0F;
                    int intValue = 0;

                    if (Regex.IsMatch(wkstr, "^NAME:", RegexOptions.IgnoreCase)) {
                        if (!firstFlg) {
                            firstFlg = true;
                            title = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                        }
                        else {

                            result = new FragmenterResult(isSpectrumSearch, fragments, spectum,
                                title, id, bondEnergy, inchikey, smiles, substructureShortInChIKeys, substructureOntologies,
                                resources, ontology, ontologyID,
                                retentionTime, retentionIndex, ccs,
                                totalScore, totalHrLikelihood, totalBcLikelihood, totalMaLikelihood, totalFlLikelihood, totalBeLikelihood,
                                substructureAssignmentScore, databaseScore, rtSimilarityScore, riSimilarityScore, ccsSimilarityScore);

                            fragmenterResults.Add(result);

                            title = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                            fragments = new List<PeakFragmentPair>();
                            spectum = new List<Peak>();
                        }
                    }
                    else if (Regex.IsMatch(wkstr, "^ID:", RegexOptions.IgnoreCase)) {
                        id = wkstr.Split(':')[1].Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "IsSpectrumSearch:", RegexOptions.IgnoreCase)) {
                        var isSpectrumString = wkstr.Split(':')[1].Trim();
                        if (isSpectrumString.Contains("T")) isSpectrumSearch = true;
                        else isSpectrumSearch = false;
                    }
                    else if (Regex.IsMatch(wkstr, "INCHIKEY:", RegexOptions.IgnoreCase)) {
                        inchikey = wkstr.Split(':')[1].Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "SMILES:", RegexOptions.IgnoreCase)) {
                        smiles = wkstr.Split(':')[1].Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "Ontology:", RegexOptions.IgnoreCase)) {
                        ontology = wkstr.Split(':')[1].Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "OntologyID:", RegexOptions.IgnoreCase)) {
                        ontologyID = wkstr.Split(':')[1].Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "RESOURCES:", RegexOptions.IgnoreCase)) {
                        resources = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "SubstructureInChIKeys:", RegexOptions.IgnoreCase)) {
                        substructureShortInChIKeys = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "SubstructureOntologies:", RegexOptions.IgnoreCase)) {
                        substructureOntologies = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "RETENTIONTIME:", RegexOptions.IgnoreCase)) {
                        if (float.TryParse(wkstr.Split(':')[1].Trim(), out floatValue)) retentionTime = floatValue; else retentionTime = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "RETENTIONINDEX:", RegexOptions.IgnoreCase)) {
                        if (float.TryParse(wkstr.Split(':')[1].Trim(), out floatValue)) retentionIndex = floatValue; else retentionIndex = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "CCS:", RegexOptions.IgnoreCase)) {
                        if (float.TryParse(wkstr.Split(':')[1].Trim(), out floatValue)) ccs = floatValue; else ccs = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "TotalBondEnergy:", RegexOptions.IgnoreCase)) {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) bondEnergy = doubleValue; else bondEnergy = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "TotalScore:", RegexOptions.IgnoreCase)) {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) totalScore = doubleValue; else totalScore = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "TotalHrRulesScore:", RegexOptions.IgnoreCase)) {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) totalHrLikelihood = doubleValue; else totalHrLikelihood = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "TotalBondCleavageScore:", RegexOptions.IgnoreCase)) {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) totalBcLikelihood = doubleValue; else totalBcLikelihood = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "TotalMassAccuracyScore:", RegexOptions.IgnoreCase)) {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) totalMaLikelihood = doubleValue; else totalMaLikelihood = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "TotalFragmentLinkageScore:", RegexOptions.IgnoreCase)) {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) totalFlLikelihood = doubleValue; else totalFlLikelihood = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "TotalBondDissociationEnergyScore:", RegexOptions.IgnoreCase)) {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) totalBeLikelihood = doubleValue; else totalBeLikelihood = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "DatabaseScore:", RegexOptions.IgnoreCase)) {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) databaseScore = doubleValue; else databaseScore = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "SubstructureAssignmentScore:", RegexOptions.IgnoreCase)) {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) substructureAssignmentScore = doubleValue; else substructureAssignmentScore = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "RtSimilarityScore:", RegexOptions.IgnoreCase)) {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) rtSimilarityScore = doubleValue; else rtSimilarityScore = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "RiSimilarityScore:", RegexOptions.IgnoreCase)) {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) riSimilarityScore = doubleValue; else riSimilarityScore = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "CcsSimilarityScore:", RegexOptions.IgnoreCase)) {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) ccsSimilarityScore = doubleValue; else ccsSimilarityScore = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "Num Fragment \\(", RegexOptions.IgnoreCase)) {
                        intValue = 0;
                        
                        isSpectrumSearch = false;
                        spectum = null;
                        fragments = new List<PeakFragmentPair>();
                        
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue)) {
                            if (intValue == 0) continue;
                            for (int i = 0; i < intValue; i++) {
                                if (!readFragmenterArray(fragments, sr.ReadLine())) { return null; }
                            }
                        }
                    }
                    else if (Regex.IsMatch(wkstr, "Num Fragment VS2 \\(", RegexOptions.IgnoreCase)) {
                        intValue = 0;

                        isSpectrumSearch = false;
                        isSpectrumSearch = false;
                        spectum = null;
                        fragments = new List<PeakFragmentPair>();

                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue)) {
                            if (intValue == 0) continue;
                            for (int i = 0; i < intValue; i++) {
                                if (!readFragmenterArrayVS2(fragments, sr.ReadLine())) { return null; }
                            }
                        }
                    }
                    else if (Regex.IsMatch(wkstr, "Num Peaks:.*", RegexOptions.IgnoreCase)) {
                        
                        isSpectrumSearch = true;
                        spectum = new List<Peak>();
                        fragments = null;

                        var peakNum = 0;
                        spectum = MspFileParcer.ReadPeaks(sr, wkstr, out peakNum);
                        isSpectrumSearch = true;
                    }
                }
                //reminder
                result = new FragmenterResult(isSpectrumSearch, fragments, spectum,
                    title, id, bondEnergy, inchikey, smiles, substructureShortInChIKeys, substructureOntologies,
                    resources, ontology, ontologyID,
                    retentionTime, retentionIndex, ccs,
                    totalScore, totalHrLikelihood, totalBcLikelihood, totalMaLikelihood, totalFlLikelihood, totalBeLikelihood,
                    substructureAssignmentScore, databaseScore, rtSimilarityScore, riSimilarityScore, ccsSimilarityScore);

                fragmenterResults.Add(result);
            }
            return fragmenterResults.OrderByDescending(n => n.TotalScore).ToList();
        }

        /// <summary>
        /// this method is for faster reading of structure results.
        /// It parses only metadata
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<FragmenterResult> FragmenterResultFastReader(string filePath) {
            var fragmenterResults = new List<FragmenterResult>();
            var fragments = new List<PeakFragmentPair>();
            var spectum = new List<Peak>();
            var result = new FragmenterResult();

            var isSpectrumSearch = false;

            var wkstr = string.Empty;
            var firstFlg = false;

            var id = string.Empty;
            var title = string.Empty;
            var inchikey = string.Empty;
            var smiles = string.Empty;
            var resources = string.Empty;
            var substructureShortInChIKeys = string.Empty;
            var substructureOntologies = string.Empty;
            var ontology = string.Empty;
            var ontologyID = string.Empty;

            var totalScore = -1.0;
            var totalHrLikelihood = -1.0;
            var totalBcLikelihood = -1.0;
            var totalMaLikelihood = -1.0;
            var totalFlLikelihood = -1.0;
            var totalBeLikelihood = -1.0;
            var substructureAssignmentScore = -1.0;
            var databaseScore = -1.0;
            var rtSimilarityScore = -1.0;
            var riSimilarityScore = -1.0;
            var ccsSimilarityScore = -1.0;
            var bondEnergy = -1.0;
            var retentionTime = -1.0;
            var retentionIndex = -1.0;
            var ccs = -1.0;

            using (var sr = new StreamReader(filePath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    wkstr = sr.ReadLine();

                    double doubleValue = 0;

                    if (Regex.IsMatch(wkstr, "^NAME:", RegexOptions.IgnoreCase)) {
                        if (!firstFlg) {
                            firstFlg = true;
                            title = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                        }
                        else {

                            result = new FragmenterResult(isSpectrumSearch, fragments, spectum,
                                title, id, bondEnergy, inchikey, smiles, substructureShortInChIKeys, substructureOntologies,
                                resources, ontology, ontologyID,
                                retentionTime, retentionIndex, ccs,
                                totalScore, totalHrLikelihood, totalBcLikelihood, totalMaLikelihood, totalFlLikelihood, totalBeLikelihood,
                                substructureAssignmentScore, databaseScore, rtSimilarityScore, riSimilarityScore, ccsSimilarityScore);

                            fragmenterResults.Add(result);

                            title = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                            fragments = new List<PeakFragmentPair>();
                            spectum = new List<Peak>();
                        }
                    }
                    else if (Regex.IsMatch(wkstr, "^ID:", RegexOptions.IgnoreCase)) {
                        id = wkstr.Split(':')[1].Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "IsSpectrumSearch:", RegexOptions.IgnoreCase)) {
                        var isSpectrumString = wkstr.Split(':')[1].Trim();
                        if (isSpectrumString.Contains("T")) isSpectrumSearch = true;
                        else isSpectrumSearch = false;
                    }
                    else if (Regex.IsMatch(wkstr, "INCHIKEY:", RegexOptions.IgnoreCase)) {
                        inchikey = wkstr.Split(':')[1].Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "SMILES:", RegexOptions.IgnoreCase)) {
                        smiles = wkstr.Split(':')[1].Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "Ontology:", RegexOptions.IgnoreCase)) {
                        ontology = wkstr.Split(':')[1].Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "TotalScore:", RegexOptions.IgnoreCase)) {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) totalScore = doubleValue; else totalScore = -1;
                    }
                }
                //reminder
                result = new FragmenterResult(isSpectrumSearch, fragments, spectum,
                    title, id, bondEnergy, inchikey, smiles, substructureShortInChIKeys, substructureOntologies,
                    resources, ontology, ontologyID,
                    retentionTime, retentionIndex, ccs,
                    totalScore, totalHrLikelihood, totalBcLikelihood, totalMaLikelihood, totalFlLikelihood, totalBeLikelihood,
                    substructureAssignmentScore, databaseScore, rtSimilarityScore, riSimilarityScore, ccsSimilarityScore);

                fragmenterResults.Add(result);
            }
            return fragmenterResults.OrderByDescending(n => n.TotalScore).ToList();
        }

        private static bool readFragmenterArrayVS2(List<PeakFragmentPair> fragments, string line)
        {
            var array = line.Split('\t');
            if (array.Length < 21) { errorFragmnetArray(); return false; }

            var peak = new Peak();
            var matchedInfo = new MatchedFragmentInfo();

            double doubleValue;
            int intValue;

            if (double.TryParse(array[0], out doubleValue)) peak.Mz = doubleValue; else { errorValue(); return false; }
            if (double.TryParse(array[1], out doubleValue)) peak.Intensity = doubleValue; else { errorValue(); return false; }
            if (double.TryParse(array[2], out doubleValue)) matchedInfo.MatchedMass = doubleValue; else { errorValue(); return false; }
            if (double.TryParse(array[3], out doubleValue)) matchedInfo.SaturatedMass = doubleValue; else { errorValue(); return false; }
            matchedInfo.Formula = array[4];
            if (int.TryParse(array[5], out intValue)) matchedInfo.RearrangedHydrogen = intValue; else { errorValue(); return false; }
            if (double.TryParse(array[6], out doubleValue)) matchedInfo.Ppm = doubleValue; else { errorValue(); return false; }
            if (double.TryParse(array[7], out doubleValue)) matchedInfo.Massdiff = doubleValue * 0.001; else { errorValue(); return false; }
            if (string.Compare(array[8], "True", true) == 0) matchedInfo.IsEeRule = true; else matchedInfo.IsEeRule = false;
            if (string.Compare(array[9], "True", true) == 0) matchedInfo.IsHrRule = true; else matchedInfo.IsHrRule = false;
            if (string.Compare(array[10], "True", true) == 0) matchedInfo.IsSolventAdductFragment = true; else matchedInfo.IsSolventAdductFragment = false;
            if (double.TryParse(array[11], out doubleValue)) matchedInfo.AssignedAdductMass = doubleValue; else { errorValue(); return false; }
            matchedInfo.AssignedAdductString = array[12];
            if (double.TryParse(array[13], out doubleValue)) matchedInfo.BdEnergy = doubleValue; else { errorValue(); return false; }
            if (int.TryParse(array[14], out intValue)) matchedInfo.TreeDepth = intValue; else { errorValue(); return false; }
            matchedInfo.Smiles = array[15];
            if (double.TryParse(array[16], out doubleValue)) matchedInfo.TotalLikelihood = doubleValue; else { errorValue(); return false; }
            if (double.TryParse(array[17], out doubleValue)) matchedInfo.HrLikelihood = doubleValue; else { errorValue(); return false; }
            if (double.TryParse(array[18], out doubleValue)) matchedInfo.BcLikelihood = doubleValue; else { errorValue(); return false; }
            if (double.TryParse(array[19], out doubleValue)) matchedInfo.MaLikelihood = doubleValue; else { errorValue(); return false; }
            if (double.TryParse(array[20], out doubleValue)) matchedInfo.FlLikelihood = doubleValue; else { errorValue(); return false; }
            if (double.TryParse(array[21], out doubleValue)) matchedInfo.BeLikelihood = doubleValue; else { errorValue(); return false; }

            var peakFragPair = new PeakFragmentPair() { Peak = peak, MatchedFragmentInfo = matchedInfo };
            fragments.Add(peakFragPair);

            return true;
        }

        private static bool readFragmenterArray(List<PeakFragmentPair> fragments, string line)
        {
            string[] array = line.Split('\t');
            if (array.Length < 13) { errorFragmnetArray(); return false; }

            var peak = new Peak();
            var matchedInfo = new MatchedFragmentInfo();

            double doubleValue;
            int intValue;

            if (double.TryParse(array[0], out doubleValue)) peak.Mz = doubleValue; else { errorValue(); return false; }
            if (double.TryParse(array[1], out doubleValue)) peak.Intensity = doubleValue; else { errorValue(); return false; }
            if (double.TryParse(array[2], out doubleValue)) matchedInfo.MatchedMass = doubleValue; else { errorValue(); return false; }
            matchedInfo.Formula = array[3];
            if (double.TryParse(array[4], out doubleValue)) matchedInfo.Ppm = doubleValue; else { errorValue(); return false; }
            if (double.TryParse(array[5], out doubleValue)) matchedInfo.Massdiff = doubleValue; else { errorValue(); return false; }
            if (double.TryParse(array[6], out doubleValue)) matchedInfo.TotalLikelihood = doubleValue; else { errorValue(); return false; }
            //if (double.TryParse(array[7], out doubleValue)) hydrogenPenalty = doubleValue; else { errorValue(); return false; }
            if (double.TryParse(array[8], out doubleValue)) matchedInfo.BdEnergy = doubleValue; else { errorValue(); return false; }
            if (int.TryParse(array[9], out intValue)) matchedInfo.TreeDepth = intValue; else { errorValue(); return false; }
            //if (int.TryParse(array[10], out intValue)) parentID = intValue; else { errorValue(); return false; }
            //if (int.TryParse(array[11], out intValue)) fragmentID = intValue; else { errorValue(); return false; }
            matchedInfo.Smiles = array[12];

            if (array.Length >= 14) {
                matchedInfo.AssignedAdductString = array[13];
            }

            if (array.Length >= 15) {
                if (string.Compare(array[14], "True", true) == 0) matchedInfo.IsHrRule = true; else matchedInfo.IsHrRule = false;
            }

            //if (array.Length >= 16) {
            //    if (string.Compare(array[15], "True", true) == 0) ruleOfFragmentationTree = true; else ruleOfFragmentationTree = false;
            //}

            //var cleavagedElementString = string.Empty;
            //if (array.Length >= 17) {
            //    cleavagedElementString = array[16];
            //}

            //var suggestedRuleCombination = string.Empty;
            //if (array.Length >= 18) {
            //    suggestedRuleCombination = array[17];
            //}

            var peakFragPair = new PeakFragmentPair() { Peak = peak, MatchedFragmentInfo = matchedInfo };
            fragments.Add(peakFragPair);

            return true;
        }

        private static void errorValue()
        {
            Console.WriteLine("Bad format: Empty value or non-figure value exist.");
            //MessageBox.Show("Bad format: Empty value or non-figure value exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static void errorFragmnetArray()
        {
            Console.WriteLine("Bad format: Fragment array.");
            //MessageBox.Show("Bad format: Fragment array.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

    }
}
