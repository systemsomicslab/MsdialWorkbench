using CompMs.Common.MessagePack;
using Msdial.Lcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Msdial.Lcms.Dataprocess.Algorithm {

    public sealed class PosNegAmalgamator {
        private PosNegAmalgamator() {

        }

        // this method is to tentatively make the peak list from the file
        // the file should be [0] m/z [1] rt with the header row
        // the file should only contain monoisotopic ions
        public static List<PeakAreaBean> GenerateTempPeakAreaBeans(string filepath) {
            var peaks = new List<PeakAreaBean>();
            var counter = 0;
            using (var sr = new StreamReader(filepath, Encoding.ASCII, false)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    var lineArray = line.Split('\t');
                    if (lineArray.Length < 2) continue;

                    float mz = 0.0F, rt =0.0F;
                    float.TryParse(lineArray[0], out mz);
                    float.TryParse(lineArray[1], out rt);
                    if (mz == 0.0 || rt == 0.0) continue;

                    var peak = new PeakAreaBean() {
                        PeakID = counter,
                        RtAtPeakTop = rt,
                        AccurateMass = mz
                    };
                    peaks.Add(peak);
                    counter++;
                }
            }
            return peaks;
        }
        
        // this method is to tentatively make the alignment list from the file
        // the file should be [0] m/z [1] rt with the header row
        // the file should only contain monoisotopic ions
        public static List<AlignmentPropertyBean> GenerateTempAlignmentPropertyBeans(string filepath) {
            var peaks = new List<AlignmentPropertyBean>();
            var counter = 0;
            using (var sr = new StreamReader(filepath, Encoding.ASCII, false)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    var lineArray = line.Split('\t');
                    if (lineArray.Length < 2) continue;

                    float mz = 0.0F, rt = 0.0F;
                    float.TryParse(lineArray[0], out mz);
                    float.TryParse(lineArray[1], out rt);
                    if (mz == 0.0 || rt == 0.0) continue;

                    var peak = new AlignmentPropertyBean() {
                        AlignmentID = counter,
                        CentralRetentionTime = rt,
                        CentralAccurateMass = mz
                    };
                    peaks.Add(peak);
                    counter++;
                }
            }
            return peaks;
        }

        // this method is for the results of positive ion mode with the list from negative ion mode.
        // however, the meaning is changed if ionMode is negative.
        // moreover, this method is not applied to the identified spots
        // currently, this method is only applied to unassigned spots by peak character evaluator.
        // the reference spots, i.e. in this case negative ion spots, must be including monoisotopic ions only.
        public static void AmalgamateDifferentPolarityDataListToDetectedSpots(List<PeakAreaBean> posSpots, string negfile, 
            List<AdductDiff> adductDiffs, float rtTol, float mzTol, IonMode ionMode) {
            var negSpots = GenerateTempPeakAreaBeans(negfile);
            if (posSpots == null || posSpots.Count == 0 ||
                negSpots == null || negSpots.Count == 0) return;
            if (posSpots[0].PeakLinks == null) return;

            posSpots = posSpots.OrderBy(n => n.RtAtPeakTop).ToList();
            negSpots = negSpots.OrderBy(n => n.RtAtPeakTop).ToList();

            //initialization
            foreach (var pSpot in posSpots) {
                pSpot.AdductFromAmalgamation = new AdductIon() { FormatCheck = false };
            }
            
            foreach (var pSpot in posSpots.Where(n => n.IsotopeWeightNumber == 0)) {
                if (pSpot.PeakLinks.Count(n => n.Character == PeakLinkFeatureEnum.Adduct) > 0) continue;
                if (ionMode == IonMode.Negative && (pSpot.AdductIonName == "[M+FA-H]-" || pSpot.AdductIonName == "[M+Hac-H]-")) continue;
                var pRt = pSpot.RtAtPeakTop;
                var pMz = pSpot.AccurateMass;
                var startIndex = DataAccessLcUtility.GetScanStartIndexByRt(pRt - rtTol, negSpots);

                for (int i = startIndex; i < negSpots.Count; i++) {
                    var nRt = negSpots[i].RtAtPeakTop;
                    var nMz = negSpots[i].AccurateMass;

                    if (nRt < pRt - rtTol) continue;
                    if (nRt > pRt + rtTol) break;

                    foreach (var diff in adductDiffs) {
                        var experimentalMassDiff =
                            ionMode == IonMode.Positive
                            ? pMz - nMz
                            : nMz - pMz; // meaning nMz is changed to pMz, and vice versa

                        var theoreticalMassDiff = 
                            ionMode == IonMode.Positive
                            ? diff.Diff 
                            : -1 * diff.Diff;
                        
                        if (Math.Abs(experimentalMassDiff - theoreticalMassDiff) < mzTol) {
                            
                            if (ionMode == IonMode.Positive) {
                                pSpot.AdductFromAmalgamation = AdductIonParcer.GetAdductIonBean(diff.PosAdduct.AdductIonName);
                                //pSpot.AdductIonName = diff.PosAdduct.AdductIonName;
                                //pSpot.AdductIonXmer = diff.PosAdduct.AdductIonXmer;
                                //pSpot.AdductIonAccurateMass = (float)diff.PosAdduct.AdductIonAccurateMass;
                                //pSpot.ChargeNumber = diff.PosAdduct.ChargeNumber;
                            }
                            else {
                                pSpot.AdductFromAmalgamation = AdductIonParcer.GetAdductIonBean(diff.NegAdduct.AdductIonName);
                                //pSpot.AdductIonName = diff.NegAdduct.AdductIonName;
                                //pSpot.AdductIonXmer = diff.NegAdduct.AdductIonXmer;
                                //pSpot.AdductIonAccurateMass = (float)diff.NegAdduct.AdductIonAccurateMass;
                                //pSpot.ChargeNumber = diff.NegAdduct.ChargeNumber;
                            }

                            break;
                        }
                    }
                }
            }

            posSpots = posSpots.OrderBy(n => n.PeakID).ToList();
        }

        // this method is for the results of positive ion mode with the list from negative ion mode.
        // however, the meaning is changed if ionMode is negative.
        // moreover, this method is not applied to the identified spots
        // currently, this method is only applied to unassigned spots by peak character evaluator.
        // the reference spots, i.e. in this case negative ion spots, must be including monoisotopic ions only.
        public static void AmalgamateDifferentPolarityDataListToDetectedSpots(List<AlignmentPropertyBean> posSpots, string negfile,
            List<AdductDiff> adductDiffs, float rtTol, float mzTol, IonMode ionMode) {
            var negSpots = GenerateTempAlignmentPropertyBeans(negfile);
            if (posSpots == null || posSpots.Count == 0 ||
                negSpots == null || negSpots.Count == 0) return;
            if (posSpots[0].PeakLinks == null) return;

            posSpots = posSpots.OrderBy(n => n.CentralRetentionTime).ToList();
            negSpots = negSpots.OrderBy(n => n.CentralRetentionTime).ToList();

            // initialization
            foreach (var pSpot in posSpots) {
                pSpot.AdductIonNameFromAmalgamation = string.Empty;
            }

            foreach (var pSpot in posSpots) {
                if (pSpot.PeakLinks.Count(n => n.Character == PeakLinkFeatureEnum.Adduct) > 0) continue;

                var pRt = pSpot.CentralRetentionTime;
                var pMz = pSpot.CentralAccurateMass;
                var startIndex = DataAccessLcUtility.GetScanStartIndexByRt(pRt - rtTol, negSpots);

                for (int i = startIndex; i < negSpots.Count; i++) {
                    var nRt = negSpots[i].CentralRetentionTime;
                    var nMz = negSpots[i].CentralAccurateMass;

                    if (nRt < pRt - rtTol) continue;
                    if (nRt > pRt + rtTol) break;

                    foreach (var diff in adductDiffs) {
                        var experimentalMassDiff =
                            ionMode == IonMode.Positive
                            ? pMz - nMz
                            : nMz - pMz; // meaning nMz is changed to pMz, and vice versa

                        var theoreticalMassDiff =
                            ionMode == IonMode.Positive
                            ? diff.Diff
                            : -1 * diff.Diff;

                        if (Math.Abs(experimentalMassDiff - theoreticalMassDiff) < mzTol) {

                            if (ionMode == IonMode.Positive) {
                                pSpot.AdductIonNameFromAmalgamation = diff.PosAdduct.AdductIonName;
                                //pSpot.AdductIonName = diff.PosAdduct.AdductIonName;
                                //pSpot.ChargeNumber = diff.PosAdduct.ChargeNumber;
                            }
                            else {
                                pSpot.AdductIonNameFromAmalgamation = diff.NegAdduct.AdductIonName;
                                //pSpot.AdductIonName = diff.NegAdduct.AdductIonName;
                                //pSpot.ChargeNumber = diff.NegAdduct.ChargeNumber;
                            }

                            break;
                        }
                    }
                }
            }

            posSpots = posSpots.OrderBy(n => n.AlignmentID).ToList();
        }

        public static void AmalgamatorPrivateTest(ProjectPropertyBean projectProperty, ObservableCollection<AnalysisFileBean> files,
            AnalysisParametersBean param) {

            var adductDiffs = new List<AdductDiff>();
            adductDiffs.Add(new AdductDiff(AdductIonParcer.GetAdductIonBean("[M+H]+"), AdductIonParcer.GetAdductIonBean("[M-H]-")));
            adductDiffs.Add(new AdductDiff(AdductIonParcer.GetAdductIonBean("[M+H]+"), AdductIonParcer.GetAdductIonBean("[M+HCOOH-H]-")));
            adductDiffs.Add(new AdductDiff(AdductIonParcer.GetAdductIonBean("[M-H2O+H]+"), AdductIonParcer.GetAdductIonBean("[M+HCOOH-H]-")));
            adductDiffs.Add(new AdductDiff(AdductIonParcer.GetAdductIonBean("[M-H2O+H]+"), AdductIonParcer.GetAdductIonBean("[M-H]-")));
            adductDiffs.Add(new AdductDiff(AdductIonParcer.GetAdductIonBean("[M+NH4]+"), AdductIonParcer.GetAdductIonBean("[M-H]-")));
            adductDiffs.Add(new AdductDiff(AdductIonParcer.GetAdductIonBean("[M+NH4]+"), AdductIonParcer.GetAdductIonBean("[M+HCOOH-H]-")));
            //adductDiffs.Add(new AdductDiff(AdductIonParcer.GetAdductIonBean("[M-C6H10O5+H]+"), AdductIonParcer.GetAdductIonBean("[M-H]-")));

            var featureFiles = System.IO.Directory.GetFiles(@"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\PeakFeatures", "*.txt", SearchOption.TopDirectoryOnly);

            foreach (var file in files) {
                var filename = file.AnalysisFilePropertyBean.AnalysisFileName;
                var filenameArray = filename.Split('_');
                var featureFilePath = string.Empty;
                foreach (var fFile in featureFiles) {
                    var fFileName = System.IO.Path.GetFileNameWithoutExtension(fFile);
                    var fFileNameArray = fFileName.Split('_');

                    if (filenameArray[0] == fFileNameArray[0] && filenameArray[1] == fFileNameArray[1] && filenameArray[2] != fFileNameArray[2]) {

                        DataStorageLcUtility.SetPeakAreaBeanCollection(file, file.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
                        AmalgamateDifferentPolarityDataListToDetectedSpots(new List<PeakAreaBean>(file.PeakAreaBeanCollection), fFile, adductDiffs, 0.05F, 0.015F, projectProperty.IonMode);
                        MessagePackHandler.SaveToFile<ObservableCollection<PeakAreaBean>>(file.PeakAreaBeanCollection, file.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
                        break;
                    }
                }
            }
        }
    }
}
