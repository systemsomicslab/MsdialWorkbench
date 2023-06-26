using Msdial.Lcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.Lipidomics;
using Riken.Metabolomics.Lipidomics.Searcher;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.Dataprocess.Scoring
{
    public sealed class LcmsScoring
    {
        private LcmsScoring() { }

        /// <summary>
        /// This method returns the similarity score between theoretical isotopic ratios and experimental isotopic patterns in MS1 axis.
        /// This method will utilize up to [M+4] for their calculations.
        /// </summary>
        /// <param name="ms1Spectra">
        /// Add the MS1 spectrum with respect to the focused data point.
        /// </param>
        /// <param name="libIsotopeRatioList">
        /// Add the theoretical isotopic abundances. The theoretical patterns are supposed to be calculated in MSP parcer.
        /// </param>
        /// <param name="accurateMass">
        /// Add the experimental precursor mass.
        /// </param>
        /// <param name="ms1LibrarySearchTol">
        /// Add the torelance to merge the spectrum of experimental MS1.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetIsotopeRatioSimilarity(ObservableCollection<double[]> ms1Spectra, List<float> libIsotopeRatioList, float accurateMass, float ms1LibrarySearchTol)
        {
            if (ms1Spectra.Count == 0) return -1;
            int index = DataAccessLcUtility.GetMs1StartIndex(accurateMass, ms1LibrarySearchTol, ms1Spectra);
            List<float> actIsotopeRatioList = new List<float>();
            float c13_c12Diff = 1.003355F;
            double sum0 = 0, sum1 = 0, sum2 = 0, sum3 = 0, sum4 = 0;
            double focusedMass0 = accurateMass, focusedMass1 = accurateMass + c13_c12Diff, focusedMass2 = accurateMass + 2 * c13_c12Diff
                , focusedMass3 = accurateMass + 3 * c13_c12Diff, focusedMass4 = accurateMass + 4 * c13_c12Diff;
            while (index < ms1Spectra.Count)
            {
                if (focusedMass0 - ms1LibrarySearchTol <= ms1Spectra[index][0] && ms1Spectra[index][0] <= focusedMass0 + ms1LibrarySearchTol) sum0 += ms1Spectra[index][1];
                if (focusedMass1 - ms1LibrarySearchTol <= ms1Spectra[index][0] && ms1Spectra[index][0] <= focusedMass1 + ms1LibrarySearchTol) sum1 += ms1Spectra[index][1];
                if (focusedMass2 - ms1LibrarySearchTol <= ms1Spectra[index][0] && ms1Spectra[index][0] <= focusedMass2 + ms1LibrarySearchTol) sum2 += ms1Spectra[index][1];
                if (focusedMass3 - ms1LibrarySearchTol <= ms1Spectra[index][0] && ms1Spectra[index][0] <= focusedMass3 + ms1LibrarySearchTol) sum3 += ms1Spectra[index][1];
                if (focusedMass4 - ms1LibrarySearchTol <= ms1Spectra[index][0] && ms1Spectra[index][0] <= focusedMass4 + ms1LibrarySearchTol) sum4 += ms1Spectra[index][1];
                if (ms1Spectra[index][0] > focusedMass4 + ms1LibrarySearchTol) break;

                index++;
            }

            actIsotopeRatioList.Add((float)sum0);
            actIsotopeRatioList.Add((float)sum1);
            actIsotopeRatioList.Add((float)sum2);
            actIsotopeRatioList.Add((float)sum3);
            actIsotopeRatioList.Add((float)sum4);

            double similarity = 0;
            double ratio1 = 0, ratio2 = 0;
            if (actIsotopeRatioList[0] <= 0 || libIsotopeRatioList[0] <= 0) return 0;

            for (int i = 1; i < 5; i++)
            {
                if (libIsotopeRatioList.Count - 1 < i + 1) break;

                ratio1 = actIsotopeRatioList[i] / actIsotopeRatioList[0];
                ratio2 = libIsotopeRatioList[i] / libIsotopeRatioList[0];

                if (ratio1 <= 1 && ratio2 <= 1) similarity += Math.Abs(ratio1 - ratio2);
                else
                {
                    if (ratio1 > ratio2)
                    {
                        similarity += 1 - ratio2 / ratio1;
                    }
                    else if (ratio2 > ratio1)
                    {
                        similarity += 1 - ratio1 / ratio2;
                    }
                }
            }
            return 1 - similarity;
        }

        /// <summary>
        /// This method returns the presence similarity (% of matched fragments) between the experimental MS/MS spectrum and the standard MS/MS spectrum.
        /// So, this program will calculate how many fragments of library spectrum are found in the experimental spectrum and will return the %.
        /// double[] [0]m/z[1]intensity
        /// 
        /// </summary>
        /// <param name="measuredSpectra">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="librarySpectra">
        /// Add the theoretical MS/MS spectrum. The theoretical MS/MS spectrum is supposed to be retreived in MSP parcer.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetPresenceSimilarity(ObservableCollection<double[]> measuredSpectra, List<MzIntensityCommentBean> librarySpectra, float bin, 
            float massBegin, float massEnd)
        {
            if (librarySpectra.Count == 0) return 0;

            double sumM = 0, sumL = 0;
            double minMz = librarySpectra[0].Mz;
            double maxMz = librarySpectra[librarySpectra.Count - 1].Mz;

            if (massBegin > minMz) minMz = massBegin;
            if (maxMz > massEnd) maxMz = massEnd;

            double focusedMz = minMz;
            double maxLibIntensity = librarySpectra.Max(n => n.Intensity);
            int remaindIndexM = 0, remaindIndexL = 0;
            int counter = 0;
            int libCounter = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            while (focusedMz <= maxMz)
            {
                sumL = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++) {
                    if (librarySpectra[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= librarySpectra[i].Mz && librarySpectra[i].Mz < focusedMz + bin)
                        sumL += librarySpectra[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                if (sumL >= 0.01 * maxLibIntensity) {
                    libCounter++;
                }

                sumM = 0;
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++) {
                    if (measuredSpectra[i][0] < focusedMz - bin) continue;
                    else if (focusedMz - bin <= measuredSpectra[i][0] && measuredSpectra[i][0] < focusedMz + bin)
                        sumM += measuredSpectra[i][1];
                    else { remaindIndexM = i; break; }
                }

                if (sumM > 0 && sumL >= 0.01 * maxLibIntensity) {
                    counter++;
                }

                if (focusedMz + bin > librarySpectra[librarySpectra.Count - 1].Mz) break;
                focusedMz = librarySpectra[remaindIndexL].Mz;
            }

            if (libCounter == 0) return 0;
            else
                return (double)counter / (double)libCounter;
        }

        /// <summary>
        /// This method returns the presence similarity (% of matched fragments) between the experimental MS/MS spectrum and the standard MS/MS spectrum.
        /// So, this program will calculate how many fragments of library spectrum are found in the experimental spectrum and will return the %.
        /// double[] [0]m/z[1]intensity
        /// 
        /// </summary>
        /// <param name="expSpec">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="refSpec">
        /// Add the theoretical MS/MS spectrum. The theoretical MS/MS spectrum is supposed to be retreived in MSP parcer.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetFragmentPresenceScore(ObservableCollection<double[]> expSpec, MspFormatCompoundInformationBean query, 
            float bin, float massBegin, float massEnd, IonMode ionMode, TargetOmics omics) {

            var refSpec = query.MzIntensityCommentBeanList;
            if (refSpec.Count == 0) return 0;
            if (omics == TargetOmics.Metablomics) return GetPresenceSimilarity(expSpec, refSpec, bin, massBegin, massEnd);

            // in lipidomics project, currently, the well-known lipid classes now including
            // PC, PE, PI, PS, PG, BMP, SM, TAG are now evaluated.
            // if the lipid class diagnostic fragment (like m/z 184 in PC and SM in ESI(+)) is observed, 
            // the bonus 0.5 is added to the normal presence score

            var defaultPresenceScore = GetPresenceSimilarity(expSpec, refSpec, bin, massBegin, massEnd);
            var compClass = query.CompoundClass;
            var adductname = query.AdductIonBean.AdductIonName;
            var comment = query.Comment;
            //if (compClass == "TG_EST") {
            //    Console.WriteLine();
            //}
            //if (compClass == "SM" && comment == "SPLASH") {
            //    Console.WriteLine();
            //}
            if (comment != "SPLASH" && compClass != "Unknown" && compClass != "Others")
            {
                var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(query);
                if (molecule == null || molecule.Adduct == null) return defaultPresenceScore;
                if (molecule.LipidClass == LbmClass.EtherPE && refSpec.Count == 3 && query.IonMode == IonMode.Positive) return defaultPresenceScore;
                //if (query.Name == "PC 36:4; PC 16:0-20:4; [M+CH3COO]-") {
                //    Console.WriteLine();
                //}
                var result = GetLipidMoleculeAnnotationResult(expSpec, molecule, bin);
                if (result != null) {
                    if (result.AnnotationLevel == 1) {
                        if (compClass == "SM" && (molecule.LipidName.Contains("3O") || molecule.LipidName.Contains("O3")))
                            return defaultPresenceScore + 1.0; // add bonus
                        else
                            return defaultPresenceScore + 0.5; // add bonus
                    }
                    else if (result.AnnotationLevel == 2)
                        return defaultPresenceScore + 1.0; // add bonus
                    else
                        return defaultPresenceScore;
                }
                else {
                    return defaultPresenceScore;
                }
            }
            else { // currently default value is retured for other lipids
                return defaultPresenceScore;
            }
        }

        public static string GetRefinedLipidAnnotationLevel(ObservableCollection<double[]> expSpec, MspFormatCompoundInformationBean query, float bin, 
            out bool isLipidClassMatched, out bool isLipidChainMatched, out bool isLipidPositionMatched, out bool isOthers) {

            isLipidClassMatched = false;
            isLipidChainMatched = false;
            isLipidPositionMatched = false;
            isOthers = false;

            var refSpec = query.MzIntensityCommentBeanList;
            if (refSpec.Count == 0) return string.Empty;

            // in lipidomics project, currently, the well-known lipid classes now including
            // PC, PE, PI, PS, PG, SM, TAG are now evaluated.
            // if the lipid class diagnostic fragment (like m/z 184 in PC and SM in ESI(+)) is observed, 
            // the bonus 0.5 is added to the normal presence score

            var compClass = query.CompoundClass;
            var adductname = query.AdductIonBean.AdductIonName;
            var comment = query.Comment;
            //if (compClass == "SM") {
            //    Console.WriteLine();
            //}
            //if (compClass == "SM" && comment == "SPLASH") {
            //    Console.WriteLine();
            //}
            if (comment != "SPLASH" && compClass != "Unknown" && compClass != "Others") {

                if (compClass == "Cholesterol" || compClass == "CholesterolSulfate" || 
                    compClass == "Undefined" || compClass == "BileAcid" ||
                    compClass == "Ac2PIM1" || compClass == "Ac2PIM2" || compClass == "Ac3PIM2" || compClass == "Ac4PIM2" || 
                    compClass == "LipidA") {
                    isOthers = true;
                    return query.Name; // currently default value is retured for these lipids
                }

                var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(query);
                if (molecule == null || molecule.Adduct == null) {
                    isOthers = true;
                    return query.Name;
                }

                var result = GetLipidMoleculeAnnotationResult(expSpec, molecule, bin);
                if (result != null) {
                    var refinedName = string.Empty;
                    if (result.AnnotationLevel == 1) {
                        refinedName = result.SublevelLipidName;
                        isLipidClassMatched = true;
                        isLipidChainMatched = false;
                        isLipidPositionMatched = false;
                    }
                    else if (result.AnnotationLevel == 2) {
                        isLipidClassMatched = true;
                        isLipidChainMatched = true;
                        isLipidPositionMatched = false;
                        if (result.SublevelLipidName == result.LipidName) {
                            refinedName = result.SublevelLipidName;
                        }
                        else {
                            refinedName = result.SublevelLipidName + "|" + result.LipidName;
                        }
                    }
                    else
                        return string.Empty;
                    //refinedName += "; " + molecule.Adduct.AdductIonName;
                   
                    //if (refinedName == "HexCer-AP 70:5; [M+H]+") {
                    //    Console.WriteLine();
                    //}
                    //if (query.IonMode == IonMode.Negative && query.CompoundClass == "PG") {
                    //}
                    return refinedName;
                }
                else {
                    return string.Empty;
                }
            }
            else { // currently default value is retured for other lipids
                isOthers = true;
                return query.Name;
            }
        }

        public static LipidMolecule GetLipidMoleculeAnnotationResult(ObservableCollection<double[]> spectrum, 
            LipidMolecule molecule, double ms2tol) {

            var lipidclass = molecule.LipidClass;
            var refMz = molecule.Mz;
            var adduct = molecule.Adduct;

            var totalCarbon = molecule.TotalCarbonCount;
            var totalDbBond = molecule.TotalDoubleBondCount;
            var totalOxidized = molecule.TotalOxidizedCount;

            var sn1Carbon = molecule.Sn1CarbonCount;
            var sn1DbBond = molecule.Sn1DoubleBondCount;
            var sn1Oxidized = molecule.Sn1Oxidizedount;

            var sn2Oxidized = molecule.Sn2Oxidizedount;

           // Console.WriteLine(molecule.LipidName);
            var lipidheader = LipidomicsConverter.GetLipidHeaderString(molecule.LipidName);
           // Console.WriteLine(lipidheader + "\t" + lipidclass.ToString());

            switch (lipidclass) {
                case LbmClass.PC:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylcholine(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PE:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylethanolamine(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PS:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylserine(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PG:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylglycerol(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.BMP:
                    return LipidMsmsCharacterization.JudgeIfBismonoacylglycerophosphate(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PI:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylinositol(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.SM:
                    if (molecule.TotalChainString.Contains("3O"))
                    {
                        return LipidMsmsCharacterization.JudgeIfSphingomyelinPhyto(spectrum, ms2tol, refMz,
                       totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }
                    else
                    {
                        return LipidMsmsCharacterization.JudgeIfSphingomyelin(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }
                case LbmClass.LNAPE:
                    return LipidMsmsCharacterization.JudgeIfNacylphosphatidylethanolamine(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.LNAPS:
                    return LipidMsmsCharacterization.JudgeIfNacylphosphatidylserine(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.CE:
                    return LipidMsmsCharacterization.JudgeIfCholesterylEster(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                case LbmClass.CAR:
                    return LipidMsmsCharacterization.JudgeIfAcylcarnitine(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);

                case LbmClass.DG:
                    return LipidMsmsCharacterization.JudgeIfDag(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.MG:
                    return LipidMsmsCharacterization.JudgeIfMag(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.MGDG:
                    return LipidMsmsCharacterization.JudgeIfMgdg(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.DGDG:
                    return LipidMsmsCharacterization.JudgeIfDgdg(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.PMeOH:
                    return LipidMsmsCharacterization.JudgeIfPmeoh(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.PEtOH:
                    return LipidMsmsCharacterization.JudgeIfPetoh(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.PBtOH:
                    return LipidMsmsCharacterization.JudgeIfPbtoh(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LPC:
                    return LipidMsmsCharacterization.JudgeIfLysopc(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LPE:
                    return LipidMsmsCharacterization.JudgeIfLysope(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.PA:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidicacid(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LPA:
                    return LipidMsmsCharacterization.JudgeIfLysopa(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LPG:
                    return LipidMsmsCharacterization.JudgeIfLysopg(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LPI:
                    return LipidMsmsCharacterization.JudgeIfLysopi(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LPS:
                    return LipidMsmsCharacterization.JudgeIfLysops(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.EtherPC:
                    return LipidMsmsCharacterization.JudgeIfEtherpc(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.EtherPE:
                    return LipidMsmsCharacterization.JudgeIfEtherpe(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.EtherLPC:
                    return LipidMsmsCharacterization.JudgeIfEtherlysopc(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.EtherLPE:
                    return LipidMsmsCharacterization.JudgeIfEtherlysope(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.OxPC:
                    return LipidMsmsCharacterization.JudgeIfOxpc(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);
                    
                case LbmClass.OxPE:
                    return LipidMsmsCharacterization.JudgeIfOxpe(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);
                    
                case LbmClass.OxPG:
                    return LipidMsmsCharacterization.JudgeIfOxpg(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);
                    
                case LbmClass.OxPI:
                    return LipidMsmsCharacterization.JudgeIfOxpi(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);
                    
                case LbmClass.OxPS:
                    return LipidMsmsCharacterization.JudgeIfOxps(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);
                    
                case LbmClass.EtherMGDG:
                    return LipidMsmsCharacterization.JudgeIfEthermgdg(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.EtherDGDG:
                    return LipidMsmsCharacterization.JudgeIfEtherdgdg(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.DGTS:
                    return LipidMsmsCharacterization.JudgeIfDgts(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.LDGTS:
                    return LipidMsmsCharacterization.JudgeIfLdgts(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.DGCC:
                    return LipidMsmsCharacterization.JudgeIfDgcc(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LDGCC:
                    return LipidMsmsCharacterization.JudgeIfLdgcc(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DGGA:
                    return LipidMsmsCharacterization.JudgeIfGlcadg(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.SQDG:
                    return LipidMsmsCharacterization.JudgeIfSqdg(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.DLCL:
                    return LipidMsmsCharacterization.JudgeIfDilysocardiolipin(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.FA:
                    return LipidMsmsCharacterization.JudgeIfFattyacid(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.OxFA:
                    return LipidMsmsCharacterization.JudgeIfOxfattyacid(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized);

                case LbmClass.FAHFA:
                    return LipidMsmsCharacterization.JudgeIfFahfa(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DMEDFAHFA:
                    return LipidMsmsCharacterization.JudgeIfFahfaDMED(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DMEDFA:
                    return LipidMsmsCharacterization.JudgeIfDmedFattyacid(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.DMEDOxFA:
                    return LipidMsmsCharacterization.JudgeIfDmedOxfattyacid(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized);
                    
                case LbmClass.EtherOxPC:
                    return LipidMsmsCharacterization.JudgeIfEtheroxpc(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);
                    
                case LbmClass.EtherOxPE:
                    return LipidMsmsCharacterization.JudgeIfEtheroxpe(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);
                    
                case LbmClass.Cer_NS:
                    return LipidMsmsCharacterization.JudgeIfCeramidens(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.Cer_NDS:
                    return LipidMsmsCharacterization.JudgeIfCeramidends(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.HexCer_NS:
                    return LipidMsmsCharacterization.JudgeIfHexceramidens(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.HexCer_NDS:
                    return LipidMsmsCharacterization.JudgeIfHexceramidends(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.Hex2Cer:
                    return LipidMsmsCharacterization.JudgeIfHexhexceramidens(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.Hex3Cer:
                    return LipidMsmsCharacterization.JudgeIfHexhexhexceramidens(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.Cer_AP:
                    return LipidMsmsCharacterization.JudgeIfCeramideap(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.HexCer_AP:
                    return LipidMsmsCharacterization.JudgeIfHexceramideap(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
               
                case LbmClass.SHexCer:
                    return LipidMsmsCharacterization.JudgeIfShexcer(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized);
                    
                case LbmClass.GM3:
                    return LipidMsmsCharacterization.JudgeIfGm3(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.DHSph:
                    return LipidMsmsCharacterization.JudgeIfSphinganine(spectrum, ms2tol, refMz,
                        molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, adduct);
                    
                case LbmClass.Sph:
                    return LipidMsmsCharacterization.JudgeIfSphingosine(spectrum, ms2tol, refMz,
                        molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, adduct);
                    
                case LbmClass.PhytoSph:
                    return LipidMsmsCharacterization.JudgeIfPhytosphingosine(spectrum, ms2tol, refMz,
                        molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, adduct);

                case LbmClass.TG:
                    var sn2Carbon = molecule.Sn2CarbonCount;
                    var sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfTriacylglycerol(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                        sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.ADGGA:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfAcylglcadg(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);
                case LbmClass.HBMP:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfHemiismonoacylglycerophosphate(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.EtherTG:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfEthertag(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);
                    
                case LbmClass.MLCL:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfLysocardiolipin(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);
                    
                case LbmClass.Cer_EOS:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfCeramideeos(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);
                    
                case LbmClass.Cer_EODS:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfCeramideeods(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);
                    
                case LbmClass.HexCer_EOS:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfHexceramideeos(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);
                    
                case LbmClass.ASM:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfAcylsm(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, 
                         sn2Carbon, sn2DbBond, sn2DbBond, adduct);
                    
                case LbmClass.Cer_EBDS:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfAcylcerbds(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.AHexCer:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfAcylhexceras(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized,sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.ASHexCer:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfAshexcer(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.CL:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    var sn3Carbon = molecule.Sn3CarbonCount;
                    var sn3DbBond = molecule.Sn3DoubleBondCount;
                    if (sn3Carbon < 1) {
                        return LipidMsmsCharacterization.JudgeIfCardiolipin(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }
                    else {
                        return LipidMsmsCharacterization.JudgeIfCardiolipin(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                        sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, sn3Carbon, sn3Carbon, sn3DbBond, sn3DbBond, adduct);
                    }

                //add 10/04/19
                case LbmClass.EtherPI:
                    return LipidMsmsCharacterization.JudgeIfEtherpi(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.EtherPS:
                    return LipidMsmsCharacterization.JudgeIfEtherps(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.EtherDG:
                    return LipidMsmsCharacterization.JudgeIfEtherDAG(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PI_Cer:
                    return LipidMsmsCharacterization.JudgeIfPicermide(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized);
                case LbmClass.PE_Cer:
                    return LipidMsmsCharacterization.JudgeIfPecermide(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized);

                //add 13/5/19
                case LbmClass.DCAE:
                    return LipidMsmsCharacterization.JudgeIfDcae(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);
                    
                case LbmClass.GDCAE:
                    return LipidMsmsCharacterization.JudgeIfGdcae(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);
                    
                case LbmClass.GLCAE:
                    return LipidMsmsCharacterization.JudgeIfGlcae(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);
                    
                case LbmClass.TDCAE:
                    return LipidMsmsCharacterization.JudgeIfTdcae(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);
                    
                case LbmClass.TLCAE:
                    return LipidMsmsCharacterization.JudgeIfTlcae(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);
                    
                case LbmClass.NAE:
                    return LipidMsmsCharacterization.JudgeIfAnandamide(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                    
                case LbmClass.NAGly:
                    if (totalCarbon < 29)
                    {
                        return LipidMsmsCharacterization.JudgeIfNAcylGlyOxFa(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, adduct);
                    }
                    else {
                        return LipidMsmsCharacterization.JudgeIfFahfamidegly(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }
                    
                    
                case LbmClass.NAGlySer:
                    if (totalCarbon < 29)
                    {
                        return LipidMsmsCharacterization.JudgeIfNAcylGlySerOxFa(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, adduct);
                    }
                    else {
                        return LipidMsmsCharacterization.JudgeIfFahfamideglyser(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }
                    

                case LbmClass.SL:
                    return LipidMsmsCharacterization.JudgeIfSulfonolipid(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,adduct, totalOxidized);
                    
                case LbmClass.EtherPG:
                    return LipidMsmsCharacterization.JudgeIfEtherpg(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.EtherLPG:
                    return LipidMsmsCharacterization.JudgeIfEtherlysopg(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    
                case LbmClass.CoQ:
                    return LipidMsmsCharacterization.JudgeIfCoenzymeq(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);


                case LbmClass.Vitamin_E:
                    return LipidMsmsCharacterization.JudgeIfVitaminEmolecules(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.Vitamin_D:
                    return LipidMsmsCharacterization.JudgeIfVitaminDmolecules(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);


                case LbmClass.VAE:
                    return LipidMsmsCharacterization.JudgeIfVitaminaestermolecules(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                    

                case LbmClass.NAOrn:
                    if (totalCarbon < 29)
                    {
                        return LipidMsmsCharacterization.JudgeIfNAcylOrnOxFa(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, adduct);
                    }
                    else {
                        return LipidMsmsCharacterization.JudgeIfFahfamideorn(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }


                case LbmClass.BRSE:
                    return LipidMsmsCharacterization.JudgeIfBrseSpecies(spectrum, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);
                    
                case LbmClass.CASE:
                    return LipidMsmsCharacterization.JudgeIfCaseSpecies(spectrum, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);
                    
                case LbmClass.SISE:
                    return LipidMsmsCharacterization.JudgeIfSiseSpecies(spectrum, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);
                    
                case LbmClass.STSE:
                    return LipidMsmsCharacterization.JudgeIfStseSpecies(spectrum, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);
                    

                case LbmClass.AHexBRS:
                    return LipidMsmsCharacterization.JudgeIfAhexbrseSpecies(spectrum, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);
                    
                case LbmClass.AHexCAS:
                    return LipidMsmsCharacterization.JudgeIfAhexcaseSpecies(spectrum, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);
                    
                case LbmClass.AHexCS:
                    return LipidMsmsCharacterization.JudgeIfAhexceSpecies(spectrum, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);
                    
                case LbmClass.AHexSIS:
                    return LipidMsmsCharacterization.JudgeIfAhexsiseSpecies(spectrum, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);
                    
                case LbmClass.AHexSTS:
                    return LipidMsmsCharacterization.JudgeIfAhexstseSpecies(spectrum, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);
                    

                // add 27/05/19
                case LbmClass.Cer_AS:
                    return LipidMsmsCharacterization.JudgeIfCeramideas(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_ADS:
                    return LipidMsmsCharacterization.JudgeIfCeramideads(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_BS:
                    return LipidMsmsCharacterization.JudgeIfCeramidebs(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_BDS:
                    return LipidMsmsCharacterization.JudgeIfCeramidebds(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_NP:
                    return LipidMsmsCharacterization.JudgeIfCeramidenp(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_OS:
                    return LipidMsmsCharacterization.JudgeIfCeramideos(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
              
                    //add 190528
                case LbmClass.Cer_HS:
                    return LipidMsmsCharacterization.JudgeIfCeramideo(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_HDS:
                    return LipidMsmsCharacterization.JudgeIfCeramideo(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_NDOS:
                    return LipidMsmsCharacterization.JudgeIfCeramidedos(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.HexCer_HS:
                    return LipidMsmsCharacterization.JudgeIfHexceramideo(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.HexCer_HDS:
                    return LipidMsmsCharacterization.JudgeIfHexceramideo(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                //190801
                case LbmClass.SHex:
                    return LipidMsmsCharacterization.JudgeIfSterolHexoside(molecule.LipidName, molecule.LipidClass,
                        spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.BAHex:
                    return LipidMsmsCharacterization.JudgeIfSterolHexoside(molecule.LipidName, molecule.LipidClass,
                        spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.SSulfate:
                    return LipidMsmsCharacterization.JudgeIfSterolSulfate(molecule.LipidName, molecule.LipidClass,
                        spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.BASulfate:
                    return LipidMsmsCharacterization.JudgeIfSterolSulfate(molecule.LipidName, molecule.LipidClass,
                        spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);

                // added 190811
                case LbmClass.CerP:
                    return LipidMsmsCharacterization.JudgeIfCeramidePhosphate(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                ///2019/11/25 add
                case LbmClass.SMGDG:
                    return LipidMsmsCharacterization.JudgeIfSmgdg(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.EtherSMGDG:
                    return LipidMsmsCharacterization.JudgeIfEtherSmgdg(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
               
                //add 20200218
                case LbmClass.LCAE:
                    return LipidMsmsCharacterization.JudgeIfLcae(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                case LbmClass.KLCAE:
                    return LipidMsmsCharacterization.JudgeIfKlcae(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                case LbmClass.KDCAE:
                    return LipidMsmsCharacterization.JudgeIfKdcae(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                //add 20200714
                case LbmClass.DMPE:
                    return LipidMsmsCharacterization.JudgeIfDiMethylPE(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.MMPE:
                    return LipidMsmsCharacterization.JudgeIfMonoMethylPE(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.MIPC:
                    return LipidMsmsCharacterization.JudgeIfMipc(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                //add 20200720
                case LbmClass.EGSE:
                    return LipidMsmsCharacterization.JudgeIfErgoSESpecies(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.DEGSE:
                    return LipidMsmsCharacterization.JudgeIfDehydroErgoSESpecies(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);

                //add 20200812
                case LbmClass.OxTG:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    
                    return LipidMsmsCharacterization.JudgeIfOxTriacylglycerol(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                        sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, totalOxidized, adduct);
                case LbmClass.TG_EST:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    sn3Carbon = molecule.Sn3CarbonCount;
                    sn3DbBond = molecule.Sn3DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfFahfaTriacylglycerol(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                        sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond,
                        sn3Carbon, sn3Carbon, sn3DbBond, sn3DbBond, adduct);
                //add 20200923
                case LbmClass.DSMSE:
                    return LipidMsmsCharacterization.JudgeIfDesmosterolSpecies(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                //add20210216
                case LbmClass.GPNAE:
                    return LipidMsmsCharacterization.JudgeIfGpnae(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.MGMG:
                    return LipidMsmsCharacterization.JudgeIfMgmg(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.DGMG:
                    return LipidMsmsCharacterization.JudgeIfDgmg(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                //add 20210315
                case LbmClass.GD1a:
                    return LipidMsmsCharacterization.JudgeIfGD1a(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GD1b:
                    return LipidMsmsCharacterization.JudgeIfGD1b(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GD2:
                    return LipidMsmsCharacterization.JudgeIfGD2(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GD3:
                    return LipidMsmsCharacterization.JudgeIfGD3(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GM1:
                    return LipidMsmsCharacterization.JudgeIfGM1(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GQ1b:
                    return LipidMsmsCharacterization.JudgeIfGQ1b(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GT1b:
                    return LipidMsmsCharacterization.JudgeIfGT1b(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.NGcGM3:
                    return LipidMsmsCharacterization.JudgeIfNGcGM3(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.ST:
                    return LipidMsmsCharacterization.JudgeIfnoChainSterol(molecule.LipidName, molecule.LipidClass,
                        spectrum, ms2tol, refMz, totalCarbon, totalDbBond, adduct);

                case LbmClass.CSLPHex:
                case LbmClass.BRSLPHex:
                case LbmClass.CASLPHex:
                case LbmClass.SISLPHex:
                case LbmClass.STSLPHex:
                    return LipidMsmsCharacterization.JudgeIfSteroidWithLpa(molecule.LipidName, molecule.LipidClass,
                        spectrum, ms2tol, refMz, totalCarbon, totalDbBond, adduct);

                case LbmClass.CSPHex:
                case LbmClass.BRSPHex:
                case LbmClass.CASPHex:
                case LbmClass.SISPHex:
                case LbmClass.STSPHex:
                    return LipidMsmsCharacterization.JudgeIfSteroidWithPa(molecule.LipidName, molecule.LipidClass,
                        spectrum, ms2tol, refMz, totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                //20220201
                case LbmClass.SPE:
                    return LipidMsmsCharacterization.JudgeIfSpeSpecies(molecule.LipidName, molecule.LipidClass,
                        spectrum, ms2tol, refMz, totalCarbon, totalDbBond, adduct);
                //20220322
                case LbmClass.NAPhe:
                    return LipidMsmsCharacterization.JudgeIfNAcylPheFa(spectrum, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                case LbmClass.NATau:
                        return LipidMsmsCharacterization.JudgeIfNAcylTauFa(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, adduct);
                //20221019
                case LbmClass.PT:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylThreonine(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                //20230407
                case LbmClass.PC_d5:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylcholineD5(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PE_d5:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylethanolamineD5(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PS_d5:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylserineD5(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PG_d5:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylglycerolD5(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PI_d5:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylinositolD5(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.LPC_d5:
                    return LipidMsmsCharacterization.JudgeIfLysopcD5(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.LPE_d5:
                    return LipidMsmsCharacterization.JudgeIfLysopeD5(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.LPG_d5:
                    return LipidMsmsCharacterization.JudgeIfLysopgD5(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.LPI_d5:
                    return LipidMsmsCharacterization.JudgeIfLysopiD5(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.LPS_d5:
                    return LipidMsmsCharacterization.JudgeIfLysopsD5(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.TG_d5:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfTriacylglycerolD5(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                        sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);
                case LbmClass.DG_d5:
                    return LipidMsmsCharacterization.JudgeIfDagD5(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.SM_d9:
                    return LipidMsmsCharacterization.JudgeIfSphingomyelinD9(spectrum, ms2tol, refMz,
                    totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.CE_d7:
                    return LipidMsmsCharacterization.JudgeIfCholesterylEsterD7(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                case LbmClass.Cer_NS_d7:
                    return LipidMsmsCharacterization.JudgeIfCeramidensD7(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                //20230424
                case LbmClass.bmPC:
                    return LipidMsmsCharacterization.JudgeIfBetaMethylPhosphatidylcholine(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                default:
                    return null;
            }
        }

        /// <summary>
        /// This program will return so called reverse dot product similarity as described in the previous resport.
        /// Stein, S. E. An Integrated Method for Spectrum Extraction. J.Am.Soc.Mass.Spectrom, 10, 770-781, 1999.
        /// The spectrum similarity of MS/MS with respect to library spectrum will be calculated in this method.
        /// </summary>
        /// <param name="measuredSpectra">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="librarySpectra">
        /// Add the theoretical MS/MS spectrum. The theoretical MS/MS spectrum is supposed to be retreived in MSP parcer.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetReverseSearchSimilarity(ObservableCollection<double[]> measuredSpectra, List<MzIntensityCommentBean> librarySpectra, float bin, 
            float massBegin, float massEnd)
        {
            double scalarM = 0, scalarR = 0, covariance = 0;
            double sumM = 0, sumL = 0;

            if (librarySpectra.Count == 0) return 0;

            double minMz = librarySpectra[0].Mz;
            double maxMz = librarySpectra[librarySpectra.Count - 1].Mz;

            if (massBegin > minMz) minMz = massBegin;
            if (maxMz > massEnd) maxMz = massEnd;

            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;
            int counter = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            double sumMeasure = 0, sumReference = 0, baseM = double.MinValue, baseR = double.MinValue;

            while (focusedMz <= maxMz)
            {
                sumL = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++) {
                    if (librarySpectra[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= librarySpectra[i].Mz && librarySpectra[i].Mz < focusedMz + bin)
                        sumL += librarySpectra[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                sumM = 0;
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++) {
                    if (measuredSpectra[i][0] < focusedMz - bin) continue;
                    else if (focusedMz - bin <= measuredSpectra[i][0] && measuredSpectra[i][0] < focusedMz + bin)
                        sumM += measuredSpectra[i][1];
                    else { remaindIndexM = i; break; }
                }

                if (sumM <= 0) {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumL });
                    if (sumL > baseR) baseR = sumL;
                }
                else {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumL });
                    if (sumL > baseR) baseR = sumL;

                    counter++;
                }

                if (focusedMz + bin > librarySpectra[librarySpectra.Count - 1].Mz) break;
                focusedMz = librarySpectra[remaindIndexL].Mz;
            }

            if (baseM == 0 || baseR == 0) return 0;

            var eSpectrumCounter = 0;
            var lSpectrumCounter = 0;
            for (int i = 0; i < measuredMassList.Count; i++) {
                measuredMassList[i][1] = measuredMassList[i][1] / baseM;
                referenceMassList[i][1] = referenceMassList[i][1] / baseR;
                sumMeasure += measuredMassList[i][1];
                sumReference += referenceMassList[i][1];

                if (measuredMassList[i][1] > 0.1) eSpectrumCounter++;
                if (referenceMassList[i][1] > 0.1) lSpectrumCounter++;
            }

            var peakCountPenalty = 1.0;
            if (lSpectrumCounter == 1) peakCountPenalty = 0.75;
            else if (lSpectrumCounter == 2) peakCountPenalty = 0.88;
            else if (lSpectrumCounter == 3) peakCountPenalty = 0.94;
            else if (lSpectrumCounter == 4) peakCountPenalty = 0.97;

            double wM, wR;

            if (sumMeasure - 0.5 == 0) wM = 0;
            else wM = 1 / (sumMeasure - 0.5);

            if (sumReference - 0.5 == 0) wR = 0;
            else wR = 1 / (sumReference - 0.5);

            var cutoff = 0.01;

            for (int i = 0; i < measuredMassList.Count; i++) {
                if (referenceMassList[i][1] < cutoff)
                    continue;

                scalarM += measuredMassList[i][1] * measuredMassList[i][0];
                scalarR += referenceMassList[i][1] * referenceMassList[i][0];
                covariance += Math.Sqrt(measuredMassList[i][1] * referenceMassList[i][1]) * measuredMassList[i][0];

                //scalarM += measuredMassList[i][1];
                //scalarR += referenceMassList[i][1];
                //covariance += Math.Sqrt(measuredMassList[i][1] * referenceMassList[i][1]);
            }

            if (scalarM == 0 || scalarR == 0) { return 0; }
            else { return Math.Pow(covariance, 2) / scalarM / scalarR * peakCountPenalty; }
        }

        /// <summary>
        /// This program will return so called dot product similarity as described in the previous resport.
        /// Stein, S. E. An Integrated Method for Spectrum Extraction. J.Am.Soc.Mass.Spectrom, 10, 770-781, 1999.
        /// The spectrum similarity of MS/MS will be calculated in this method.
        /// </summary>
        /// <param name="measuredSpectra">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="librarySpectra">
        /// Add the theoretical MS/MS spectrum. The theoretical MS/MS spectrum is supposed to be retreived in MSP parcer.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetMassSpectraSimilarity(ObservableCollection<double[]> measuredSpectra, List<MzIntensityCommentBean> librarySpectra, float bin,
            float massBegin, float massEnd)
        {
            double scalarM = 0, scalarR = 0, covariance = 0;
            double sumM = 0, sumR = 0;

            if (measuredSpectra.Count == 0) return 0;
            if (librarySpectra.Count == 0) return 0;

            double minMz = Math.Min(measuredSpectra[0][0], librarySpectra[0].Mz);
            double maxMz = Math.Max(measuredSpectra[measuredSpectra.Count - 1][0], librarySpectra[librarySpectra.Count - 1].Mz);

            if (massBegin > minMz) minMz = massBegin;
            if (maxMz > massEnd) maxMz = massEnd;

            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            double sumMeasure = 0, sumReference = 0, baseM = double.MinValue, baseR = double.MinValue;

            while (focusedMz <= maxMz)
            {
                sumM = 0;
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++) {
                    if (measuredSpectra[i][0] < focusedMz - bin) { continue; }
                    else if (focusedMz - bin <= measuredSpectra[i][0] && measuredSpectra[i][0] < focusedMz + bin) sumM += measuredSpectra[i][1];
                    else { remaindIndexM = i; break; }
                }

                sumR = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++) {
                    if (librarySpectra[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= librarySpectra[i].Mz && librarySpectra[i].Mz < focusedMz + bin)
                        sumR += librarySpectra[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                if (sumM <= 0 && sumR > 0) {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumR });
                    if (sumR > baseR) baseR = sumR;
                }
                else {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumR });
                    if (sumR > baseR) baseR = sumR;
                }

                if (focusedMz + bin > Math.Max(measuredSpectra[measuredSpectra.Count - 1][0], librarySpectra[librarySpectra.Count - 1].Mz)) break;
                if (focusedMz + bin > librarySpectra[remaindIndexL].Mz && focusedMz + bin <= measuredSpectra[remaindIndexM][0])
                    focusedMz = measuredSpectra[remaindIndexM][0];
                else if (focusedMz + bin <= librarySpectra[remaindIndexL].Mz && focusedMz + bin > measuredSpectra[remaindIndexM][0])
                    focusedMz = librarySpectra[remaindIndexL].Mz;
                else
                    focusedMz = Math.Min(measuredSpectra[remaindIndexM][0], librarySpectra[remaindIndexL].Mz);
            }

            if (baseM == 0 || baseR == 0) return 0;


            var eSpectrumCounter = 0;
            var lSpectrumCounter = 0;
            for (int i = 0; i < measuredMassList.Count; i++) {
                measuredMassList[i][1] = measuredMassList[i][1] / baseM;
                referenceMassList[i][1] = referenceMassList[i][1] / baseR;
                sumMeasure += measuredMassList[i][1];
                sumReference += referenceMassList[i][1];

                if (measuredMassList[i][1] > 0.1) eSpectrumCounter++;
                if (referenceMassList[i][1] > 0.1) lSpectrumCounter++;
            }

            var peakCountPenalty = 1.0;
            if (lSpectrumCounter == 1) peakCountPenalty = 0.75;
            else if (lSpectrumCounter == 2) peakCountPenalty = 0.88;
            else if (lSpectrumCounter == 3) peakCountPenalty = 0.94;
            else if (lSpectrumCounter == 4) peakCountPenalty = 0.97;

            double wM, wR;

            if (sumMeasure - 0.5 == 0) wM = 0;
            else wM = 1 / (sumMeasure - 0.5);

            if (sumReference - 0.5 == 0) wR = 0;
            else wR = 1 / (sumReference - 0.5);

            var cutoff = 0.01;
            for (int i = 0; i < measuredMassList.Count; i++) {
                if (measuredMassList[i][1] < cutoff)
                    continue;

                scalarM += measuredMassList[i][1] * measuredMassList[i][0];
                scalarR += referenceMassList[i][1] * referenceMassList[i][0];
                covariance += Math.Sqrt(measuredMassList[i][1] * referenceMassList[i][1]) * measuredMassList[i][0];

                //scalarM += measuredMassList[i][1];
                //scalarR += referenceMassList[i][1];
                //covariance += Math.Sqrt(measuredMassList[i][1] * referenceMassList[i][1]);
            }

            if (scalarM == 0 || scalarR == 0) { return 0; }
            else { return Math.Pow(covariance, 2) / scalarM / scalarR * peakCountPenalty; }
        }

        public static double GetSimpleDotProductSimilarity(ObservableCollection<double[]> measuredSpectra, List<MzIntensityCommentBean> librarySpectra, float bin, float massBegin, float massEnd) {
            double scalarM = 0, scalarR = 0, covariance = 0;
            double sumM = 0, sumR = 0;

            if (measuredSpectra.Count == 0) return 0;
            if (librarySpectra.Count == 0) return 0;

            double minMz = Math.Min(measuredSpectra[0][0], librarySpectra[0].Mz);
            double maxMz = Math.Max(measuredSpectra[measuredSpectra.Count - 1][0], librarySpectra[librarySpectra.Count - 1].Mz);
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;

            if (massBegin > minMz) minMz = massBegin;
            if (maxMz > massEnd) maxMz = massEnd;


            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            double sumMeasure = 0, sumReference = 0, baseM = double.MinValue, baseR = double.MinValue;

            while (focusedMz <= maxMz) {
                sumM = 0;
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++) {
                    if (measuredSpectra[i][0] < focusedMz - bin) { continue; }
                    else if (focusedMz - bin <= measuredSpectra[i][0] && measuredSpectra[i][0] < focusedMz + bin) sumM += measuredSpectra[i][1];
                    else { remaindIndexM = i; break; }
                }

                sumR = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++) {
                    if (librarySpectra[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= librarySpectra[i].Mz && librarySpectra[i].Mz < focusedMz + bin)
                        sumR += librarySpectra[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                measuredMassList.Add(new double[] { focusedMz, sumM });
                if (sumM > baseM) baseM = sumM;

                referenceMassList.Add(new double[] { focusedMz, sumR });
                if (sumR > baseR) baseR = sumR;

                if (focusedMz + bin > Math.Max(measuredSpectra[measuredSpectra.Count - 1][0], librarySpectra[librarySpectra.Count - 1].Mz)) break;
                if (focusedMz + bin > librarySpectra[remaindIndexL].Mz && focusedMz + bin <= measuredSpectra[remaindIndexM][0])
                    focusedMz = measuredSpectra[remaindIndexM][0];
                else if (focusedMz + bin <= librarySpectra[remaindIndexL].Mz && focusedMz + bin > measuredSpectra[remaindIndexM][0])
                    focusedMz = librarySpectra[remaindIndexL].Mz;
                else
                    focusedMz = Math.Min(measuredSpectra[remaindIndexM][0], librarySpectra[remaindIndexL].Mz);
            }

            if (baseM == 0 || baseR == 0) return 0;

            for (int i = 0; i < measuredMassList.Count; i++) {
                measuredMassList[i][1] = measuredMassList[i][1] / baseM * 999;
                referenceMassList[i][1] = referenceMassList[i][1] / baseR * 999;
            }

            for (int i = 0; i < measuredMassList.Count; i++) {
                scalarM += measuredMassList[i][1];
                scalarR += referenceMassList[i][1];
                covariance += Math.Sqrt(measuredMassList[i][1] * referenceMassList[i][1]);
            }

            if (scalarM == 0 || scalarR == 0) { return 0; }
            else {
                return Math.Pow(covariance, 2) / scalarM / scalarR;
            }
        }

        /// <summary>
        /// This method is to calculate the similarity of retention time differences or precursor ion difference from the library information as described in the previous report.
        /// Tsugawa, H. et al. Anal.Chem. 85, 5191-5199, 2013.
        /// </summary>
        /// <param name="actual">
        /// Add the experimental m/z or retention time.
        /// </param>
        /// <param name="reference">
        /// Add the theoretical m/z or library's retention time.
        /// </param>
        /// <param name="tolrance">
        /// Add the user-defined search tolerance.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetGaussianSimilarity(float actual, float reference, float tolrance)
        {
            return Math.Exp(-0.5 * Math.Pow((actual - reference) / tolrance, 2));
        }

        /// <summary>
        /// MS-DIAL program utilizes the total similarity score to rank the compound candidates.
        /// This method is to calculate it from four scores including RT, isotopic ratios, m/z, and MS/MS similarities.
        /// </summary>
        /// <param name="accurateMassSimilarity"></param>
        /// <param name="rtSimilarity"></param>
        /// <param name="isotopeSimilarity"></param>
        /// <param name="spectraSimilarity"></param>
        /// <param name="reverseSearchSimilarity"></param>
        /// <param name="presenceSimilarity"></param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetTotalSimilarity(double accurateMassSimilarity, double rtSimilarity, double isotopeSimilarity, 
            double spectraSimilarity, double reverseSearchSimilarity, double presenceSimilarity, bool spectrumPenalty, TargetOmics targetOmics, bool isUseRT)
        {
            var dotProductFactor = 3.0;
            var revesrseDotProdFactor = 2.0;
            var presensePercentageFactor = 1.0;

            var msmsFactor = 2.0;
            var rtFactor = 1.0;
            var massFactor = 1.0;
            var isotopeFactor = 0.0;

            if (targetOmics == TargetOmics.Lipidomics) {
                dotProductFactor = 1.0; revesrseDotProdFactor = 2.0; presensePercentageFactor = 3.0; msmsFactor = 1.5; rtFactor = 0.5;
            }

            var msmsSimilarity = 
                (dotProductFactor * spectraSimilarity + revesrseDotProdFactor * reverseSearchSimilarity + presensePercentageFactor * presenceSimilarity) /
                (dotProductFactor + revesrseDotProdFactor + presensePercentageFactor);

            if (spectrumPenalty == true && targetOmics == TargetOmics.Metablomics) msmsSimilarity = msmsSimilarity * 0.5;

            if (!isUseRT) {
                if (isotopeSimilarity < 0) {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity)
                        / (msmsFactor + massFactor);
                }
                else {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + isotopeFactor);
                }
            }
            else {
                if (rtSimilarity < 0 && isotopeSimilarity < 0) {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity)
                        / (msmsFactor + massFactor + rtFactor);
                }
                else if (rtSimilarity < 0 && isotopeSimilarity >= 0) {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + isotopeFactor + rtFactor);
                }
                else if (isotopeSimilarity < 0 && rtSimilarity >= 0) {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity)
                        / (msmsFactor + massFactor + rtFactor);
                }
                else {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + rtFactor + isotopeFactor);
                }
            }
        }

        public static double GetTotalSimilarity(double accurateMassSimilarity, double rtSimilarity, double ccsSimilarity, double isotopeSimilarity,
            double spectraSimilarity, double reverseSearchSimilarity, double presenceSimilarity, bool spectrumPenalty, TargetOmics targetOmics, bool isUseRT, bool isUseCcs) {
            var dotProductFactor = 3.0;
            var revesrseDotProdFactor = 2.0;
            var presensePercentageFactor = 1.0;

            var msmsFactor = 2.0;
            var rtFactor = 1.0;
            var massFactor = 1.0;
            var isotopeFactor = 0.0;
            var ccsFactor = 2.0;

            if (targetOmics == TargetOmics.Lipidomics) {
                dotProductFactor = 1.0; revesrseDotProdFactor = 2.0; presensePercentageFactor = 3.0; msmsFactor = 1.5; rtFactor = 0.5; ccsFactor = 1.0F;
            }

            var msmsSimilarity =
                (dotProductFactor * spectraSimilarity + revesrseDotProdFactor * reverseSearchSimilarity + presensePercentageFactor * presenceSimilarity) /
                (dotProductFactor + revesrseDotProdFactor + presensePercentageFactor);

            if (spectrumPenalty == true && targetOmics == TargetOmics.Metablomics) msmsSimilarity = msmsSimilarity * 0.5;

            var useRtScore = true;
            var useCcsScore = true;
            var useIsotopicScore = true;
            if (!isUseRT || rtSimilarity < 0) useRtScore = false;
            if (!isUseCcs || ccsSimilarity < 0) useCcsScore = false;
            if (isotopeSimilarity < 0) useIsotopicScore = false;

            if (useRtScore == true && useCcsScore == true && useIsotopicScore == true) {
                return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + isotopeFactor * isotopeSimilarity + ccsFactor * ccsSimilarity)
                        / (msmsFactor + massFactor + rtFactor + isotopeFactor + ccsFactor);
            }
            else if (useRtScore == true && useCcsScore == true && useIsotopicScore == false) {
                return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + ccsFactor * ccsSimilarity)
                        / (msmsFactor + massFactor + rtFactor + ccsFactor);
            }
            else if (useRtScore == true && useCcsScore == false && useIsotopicScore == true) {
                return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + rtFactor + isotopeFactor);
            }
            else if (useRtScore == false && useCcsScore == true && useIsotopicScore == true) {
                return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity + ccsFactor * ccsSimilarity)
                        / (msmsFactor + massFactor + isotopeFactor + ccsFactor);
            }
            else if (useRtScore == false && useCcsScore == true && useIsotopicScore == false) {
                return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + ccsFactor * ccsSimilarity)
                      / (msmsFactor + massFactor + ccsFactor);
            }
            else if (useRtScore == true && useCcsScore == false && useIsotopicScore == false) {
                return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity)
                        / (msmsFactor + massFactor + rtFactor);
            }
            else if (useRtScore == false && useCcsScore == false && useIsotopicScore == true) {
                return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + isotopeFactor);
            }
            else {
                return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity)
                        / (msmsFactor + massFactor);
            }
            //if (!isUseRT) {
            //    if (isotopeSimilarity < 0 && ccsSimilarity < 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity)
            //            / (msmsFactor + massFactor);
            //    }
            //    else if (isotopeSimilarity < 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + ccsFactor * ccsSimilarity)
            //            / (msmsFactor + massFactor + ccsFactor);
            //    }
            //    else {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity + ccsFactor * ccsSimilarity)
            //            / (msmsFactor + massFactor + isotopeFactor + ccsFactor);
            //    }
            //}
            //else {
            //    if (rtSimilarity < 0 && isotopeSimilarity < 0 && ccsSimilarity < 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity)
            //            / (msmsFactor + massFactor);
            //    }
            //    else if (rtSimilarity < 0 && isotopeSimilarity >= 0 && ccsSimilarity < 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity)
            //            / (msmsFactor + massFactor + isotopeFactor);
            //    }
            //    else if (isotopeSimilarity < 0 && rtSimilarity >= 0 && ccsSimilarity < 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity)
            //            / (msmsFactor + massFactor + rtFactor);
            //    }
            //    else if (isotopeSimilarity < 0 && rtSimilarity < 0 && ccsSimilarity >= 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + ccsFactor * ccsSimilarity)
            //            / (msmsFactor + massFactor + ccsFactor);
            //    }
            //    else if (rtSimilarity >= 0 && isotopeSimilarity >= 0 && ccsSimilarity < 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity + rtFactor * rtSimilarity)
            //            / (msmsFactor + massFactor + isotopeFactor + rtFactor);
            //    }
            //    else if (isotopeSimilarity < 0 && rtSimilarity >= 0 && ccsSimilarity >= 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + ccsFactor * ccsSimilarity)
            //            / (msmsFactor + massFactor + rtFactor + ccsFactor);
            //    }
            //    else if (isotopeSimilarity >= 0 && rtSimilarity < 0 && ccsSimilarity >= 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + ccsFactor * ccsSimilarity + isotopeFactor * isotopeSimilarity)
            //            / (msmsFactor + massFactor + ccsFactor + isotopeFactor);
            //    }
            //    else {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + isotopeFactor * isotopeSimilarity + ccsFactor * ccsSimilarity)
            //            / (msmsFactor + massFactor + rtFactor + isotopeFactor + ccsFactor);
            //    }
            //}
        }

        /// <summary>
        /// MS-DIAL program also calculate the total similarity score without the MS/MS similarity scoring.
        /// It means that the total score will be calculated from RT, m/z, and isotopic similarities.
        /// </summary>
        /// <param name="accurateMassSimilarity"></param>
        /// <param name="rtSimilarity"></param>
        /// <param name="isotopeSimilarity"></param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetTotalSimilarity(double accurateMassSimilarity, double rtSimilarity, double isotopeSimilarity, bool isUseRT)
        {
            if (!isUseRT) {
                if (isotopeSimilarity < 0) {
                    return accurateMassSimilarity;
                }
                else {
                    return (accurateMassSimilarity + 0.5 * isotopeSimilarity) / 1.5;
                }
            }
            else {
                if (rtSimilarity < 0 && isotopeSimilarity < 0) {
                    return accurateMassSimilarity * 0.5;
                }
                else if (rtSimilarity < 0 && isotopeSimilarity >= 0) {
                    return (accurateMassSimilarity + 0.5 * isotopeSimilarity) / 2.5;
                }
                else if (isotopeSimilarity < 0 && rtSimilarity >= 0) {
                    return (accurateMassSimilarity + rtSimilarity) * 0.5;
                }
                else {
                    return (accurateMassSimilarity + rtSimilarity + 0.5 * isotopeSimilarity) * 0.4;
                }
            }
        }

        public static double GetTotalSimilarity(double accurateMassSimilarity, double rtSimilarity, double ccsSimilarity, double isotopeSimilarity, bool isUseRT, bool isUseCcs) {

            var rtFactor = 1.0;
            var massFactor = 1.0;
            var isotopeFactor = 0.0;
            var ccsFactor = 2.0;

            var useRtScore = true;
            var useCcsScore = true;
            var useIsotopicScore = true;
            if (!isUseRT || rtSimilarity < 0) useRtScore = false;
            if (!isUseCcs || ccsSimilarity < 0) useCcsScore = false;
            if (isotopeSimilarity < 0) useIsotopicScore = false;

            if (useRtScore == true && useCcsScore == true && useIsotopicScore == true) {
                return (massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + isotopeFactor * isotopeSimilarity + ccsFactor * ccsSimilarity)
                        / (massFactor + rtFactor + isotopeFactor + ccsFactor);
            }
            else if (useRtScore == true && useCcsScore == true && useIsotopicScore == false) {
                return (massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + ccsFactor * ccsSimilarity)
                        / (massFactor + rtFactor + ccsFactor);
            }
            else if (useRtScore == true && useCcsScore == false && useIsotopicScore == true) {
                return (massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + isotopeFactor * isotopeSimilarity)
                        / (massFactor + rtFactor + isotopeFactor);
            }
            else if (useRtScore == false && useCcsScore == true && useIsotopicScore == true) {
                return (massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity + ccsFactor * ccsSimilarity)
                        / (massFactor + isotopeFactor + ccsFactor);
            }
            else if (useRtScore == false && useCcsScore == true && useIsotopicScore == false) {
                return (massFactor * accurateMassSimilarity + ccsFactor * ccsSimilarity)
                      / (massFactor + ccsFactor);
            }
            else if (useRtScore == true && useCcsScore == false && useIsotopicScore == false) {
                return (massFactor * accurateMassSimilarity + rtFactor * rtSimilarity)
                        / (massFactor + rtFactor);
            }
            else if (useRtScore == false && useCcsScore == false && useIsotopicScore == true) {
                return (massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity)
                        / (massFactor + isotopeFactor);
            }
            else {
                return (massFactor * accurateMassSimilarity)
                        / massFactor;
            }
        }

        public static double GetTotalSimilarityUsingSimpleDotProduct(double accurateMassSimilarity, double rtSimilarity, double isotopeSimilarity,
            double dotProductSimilarity, bool spectrumPenalty, TargetOmics targetOmics, bool isUseRT)
        {
            var msmsFactor = 2.0;
            var rtFactor = 1.0;
            var massFactor = 1.0;
            var isotopeFactor = 0.0;

            var msmsSimilarity = dotProductSimilarity;

            if (spectrumPenalty == true && targetOmics == TargetOmics.Metablomics) msmsSimilarity = msmsSimilarity * 0.5;

            if (!isUseRT)
            {
                if (isotopeSimilarity < 0)
                {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity)
                        / (msmsFactor + massFactor);
                }
                else
                {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + isotopeFactor);
                }
            }
            else
            {
                if (rtSimilarity < 0 && isotopeSimilarity < 0)
                {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity)
                        / (msmsFactor + massFactor + rtFactor);
                }
                else if (rtSimilarity < 0 && isotopeSimilarity >= 0)
                {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + isotopeFactor + rtFactor);
                }
                else if (isotopeSimilarity < 0 && rtSimilarity >= 0)
                {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity)
                        / (msmsFactor + massFactor + rtFactor);
                }
                else
                {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + rtFactor + isotopeFactor);
                }
            }
        }
    }
}
