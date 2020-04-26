using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Resources;

namespace Msdial.Lcms.Dataprocess.Utility
{
    public sealed class DatabaseLcUtility
    {
        private DatabaseLcUtility() { }

        public static List<MspFormatCompoundInformationBean> GetMspDbQueries(string filePath, IupacReferenceBean iupac)
        {
            var mspQueries = MspFileParcer.MspFileReader(filePath);

            SetTheoreticalIsotopeRaio(mspQueries, iupac);

            return mspQueries;
        }

        public static List<PostIdentificatioinReferenceBean> GetTxtDbQueries(string filePath)
        {
            var error = string.Empty;
            var postIdentificationReferenceBeanList = new List<PostIdentificatioinReferenceBean>();
            var textFormatCompoundInformationBeanList = TextLibraryParcer.TextLibraryReader(filePath, out error);
            if (error != string.Empty) {
                Console.WriteLine(error);
            }

            if (textFormatCompoundInformationBeanList == null) return null;

            PostIdentificatioinReferenceBean postIdentificationReferenceBean;
            for (int i = 0; i < textFormatCompoundInformationBeanList.Count; i++)
            {
                postIdentificationReferenceBean = new PostIdentificatioinReferenceBean() {
                    ReferenceID = textFormatCompoundInformationBeanList[i].ReferenceId,
                    MetaboliteName = textFormatCompoundInformationBeanList[i].MetaboliteName,
                    RetentionTime = textFormatCompoundInformationBeanList[i].RetentionTime,
                    AccurateMass = textFormatCompoundInformationBeanList[i].AccurateMass,
                    AdductIon = textFormatCompoundInformationBeanList[i].AdductIon,
                    Formula = textFormatCompoundInformationBeanList[i].Formula,
                    Inchikey = textFormatCompoundInformationBeanList[i].Inchikey,
                    Smiles = textFormatCompoundInformationBeanList[i].Smiles,
                    Ontology = textFormatCompoundInformationBeanList[i].Ontology,
                    Ccs = textFormatCompoundInformationBeanList[i].Ccs
                };
                postIdentificationReferenceBeanList.Add(postIdentificationReferenceBean);
            }

            return postIdentificationReferenceBeanList;
        }

        public static List<PostIdentificatioinReferenceBean> GetTxtFormulaDbQueries(string filePath)
        {
            var targetFormulaLibrary = new List<PostIdentificatioinReferenceBean>();
            var error = string.Empty;
            var textFormulaDB = TextLibraryParcer.TargetFormulaLibraryReader(filePath, out error);
            if (error != string.Empty) {
                Console.WriteLine(error);
            }

            if (textFormulaDB == null) return null;

            for (int i = 0; i < textFormulaDB.Count; i++)
            {
                var txtFormulaQuery = new PostIdentificatioinReferenceBean() {
                    ReferenceID = textFormulaDB[i].ReferenceId,
                    MetaboliteName = textFormulaDB[i].MetaboliteName,
                    RetentionTime = textFormulaDB[i].RetentionTime,
                    AccurateMass = textFormulaDB[i].AccurateMass,
                    AdductIon = textFormulaDB[i].AdductIon,
                    Formula = textFormulaDB[i].Formula,
                    Inchikey = textFormulaDB[i].Inchikey,
                    Smiles = textFormulaDB[i].Smiles,
                    Ontology = textFormulaDB[i].Ontology,
                    Ccs = textFormulaDB[i].Ccs
                };
                targetFormulaLibrary.Add(txtFormulaQuery);
            }

            return targetFormulaLibrary;
        }

        public async Task<List<MspFormatCompoundInformationBean>> GetMspDbQueriesAsync(string filePath, IupacReferenceBean iupac, LipidQueryBean lipidQueryBean) {
            return await Task.Run(() => GetMspDbQueries(filePath, iupac, lipidQueryBean));
        }

