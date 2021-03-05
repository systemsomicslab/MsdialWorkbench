using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.Query;
using Riken.Metabolomics.MsfinderCommon.Utility;
using Riken.Metabolomics.StructureFinder.Parser;
using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.MsfinderConsoleApp.Process {
    public class SpectrumCuration {

        private string outputFormulaPath;
        private string outputStructurePath;
        private ObservableCollection<MsfinderQueryFile> queryFiles;
        private AnalysisParamOfMsfinder param;
        private List<AdductIon> adductPositiveResources;
        private List<AdductIon> adductNegativeResources;
        //private List<Formula> quickFormulaDB;
        private List<ProductIon> productIonDB;
        private List<NeutralLoss> neutralLossDB;
        private List<FragmentOntology> fragmentOntologyDB;
        private List<ExistFormulaQuery> existFormulaDB;
        private List<ExistStructureQuery> existStructureDB;
        private List<ExistStructureQuery> userDefinedStructureDB;
        private List<ExistStructureQuery> mineStructureDB;
        private List<FragmentLibrary> eiFragmentDB;
        private List<ChemicalOntology> chemicalOntologies;

        public void CombineDuplicatesBasedOnMolecularFormulaAssignment(string input, string output) {

            this.param = new AnalysisParamOfMsfinder() {
                Mass1Tolerance = 0.01,
                Mass2Tolerance = 0.025,
                IsUseUserDefinedSpectralDb = true,
                IsUseInternalExperimentalSpectralDb = false,
                UserDefinedSpectralDbFilePath = input
            };
            var errorMessage = string.Empty;
            var storages = FileStorageUtility.GetMspDB(this.param, out errorMessage);
            if (errorMessage != string.Empty)
                Console.WriteLine(errorMessage);
            var cStorages = new List<MspFormatCompoundInformationBean>();
            //cut less than 1% peaks and changed to relative abundances
            foreach (var storage in storages) {
                if (storage.InchiKey == string.Empty) continue;
                var peaks = storage.MzIntensityCommentBeanList;
                if (peaks.Count == 0 || peaks.Count == 1) continue;

                var nPeaks = new List<MzIntensityCommentBean>();
                var peakMax = peaks.Max(n => n.Intensity);
                foreach (var peak in peaks) {
                    if (peak.Intensity < peakMax * 0.01) continue;
                    nPeaks.Add(new MzIntensityCommentBean() { Mz = peak.Mz, Intensity = (float)Math.Round(peak.Intensity / peakMax * 1000.0, 2) });
                }
                storage.MzIntensityCommentBeanList = nPeaks;
                storage.PeakNumber = nPeaks.Count;
                cStorages.Add(storage);
            }

            cStorages = cStorages.OrderBy(n => n.InchiKey.Split('-')[0]).ThenBy(n => n.AdductIonBean.AdductIonName).ToList();

            var mergedMsps = new List<MspFormatCompoundInformationBean>() { cStorages[0] };
            for (int i = 1; i < cStorages.Count; i++) {
                var storage = cStorages[i];
                var inchikey = storage.InchiKey.Split('-')[0];
                var adduct = storage.AdductIonBean.AdductIonName;

                //if (inchikey == "USNPULRDBDVJAO") {
                //    Console.WriteLine();
                //}

                if (!FormulaStringParcer.IsOrganicFormula(storage.Formula)) continue;

                var formula = FormulaStringParcer.OrganicElementsReader(storage.Formula);
                //if (adduct != "[M-H]-" && adduct != "[M+H]+" && adduct != "[M-2H]-" && adduct != "[M]+") continue;
                if (adduct != "[M-H]-" && adduct != "[M+H]+") continue;
                //if (!MolecularFormulaUtility.IsFormulaCHNOSP(formula)) continue;

                var lastStorage = mergedMsps[mergedMsps.Count - 1];
                var lastInChIKey = lastStorage.InchiKey.Split('-')[0];
                var lastAdduct = lastStorage.AdductIonBean.AdductIonName;

                if (adduct == lastAdduct && inchikey == lastInChIKey) {
                    foreach (var peak in storage.MzIntensityCommentBeanList) {
                        lastStorage.MzIntensityCommentBeanList.Add(peak);
                    }
                }
                else {
                    mergedMsps.Add(storage);
                }
            }

            var rawdatas = new List<Rfx.Riken.OsakaUniv.RawData>();
            foreach (var storage in mergedMsps) {
                var rawdata = new Rfx.Riken.OsakaUniv.RawData() {
                    Name = storage.InchiKey.Split('-')[0] + "_" + storage.AdductIonBean.AdductIonName + "_" + storage.IonMode,
                    PrecursorMz = storage.PrecursorMz,
                    PrecursorType = storage.AdductIonBean.AdductIonName,
                    RetentionTime = storage.RetentionTime,
                    Ccs = storage.CollisionCrossSection,
                    Formula = storage.Formula,
                    Ontology = storage.Ontology,
                    InchiKey = storage.InchiKey,
                    Smiles = storage.Smiles,
                    IonMode = storage.IonMode,
                    Comment = storage.Comment,
                    NominalIsotopicPeakList = new List<Peak>() {
                        new Peak() { Mz = storage.PrecursorMz, Intensity = 100 },
                        new Peak() { Mz = storage.PrecursorMz + 1, Intensity = 0 },
                        new Peak() { Mz = storage.PrecursorMz + 2, Intensity = 0 }
                    }
                };

                //fixing formula
                var formula = FormulaStringParcer.OrganicElementsReader(rawdata.Formula);
                rawdata.CarbonNumberFromLabeledExperiment = formula.Cnum;
                rawdata.NitrogenNumberFromLabeledExperiment = formula.Nnum;
                rawdata.OxygenNumberFromLabeledExperiment = formula.Onum;
                rawdata.SulfurNumberFromLabeledExperiment = formula.Snum;
                rawdata.Ms2Spectrum.PeakList = new List<Peak>();
                foreach (var peak in storage.MzIntensityCommentBeanList.OrderBy(n => n.Mz)) {
                    rawdata.Ms2Spectrum.PeakList.Add(new Peak() { Mz = peak.Mz, Intensity = peak.Intensity });
                }
                rawdata.Ms2PeakNumber = rawdata.Ms2Spectrum.PeakList.Count;
                rawdatas.Add(rawdata);
            }

           

            Console.WriteLine("Preparing workspaces");
            Parallel.Invoke(
            #region invoke
            () => {
                //this.quickFormulaDB = FileStorageUtility.GetKyusyuUnivFormulaDB(this.param);
            },
            () => {
                this.neutralLossDB = FileStorageUtility.GetNeutralLossDB();
                this.productIonDB = FileStorageUtility.GetProductIonDB();
                this.fragmentOntologyDB = FileStorageUtility.GetUniqueFragmentDB();
                this.chemicalOntologies = FileStorageUtility.GetChemicalOntologyDB();

                if (this.fragmentOntologyDB != null && this.productIonDB != null)
                    ChemOntologyDbParser.ConvertInChIKeyToChemicalOntology(this.productIonDB, this.fragmentOntologyDB);

                if (this.fragmentOntologyDB != null && this.neutralLossDB != null)
                    ChemOntologyDbParser.ConvertInChIKeyToChemicalOntology(this.neutralLossDB, this.fragmentOntologyDB);

                if (this.fragmentOntologyDB != null && this.chemicalOntologies != null)
                    ChemOntologyDbParser.ConvertInChIKeyToChemicalOntology(this.chemicalOntologies, this.fragmentOntologyDB);
            },
            () => {
                this.existFormulaDB = FileStorageUtility.GetExistFormulaDB();
            }
            //,
            //() => {
            //    MoleculeImage.TryClassLoad();
            //}
            #endregion
            );

            var counter = 0;
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                foreach (var rawdata in rawdatas) {
                    counter++;
                    //if (rawdata.InchiKey.Split('-')[0] == "USNPULRDBDVJAO") {
                    //    Console.WriteLine();
                    //}

                    var formulaResult = MolecularFormulaFinder.GetMolecularFormulaList(this.productIonDB, this.neutralLossDB, this.existFormulaDB, rawdata, this.param);
                   
                    //var formulaResult = MolecularFormulaFinder.GetMolecularFormulaList(this.quickFormulaDB, this.productIonDB,
                    //    this.neutralLossDB, this.existFormulaDB, rawdata, this.param);

                    foreach (var result in formulaResult) {
                        if (MolecularFormulaUtility.isFormulaMatch(result.Formula,
                            FormulaStringParcer.OrganicElementsReader(rawdata.Formula))) {

                            var productions = result.ProductIonResult;
                            if (productions != null && productions.Count != 0) {
                                productions = productions.OrderBy(n => n.Formula.FormulaString).ToList();
                                var curatedProductIons = new List<ProductIon>() { productions[0] };
                                if (productions.Count >= 2) {
                                    for (int i = 1; i < productions.Count; i++) {
                                        var cIon = curatedProductIons[curatedProductIons.Count - 1];
                                        var pIon = productions[i];

                                        if (MolecularFormulaUtility.isFormulaMatch(cIon.Formula, pIon.Formula)) {
                                            if (Math.Abs(cIon.MassDiff) > Math.Abs(pIon.MassDiff)) {
                                                cIon.Intensity = pIon.Intensity;
                                            }
                                        }
                                        else {
                                            curatedProductIons.Add(pIon);
                                        }
                                    }
                                }
                                writeMspQuery(sw, rawdata, curatedProductIons);
                            }
                            break;
                        }
                    }
                    if (!Console.IsOutputRedirected) {
                        Console.Write("Writing result finished: {0} / {1}", counter, rawdatas.Count);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    else {
                        Console.WriteLine("Writing result finished: {0} / {1}", counter, rawdatas.Count);
                    }
                }
            }
        }

        private void writeMspQuery(StreamWriter sw, Rfx.Riken.OsakaUniv.RawData rawdata, 
            List<ProductIon> curatedProductIons) {
            sw.WriteLine("NAME: " + rawdata.Name);
            sw.WriteLine("PRECURSORMZ: " + rawdata.PrecursorMz);
            sw.WriteLine("PRECURSORTYPE: " + rawdata.PrecursorType);
            sw.WriteLine("FORMULA: " + rawdata.Formula);
            sw.WriteLine("ONTOLOGY: " + rawdata.Ontology);
            sw.WriteLine("SMILES: " + rawdata.Smiles);
            sw.WriteLine("INCHIKEY: " + rawdata.InchiKey);
            sw.WriteLine("IONMODE: " + rawdata.IonMode);
            sw.WriteLine("RETENTIONTIME: " + rawdata.RetentionTime);
            sw.WriteLine("CCS: " + rawdata.Ccs);
            sw.WriteLine("Comment: " + rawdata.Comment);
            sw.WriteLine("Num Peaks: " + curatedProductIons.Count);

            curatedProductIons = curatedProductIons.OrderBy(n => n.Mass).ToList();
            foreach (var peak in curatedProductIons) {
                sw.WriteLine(Math.Round(peak.Mass, 5) + "\t" + Math.Round(peak.Intensity, 0));
            }
            sw.WriteLine();
        }
    }
}
