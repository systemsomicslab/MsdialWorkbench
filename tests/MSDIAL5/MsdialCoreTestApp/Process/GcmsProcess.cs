using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialGcMsApi.Algorithm.Alignment;
using CompMs.MsdialGcMsApi.DataObj;
using CompMs.MsdialGcMsApi.Export;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialGcMsApi.Process;
using CompMs.MsdialIntegrate.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.Process
{
    public class GcmsProcess
    {
        public void ConvertRtToKovatRi(string alkaneDictPath, string rtListPath, string outputFilePath)
        {
            var alkaneDict = RetentionIndexHandler.GetRiDictionary(alkaneDictPath);

            using (StreamWriter sw = new StreamWriter(outputFilePath, false, Encoding.ASCII)) {
            using (var sr = new StreamReader(rtListPath, Encoding.ASCII))
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    var ri = RetentionIndexHandler.GetRetentionIndexByAlkanes(alkaneDict, float.Parse(line));

                    sw.WriteLine(ri);
                }
            }
        }

        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectStore)
        {
            var param = ConfigParser.ReadForGcms(methodFile);
            var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile);
            if (!isCorrectlyImported) return -1;
            if (param.RiDictionaryFilePath != string.Empty)
            {
                var errorMessage = string.Empty;
                if (!File.Exists(param.RiDictionaryFilePath))
                    throw new FileNotFoundException(String.Format("File '{0}' does not exist.", param.RiDictionaryFilePath));
                if (!checkRiDicionaryFiles(analysisFiles, param.RiDictionaryFilePath, out errorMessage))
                    throw new FileNotFoundException(String.Format(errorMessage, param.RiDictionaryFilePath));

                //probably, at least in fiehn lab, this has to be automatically set from GCMS raw data.
                var flgIncorrectFormat = false;
                var flgIncorrectFiehnFormat = false;

                param.FileIdRiInfoDictionary = new Dictionary<int, RiDictionaryInfo>();
                foreach (var file in analysisFiles) {
                    param.FileIdRiInfoDictionary[file.AnalysisFileId] = new RiDictionaryInfo() {
                        DictionaryFilePath = file.RiDictionaryFilePath,
                        RiDictionary = RetentionIndexHandler.GetRiDictionary(file.RiDictionaryFilePath)
                    };

                    var dictionary = param.FileIdRiInfoDictionary[file.AnalysisFileId].RiDictionary;
                    if (dictionary == null || dictionary.Count == 0) flgIncorrectFormat = true;

                    if (param.RiCompoundType == RiCompoundType.Fames) {
                        if (!isFamesContanesMatch(dictionary)) flgIncorrectFiehnFormat = true;
                    }
                }
                if (flgIncorrectFormat == true) {
                    return ridictionaryError();
                }
                if (flgIncorrectFiehnFormat == true) {
                    return famesIndexError();
                }
            }

         

            //CommonProcess.ParseLibraries(param, -1, out IupacDatabase iupacDB, out _, out var txtDB, out var isotopeTextDB, out _, out var lbmDB);

            

            //var param = ConfigParser.ReadForLcmsParameter(methodFile);
            //if (param.ProjectParam.AcquisitionType == AcquisitionType.None) param.ProjectParam.AcquisitionType = AcquisitionType.DDA;
            //var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile);
            //if (!isCorrectlyImported) return -1;

            CommonProcess.ParseLibraries(param, -1, out IupacDatabase iupacDB,
                out List<MoleculeMsReference> mspDB, out List<MoleculeMsReference> txtDB,
                out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode,
                out List<MoleculeMsReference> lbmDB);

            
            var container = new MsdialGcmsDataStorage()
            {
                AnalysisFiles = analysisFiles,
                AlignmentFiles = new List<AlignmentFileBean>() { alignmentFile },
                IsotopeTextDB = isotopeTextDB,
                IupacDatabase = iupacDB,
                MsdialGcmsParameter = param
            };


            var dbStorage = DataBaseStorage.CreateEmpty();
            if (File.Exists(param.MspFilePath))
            {
                MoleculeDataBase database = new MoleculeDataBase(mspDB, param.MspFilePath, DataBaseSource.Msp, SourceType.MspDB);
                var annotator = new MassAnnotator(database, param.MspSearchParam, param.TargetOmics, SourceType.MspDB, "MspDB", 1);
                dbStorage.AddMoleculeDataBase(database, new List<IAnnotatorParameterPair<MoleculeDataBase>> {
                new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, param.PeakPickBaseParam, param.MspSearchParam, ignoreIsotopicPeak: true)),
            });
            }
            if (File.Exists(param.TextDBFilePath))
            {
                var textdatabase = new MoleculeDataBase(txtDB, param.TextDBFilePath, DataBaseSource.Text, SourceType.TextDB);
                var textannotator = new MassAnnotator(textdatabase, param.TextDbSearchParam, param.TargetOmics, SourceType.TextDB, "TextDB", 2);
                dbStorage.AddMoleculeDataBase(textdatabase, new List<IAnnotatorParameterPair<MoleculeDataBase>> {
                new MetabolomicsAnnotatorParameterPair(textannotator.Save(), new AnnotationQueryFactory(textannotator, param.PeakPickBaseParam, param.TextDbSearchParam, ignoreIsotopicPeak: false)),
            });
            }
            container.DataBases = dbStorage;
            container.DataBaseMapper = dbStorage.CreateDataBaseMapper();
            var projectDataStorage = new ProjectDataStorage(new ProjectParameter(DateTime.Now, param.ProjectParam.ProjectFolderPath, Path.ChangeExtension(param.ProjectParam.ProjectFileName, ".mdproject")));
            projectDataStorage.AddStorage(container);
            Console.WriteLine("Start processing..");
			return Execute(projectDataStorage, container, outputFolder, isProjectStore);
		}

        private bool isFamesContanesMatch(Dictionary<int, float> riDictionary)
        {
            var fiehnFamesDictionary = RetentionIndexHandler.GetFiehnFamesDictionary();

            if (fiehnFamesDictionary.Count != riDictionary.Count) return false;
            foreach (var fFame in fiehnFamesDictionary) {
                var fiehnCnumber = fFame.Key;
                var flg = false;
                foreach (var dict in riDictionary) {
                    if (fiehnCnumber == dict.Key) {
                        flg = true;
                        break;
                    }
                }
                if (flg == false) return false;
            }
            return true;
        }


        private bool checkRiDicionaryFiles(List<AnalysisFileBean> analysisFiles, string riDictionaryFile, out string errorMessage)
        {
            errorMessage = string.Empty;
            using (var sr = new StreamReader(riDictionaryFile, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    var lineArray = line.Split('\t');
                    if (lineArray.Length < 2) continue;

                    var analysisFilePath = lineArray[0];
                    var riFilePath = lineArray[1];

                    foreach (var file in analysisFiles) {
                        if (file.AnalysisFilePath == analysisFilePath) {
                            file.RiDictionaryFilePath = riFilePath;
                            break;
                        }
                    }
                }
            }

            //check if ri file exist correctly
            foreach (var file in analysisFiles) {
                if (file.RiDictionaryFilePath.IsEmptyOrNull()) {
                    errorMessage += file.AnalysisFilePath + "\r\n";
                }
            }
            if (errorMessage != string.Empty) {
                errorMessage += "\r\n" + "The RI dictionary file of the above files is not set correctly. Set your RI dictionary file for all imported files.";
                return false;
            }
            else {
                return true;
            }
        }

        private int Execute(ProjectDataStorage projectDataStorage, MsdialGcmsDataStorage storage, string outputFolder, bool isProjectSaved)
        {
            var files = storage.AnalysisFiles;
            var exporterFactory = new AnalysisCSVExporterFactory("\t");
            var metaAccessor = new GcmsAnalysisMetadataAccessor(storage.DataBaseMapper, new DelegateMsScanPropertyLoader<SpectrumFeature>(s => s.AnnotatedMSDecResult.MSDecResult));
            var scanExporter = exporterFactory.CreateExporter(metaAccessor);
            foreach (var file in files)
            {
                FileProcess.Run(file, storage);

                var sfs = file.LoadSpectrumFeatures();
                using (var stream = File.Open(Path.Combine(outputFolder, file.AnalysisFileName + ".mdscan"), FileMode.Create, FileAccess.Write, FileShare.Read)) {
                    scanExporter.Export(stream, file, sfs.Items, new ExportStyle());
                }
            }
            if (storage.MsdialGcmsParameter.TogetherWithAlignment)
            {
                var alignmentFile = storage.AlignmentFiles.First();
                var factory = new GcmsAlignmentProcessFactory(files, storage);
                var aligner = factory.CreatePeakAligner();
                var result = aligner.Alignment(files, alignmentFile, null);
                result.Save(alignmentFile);

                var decResults = LoadRepresentativeDeconvolutions(storage, result?.AlignmentSpotProperties);
                var accessor = new GcmsAlignmentMetadataAccessor(storage.DataBaseMapper, storage.MsdialGcmsParameter, false);
                var quantAccessor = new LegacyQuantValueAccessor("Height", storage.MsdialGcmsParameter);
                var stats = new[] { StatsValue.Average, StatsValue.Stdev };
                var spotExporter = new AlignmentCSVExporter("\t");
                using (var stream = File.Open(Path.Combine(outputFolder, alignmentFile.FileName + ".mdalign"), FileMode.Create, FileAccess.Write, FileShare.Read)) {
                    spotExporter.Export(stream, result.AlignmentSpotProperties, decResults, files, new MulticlassFileMetaAccessor(0), accessor, quantAccessor, stats);
                }
            }
            if (isProjectSaved)
            {
                storage.MsdialGcmsParameter.ProjectParam.MsdialVersionNumber = "console";
                storage.MsdialGcmsParameter.ProjectParam.FinalSavedDate = DateTime.Now;
                using (var stream = File.Open(projectDataStorage.ProjectParameter.FilePath, FileMode.Create))
                using (var streamManager = new ZipStreamManager(stream, System.IO.Compression.ZipArchiveMode.Create))
                {
                    projectDataStorage.Save(streamManager, new MsdialIntegrateSerializer(), file => new DirectoryTreeStreamManager(file), parameter => Console.WriteLine($"Save {parameter.ProjectFileName} failed")).Wait();
                    ((IStreamManager)streamManager).Complete();
                }
            }
            return 0;
        }

        private static List<MSDecResult> LoadRepresentativeDeconvolutions(IMsdialDataStorage<MsdialGcmsParameter> storage, IReadOnlyList<AlignmentSpotProperty> spots) {
            var files = storage.AnalysisFiles;

            var pointerss = new List<(int version, List<long> pointers, bool isAnnotationInfo)>();
            foreach (var file in files) {
                MsdecResultsReader.GetSeekPointers(file.DeconvolutionFilePath, out var version, out var pointers, out var isAnnotationInfo);
                pointerss.Add((version, pointers, isAnnotationInfo));
            }

            var streams = new List<FileStream>();
            var decs = new List<MSDecResult>();
            try {
                streams = files.Select(file => File.Open(file.DeconvolutionFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)).ToList();
                foreach (var spot in spots.OrEmptyIfNull()) {
                    var repID = spot.RepresentativeFileID;
                    var peakID = spot.AlignedPeakProperties[repID].MasterPeakID;
                    var decResult = MsdecResultsReader.ReadMSDecResult(
                        streams[repID], pointerss[repID].pointers[peakID],
                        pointerss[repID].version, pointerss[repID].isAnnotationInfo);
                    decs.Add(decResult);
                }
            }
            finally {
                streams.ForEach(stream => stream.Close());
            }
            return decs;
        }

#region // error code
        private int ridictionaryError()
        {
            string error = "Invalid RI information. Please check that your file follows the following format.\r\n";
            error += "Carbon number\tRT(min)\r\n";
            error += "10\t4.72\r\n";
            error += "11\t5.63\r\n";
            error += "12\t6.81\r\n";
            error += "13\t8.08\r\n";
            error += "14\t9.12\r\n";
            error += "15\t10.33\r\n";
            error += "16\t11.91\r\n";
            error += "18\t14.01\r\n";
            error += "20\t16.15\r\n";
            error += "22\t18.28\r\n";
            error += "24\t20.33\r\n";
            error += "26\t22.17\r\n";
            error += "\r\n";
            error += "This information is required for RI calculation.";

            Console.WriteLine(error);
            return -1;
        }

        private int famesIndexError()
        {
            var error = "If you use the FAMEs RI, you have to decide the retention times as minute for \r\n"
                            + "C8, C9, C10, C12, C14, C16, C18, C20, C22, C24, C26, C28, C30.";
            Console.WriteLine(error);
            return -1;
        }
#endregion
    }
}