        public static List<MspFormatCompoundInformationBean> GetMspDbQueries(string filePath, IupacReferenceBean iupac, LipidQueryBean lipidQueryBean)
        {
            var collosionType = lipidQueryBean.CollisionType;
            var solventType = lipidQueryBean.SolventType;
            var ionMode = lipidQueryBean.IonMode;
            var queries = new List<LbmQuery>();

            foreach (var lQuery in lipidQueryBean.LbmQueries) {
                if (lQuery.IsSelected == true && lQuery.IonMode == ionMode)
                    queries.Add(lQuery);
            }

            List<MspFormatCompoundInformationBean> mspQueries = null;
            var extension = System.IO.Path.GetExtension(filePath);
            if (extension == ".lbm")
                mspQueries = LbmFileParcer.Read(filePath, queries, ionMode, solventType, collosionType);
            else if (extension == ".lbm2")
                mspQueries = LbmFileParcer.ReadSerializedObjectLibrary(filePath, queries, ionMode, solventType, collosionType);

            SetTheoreticalIsotopeRaio(mspQueries, iupac);

            return mspQueries;
        }

        public static IupacReferenceBean GetIupacReferenceBean(string filepath)
        {
            int iupacID = 0;
            string elementName = "";
            string line;
            string[] lineArray;
           
            var iupacReference = new IupacReferenceBean();
            var iupacElements = new List<IupacElementPropertyBean>();
            var iupacElement = new IupacElementPropertyBean();

            using (var sr = new StreamReader(filepath))
            {
                sr.ReadLine();
                while (sr.Peek() > -1)
                {
                    line = sr.ReadLine();
                    if (line == string.Empty) break;

                    lineArray = line.Split('\t');

                    if (iupacID != int.Parse(lineArray[0]))
                    {
                        if (iupacID != 0) { iupacReference.IupacID_IupacElementPropertyBeanList[iupacID] = iupacElements; iupacReference.ElementName_IupacElementPropertyBeanList[elementName] = iupacElements; }

                        iupacElements = new List<IupacElementPropertyBean>();
                        iupacID = int.Parse(lineArray[0]);
                        elementName = lineArray[1];

                        iupacElement = new IupacElementPropertyBean();
                        iupacElement.AccurateMass = double.Parse(lineArray[4]);
                        iupacElement.ElementName = elementName;
                        iupacElement.IupacID = iupacID;
                        iupacElement.NaturalRelativeAbundance = double.Parse(lineArray[3]);
                        iupacElement.NominalMass = int.Parse(lineArray[2]);

                        iupacElements.Add(iupacElement);
                    }
                    else
                    {
                        iupacElement = new IupacElementPropertyBean();
                        iupacElement.AccurateMass = double.Parse(lineArray[4]);
                        iupacElement.ElementName = elementName;
                        iupacElement.IupacID = iupacID;
                        iupacElement.NaturalRelativeAbundance = double.Parse(lineArray[3]);
                        iupacElement.NominalMass = int.Parse(lineArray[2]);

                        iupacElements.Add(iupacElement);
                    }
                }
                //reminder
                iupacReference.IupacID_IupacElementPropertyBeanList[iupacID] = iupacElements;
                iupacReference.ElementName_IupacElementPropertyBeanList[elementName] = iupacElements;
            }

            return iupacReference;
        }

        private static void SetTheoreticalIsotopeRaio(List<MspFormatCompoundInformationBean> mspQueries, IupacReferenceBean iupac)
        {
            if (mspQueries == null || mspQueries.Count == 0) return;
            var compoundProperty = new CompoundPropertyBean();
            string formula;
            for (int i = 0; i < mspQueries.Count; i++)
            {
                formula = mspQueries[i].Formula;
                if (formula == null || formula == string.Empty) continue;

                compoundProperty = IsotopeRatioCalculator.GetNominalIsotopeProperty(formula, 5, iupac);
                if (compoundProperty == null) continue;

                for (int j = 0; j < compoundProperty.IsotopeProfile.Count; j++)
                    mspQueries[i].IsotopeRatioList.Add((float)compoundProperty.IsotopeProfile[j].RelativeAbundance);
            }
        }

        public static void GetLipoqlualityDatabase(string input, out List<AnalysisFileBean> files, out List<AlignmentPropertyBean> alignedSpots, out List<RawData> rawDataQueries) {
            alignedSpots = new List<AlignmentPropertyBean>();
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                var firstHeader = sr.ReadLine();
                var secondHeader = sr.ReadLine();
                var thirdHeader = sr.ReadLine();
                var fourthHeader = sr.ReadLine();
                // [0] master ID (string) [1] alignment ID [2] RT [3] m/z [4] Metabolite name 
                // [5] Adduct [6] post curation result [7] Fill% [8] MS/MS included [9] Formula 
                // [10] Ontology [11] InChIKey [12] SMILES [13] Comments [14] S/N
                // [15] spec reference file name [16] spec reference file class [17] ms1 spec [18] ms2spec [19...] ion abundances 

                files = getAnalysisFileProperties(firstHeader, secondHeader, thirdHeader, fourthHeader, 19);
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var lineArray = line.Split('\t');
                    var alignedProp = getInitializedAlignmentProp(lineArray);
                    setAlignmentPeakProperties(alignedProp, lineArray, files, 19);
                    alignedSpots.Add(alignedProp);
                }
            }

            rawDataQueries = new List<RawData>();
            foreach (var spot in alignedSpots) {
                var rawData = new RawData() {
                    Name = spot.MasterIdString,
                    MetaboliteName = spot.MetaboliteName,
                    Formula = spot.Formula == null ? string.Empty : spot.Formula.FormulaString,
                    Comment = spot.Comment,
                    InchiKey = spot.Inchikey,
                    Smiles = spot.Smiles,
                    Ontology = spot.Ontology,
                    PrecursorMz = spot.CentralAccurateMass,
                    RetentionTime = spot.CentralRetentionTime,
                    PrecursorType = spot.Adduct.AdductIonName
                };
                var ms1Spec = TextToSpectrumList(spot.Ms1SpecString, ':', ' ', 20);
                var ms2Spec = TextToSpectrumList(spot.Ms2SpecString, ':', ' ', 20);
                rawData.Ms1PeakNumber = ms1Spec.Count;
                rawData.Ms1Spectrum = new Spectrum() {
                    PrecursorMz = rawData.PrecursorMz,
                    PeakList = ms1Spec
                };
                rawData.Ms2PeakNumber = ms2Spec.Count;
                rawData.Ms2Spectrum = new Spectrum() {
                    PrecursorMz = rawData.PrecursorMz,
                    PeakList = ms2Spec
                };
                rawDataQueries.Add(rawData);
            }
            rawDataQueries = rawDataQueries.OrderBy(n => n.PrecursorMz).ToList();
        }

        public static List<Peak> TextToSpectrumList(string specText, char mzIntSep, char peakSep, double threshold = 0.0) {
            var peaks = new List<Peak>();

            if (specText == null || specText == string.Empty) return peaks;
            var specArray = specText.Split(peakSep).ToList();
            if (specArray.Count == 0) return peaks;

            foreach (var spec in specArray) {
                var mzInt = spec.Split(mzIntSep).ToArray();
                if (mzInt.Length >= 2) {
                    var mz = mzInt[0];
                    var intensity = mzInt[1];

                    var peak = new Peak() { Mz = double.Parse(mz), Intensity = double.Parse(intensity) };
                    if (peak.Intensity > threshold) {
                        peaks.Add(peak);
                    }
                }
            }

            return peaks;
        }

        private static void setAlignmentPeakProperties(AlignmentPropertyBean alignedProp, string[] lineArray, List<AnalysisFileBean> files, int startColumn) {
            alignedProp.AlignedPeakPropertyBeanCollection = new System.Collections.ObjectModel.ObservableCollection<AlignedPeakPropertyBean>();
            for (int i = 0; i < files.Count; i++) {
                var fileProp = files[i].AnalysisFilePropertyBean;
                var peakProperty = new AlignedPeakPropertyBean() {
                    FileID = fileProp.AnalysisFileId,
                    FileName = fileProp.AnalysisFileName,
                    Variable = float.Parse(lineArray[i + startColumn])
                };
                alignedProp.AlignedPeakPropertyBeanCollection.Add(peakProperty);
            }
        }

        private static AlignmentPropertyBean getInitializedAlignmentProp(string[] lineArray) {
            var alignmentProp = new AlignmentPropertyBean() {
                MasterIdString = lineArray[0],
                AlignmentID = int.Parse(lineArray[1]),
                CentralRetentionTime = float.Parse(lineArray[2]),
                CentralAccurateMass = float.Parse(lineArray[3]),
                MetaboliteName = lineArray[4],
                Adduct = AdductIonParcer.GetAdductIonBean(lineArray[5]),
                PostCurationResult = lineArray[6],
                FillParcentage = float.Parse(lineArray[7]),
                MsmsIncluded = bool.Parse(lineArray[8]),
                Formula = lineArray[9] == "No record" ? null : FormulaStringParcer.OrganicElementsReader(lineArray[9]),
                Ontology = lineArray[10] == "No record" ? string.Empty : lineArray[10],
                Inchikey = lineArray[11] == "No record" ? string.Empty : lineArray[11],
                Smiles = lineArray[12] == "No record" ? string.Empty : lineArray[12],
                Comment = lineArray[13],
                SignalToNoiseAve = float.Parse(lineArray[14]),
                SpectrumReferenceFileName = lineArray[15],
                SpectrumReferenceFileClass = lineArray[16],
                Ms1SpecString = lineArray[17],
                Ms2SpecString = lineArray[18]
            };
            return alignmentProp;
        }

        private static List<AnalysisFileBean> getAnalysisFileProperties(string firstHeader, string secondHeader, string thirdHeader, string fourthHeader, int startColumn) {
            var classArray = firstHeader.Split('\t');
            var typeArray = secondHeader.Split('\t');
            var injectionArray = thirdHeader.Split('\t');
            var filenameArray = fourthHeader.Split('\t');

            var fileCount = 0;
            for (int i = startColumn; i < classArray.Length; i++) {
                var className = classArray[i];
                if (className == "" || className == string.Empty) break;
                fileCount++;
            }

            var fileProps = new List<AnalysisFileBean>();
            var counter = 0;
            for (int i = startColumn; i < startColumn + fileCount; i++) {

                var classString = classArray[i];
                var superClassString = classArray[i].Contains("Blank") || classArray[i].Contains("blank") ? classArray[i] : classString.Split('_')[0] + "_" + classString.Split('_')[1];

                var file = new AnalysisFileBean() {
                    AnalysisFilePropertyBean = new AnalysisFilePropertyBean() {
                        AnalysisFileClass = classArray[i],
                        AnalysisFileSuperClass = superClassString,
                        AnalysisFileAnalyticalOrder = int.Parse(injectionArray[i]),
                        AnalysisFileId = counter,
                        AnalysisFileIncluded = true,
                        AnalysisFileName = filenameArray[i],
                        AnalysisFileType = (AnalysisFileType)Enum.Parse(typeof(AnalysisFileType), typeArray[i], true)
                    }
                };
                fileProps.Add(file);
                counter++;
            }
            
            return fileProps;
        }

        public static string GetLipoqualityDatabaseURL(MspFormatCompoundInformationBean query) {
            return Riken.Metabolomics.Lipidomics.LipidomicsConverter.GetLipoqualityDatabaseLinkUrl(query);
        }
    }
}
