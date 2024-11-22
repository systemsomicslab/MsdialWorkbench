using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using CompMs.MsdialImmsCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialIntegrate.Parser;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.App.MsdialConsole.Process
{
    public sealed class MaldiMsProcessTest {
        private MaldiMsProcessTest() { }

        public static void Run() {
            //var filepath = @"E:\6_Projects\PROJECT_AHexCer\spatial_lipidomics\sagital_pos\20221023_TIMS_Brain_DHAP_20um_Pos.d";
            //var reffile = @"E:\6_Projects\PROJECT_ImagingMS\Lipid reference library\20220725_timsTOFpro_TextLibrary_Brain_Pos.txt";
            //var outputfile = @"E:\6_Projects\PROJECT_AHexCer\spatial_lipidomics\sagital_pos\20221023_TIMS_Brain_DHAP_20um_Pos.mddata";
            var filepath = @"E:\6_Projects\PROJECT_ImagingMS\20211005_Bruker_timsTOFfleX-selected\Eye_Neg\20211005_Eye_Acsl_HZ_KO_Neg\20211005_Eye_Acsl_HZ_KO_Neg.d";
            var reffile = @"E:\6_Projects\PROJECT_ImagingMS\Lipid reference library\20220725_timsTOFpro_TextLibrary_Eye_Neg.txt";
            var outputfile = @"E:\6_Projects\PROJECT_ImagingMS\20211005_Bruker_timsTOFfleX-selected\Eye_Neg\20211005_Eye_Acsl_HZ_KO_Neg\20211005_Eye_Acsl_HZ_KO_Neg.mddata";

            var filename = Path.GetFileNameWithoutExtension(filepath);
            var fileDir = Path.GetDirectoryName(filepath)!;
            //var projectParameter = new ProjectParameter(DateTime.Now, @"E:\6_Projects\PROJECT_AHexCer\spatial_lipidomics\sagital_pos\", "20221023_TIMS_Brain_DHAP_20um_Pos.mdproject");
            var projectParameter = new ProjectParameter(DateTime.Now, @"E:\6_Projects\PROJECT_ImagingMS\20211005_Bruker_timsTOFfleX-selected\Eye_Neg\20211005_Eye_Acsl_HZ_KO_Neg\", "20211005_Eye_Acsl_HZ_KO_Neg.mdproject");

            var storage = new ProjectDataStorage(projectParameter);
            var file = new AnalysisFileBean() {
                AnalysisFileId = 0,
                AnalysisFileIncluded = true,
                AnalysisFileName = filename,
                AnalysisFilePath = filepath,
                AnalysisFileAnalyticalOrder = 1,
                AnalysisFileClass = "0",
                AnalysisFileType = AnalysisFileType.Sample,
                DeconvolutionFilePath = Path.Combine(fileDir, filename + "_test230127" + ".dcl"),
                PeakAreaBeanInformationFilePath = Path.Combine(fileDir, filename + "_test230127" + ".pai"),
            };

            var param = new MsdialImmsParameter(isImaging: true, isLabUseOnly: true) {
                ProjectFolderPath = Path.GetDirectoryName(outputfile)!,
                ProjectFileName = Path.GetFileName(outputfile),
                MachineCategory = MachineCategory.IIMMS,
                TextDBFilePath = reffile,

                //IonMode = IonMode.Positive,
                IonMode = IonMode.Negative,
                MinimumAmplitude = 100000,
                FileID2CcsCoefficients = new Dictionary<int, CoefficientsForCcsCalculation>() {
                    { 0, new CoefficientsForCcsCalculation() { IsBrukerIM = true } }
                }
            };
            param.TextDbSearchParam.CcsTolerance = 20.0F;
            param.TextDbSearchParam.IsUseCcsForAnnotationFiltering = true;

            RawMeasurement? rawobj = null;
            using (var access = new RawDataAccess(filepath, 0, getProfileData: false, isImagingMsData: true, isGuiProcess: false)) {
                rawobj = access.GetMeasurement();
            }
            var providerFactory = new StandardDataProviderFactory().ContraMap((AnalysisFileBean f) => {
                using var access = new RawDataAccess(f.AnalysisFilePath, 0, getProfileData: false, isImagingMsData: true, isGuiProcess: false);
                return access.GetMeasurement();
            });
            var provider = new StandardDataProviderFactory().Create(rawobj);

            var db = DataBaseStorage.CreateEmpty();
            var tdb = new MoleculeDataBase(TextLibraryParser.TextLibraryReader(param.TextDBFilePath, out string error), "TextDB", DataBaseSource.Text, SourceType.TextDB);
            var textDBAnnotator = new ImmsTextDBAnnotator(tdb, param.TextDbSearchParam, "TextDB", -1);
            db.AddMoleculeDataBase(
                tdb,
                [
                    new MetabolomicsAnnotatorParameterPair(textDBAnnotator.Save(), new AnnotationQueryFactory(textDBAnnotator, param.PeakPickBaseParam, param.TextDbSearchParam, ignoreIsotopicPeak: false))
                ]);
            //var evaluator = FacadeMatchResultEvaluator.FromDataBases(db);

            var container = new MsdialImmsDataStorage {
                AnalysisFiles = [file],
                AlignmentFiles = [],
                MsdialImmsParameter = param,
                IupacDatabase = IupacResourceParser.GetIUPACDatabase(),
                DataBases = db,
                DataBaseMapper = db.CreateDataBaseMapper()
            };
            storage.AddStorage(container);

            var matchResultEvaluator = new MsScanMatchResultEvaluator(param.TextDbSearchParam);

            var processor = new MsdialImmsImagingCore.Process.FileProcess(container, providerFactory, null, null, matchResultEvaluator);
            processor.RunAsyncTest(file, provider).Wait();

            using var fs = new TemporaryFileStream(storage.ProjectParameter.FilePath);
            using (IStreamManager streamManager = ZipStreamManager.OpenCreate(fs)) {
                var serializer = new MsdialIntegrateSerializer();
                storage.Save(
                streamManager,
                serializer,
                path => new DirectoryTreeStreamManager(path),
                parameter => { }).Wait();
                streamManager.Complete();
            }
            fs.Move();
        }

        public static void TimsOnTest() {
            var filepath = @"E:\6_Projects\PROJECT_ImagingMS\EYE Project\ImagingMS\ROI_20220208_Eye_Acsl6Hz_20um_DCTB_Pos\20220208_Eye_Acsl6Hz_20um_DCTB_Pos.d";
            var reffile = @"E:\6_Projects\PROJECT_ImagingMS\Lipid reference library\20220725_timsTOFpro_TextLibrary_Eye_Pos.txt";
            var outputfile = @"E:\6_Projects\PROJECT_ImagingMS\EYE Project\ImagingMS\ROI_20220208_Eye_Acsl6Hz_20um_DCTB_Pos\20220208_Eye_Acsl6Hz_20um_DCTB_Pos.mddata";
            var filename = Path.GetFileNameWithoutExtension(filepath);
            var fileDir = Path.GetDirectoryName(filepath)!;
            var projectParameter = new ProjectParameter(DateTime.Now, @"E:\6_Projects\PROJECT_ImagingMS\EYE Project\ImagingMS\ROI_20220208_Eye_Acsl6Hz_20um_DCTB_Pos\", "20211004_Acsl6_leftHZ_rightKO_Eye.mdproject");
            var storage = new ProjectDataStorage(projectParameter);
            var file = new AnalysisFileBean() {
                AnalysisFileId = 0,
                AnalysisFileIncluded = true,
                AnalysisFileName = filename,
                AnalysisFilePath = filepath,
                AnalysisFileAnalyticalOrder = 1,
                AnalysisFileClass = "0",
                AnalysisFileType = AnalysisFileType.Sample,
                DeconvolutionFilePath = Path.Combine(fileDir, filename + "_221109" + ".dcl"),
                PeakAreaBeanInformationFilePath = Path.Combine(fileDir, filename + "_221109" + ".pai"),
            };

            var param = new MsdialImmsParameter() {
                ProjectFolderPath = Path.GetDirectoryName(outputfile)!,
                ProjectFileName = Path.GetFileName(outputfile),
                MachineCategory = MachineCategory.IMMS,
                TextDBFilePath = reffile,
                IonMode = IonMode.Positive,
                MinimumAmplitude = 1000,
                FileID2CcsCoefficients = new Dictionary<int, CoefficientsForCcsCalculation>() {
                    { 0, new CoefficientsForCcsCalculation() { IsBrukerIM = true } }
                }
            };

            CommonProcess.ParseLibraries(param, -1, 
                out var iupacDB, 
                out var mspDB, 
                out var txtDB, 
                out var isotopeTextDB, 
                out var compoundsInTargetMode,
                out var lbmDB);
            param.TextDbSearchParam.CcsTolerance = 20.0F;
            param.TextDbSearchParam.IsUseCcsForAnnotationFiltering = true;

            RawMeasurement? rawobj = null;
            Console.WriteLine("Reading data...");
            using (var access = new RawDataAccess(filepath, 0, false, true, false)) {
                rawobj = access.GetMeasurement();
            }
            var providerFactory = new StandardDataProviderFactory().ContraMap((AnalysisFileBean f) => {
                using var access = new RawDataAccess(f.AnalysisFilePath, 0, getProfileData: false, isImagingMsData: true, isGuiProcess: false);
                return access.GetMeasurement();
            });
            Console.WriteLine("Peak picking...");
            var provider = new StandardDataProviderFactory().Create(rawobj);
            var container = new MsdialImmsDataStorage {
                AnalysisFiles = [file], 
                AlignmentFiles = [],
                MspDB = mspDB, TextDB = txtDB, IsotopeTextDB = isotopeTextDB, IupacDatabase = iupacDB, MsdialImmsParameter = param
            };
            var database = new MoleculeDataBase(txtDB, reffile, DataBaseSource.Text, SourceType.TextDB);
            var annotator = new ImmsTextDBAnnotator(database, param.TextDbSearchParam, param.TextDBFilePath, 1);
            container.DataBases = DataBaseStorage.CreateEmpty();
            container.DataBases.AddMoleculeDataBase(
                database,
                [ 
                    new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, param.PeakPickBaseParam, param.TextDbSearchParam, ignoreIsotopicPeak: false))
                ]
            );
            storage.AddStorage(container);

            var evaluator = new MsScanMatchResultEvaluator(param.TextDbSearchParam);
            var processor = new MsdialImmsImagingCore.Process.FileProcess(container, providerFactory, null, null, evaluator);
            processor.RunAsync(file, provider).Wait();
            using (var fs = File.Open(storage.ProjectParameter.FilePath, FileMode.Create))
            using (var streamManager = ZipStreamManager.OpenCreate(fs)) {
                var serializer = new MsdialIntegrateSerializer();
                storage.Save(
                    streamManager,
                    serializer,
                    path => new DirectoryTreeStreamManager(path),
                    parameter => { }).Wait();
                ((IStreamManager)streamManager).Complete();
            }

            var features = MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath);
            RawSpectraOnPixels? pixelData = null;
            var featureElements = features.Select(n => new Raw2DElement(n.PeakFeature.Mass, n.PeakFeature.ChromXsTop.Value)).ToList();
            Console.WriteLine("Reading data...");
            using (var access = new RawDataAccess(filepath, 0, false, true, false)) {
                pixelData = access.GetRawPixelFeatures(featureElements, null);
            }

            //foreach (var (feature, pixel) in IEnumerableExtension.Zip(features, pixelData.PixelPeakFeaturesList)) {
                
            //    if (feature.IsReferenceMatched(evaluator)) {
            //        Console.WriteLine("Name\t" + feature.Name);
            //        Console.WriteLine("MZ\t" + feature.PrecursorMz);
            //        Console.WriteLine("Drift\t" + feature.ChromXsTop.Value);
            //        Console.WriteLine("CCS\t" + feature.CollisionCrossSection);

            //        var refdata = container.TextDB[feature.MatchResults.Representative.LibraryID];
            //        Console.WriteLine("RefMz\t" + refdata.PrecursorMz);
            //        Console.WriteLine("RefCCS\t" + refdata.CollisionCrossSection);

            //        Console.WriteLine("PixelCount\t" + pixel.IntensityArray.Length);
            //        Console.WriteLine("X_Index\tY_Index\tX_UM\tY_UM\tIntensity");
            //        foreach (var (intensity, frame) in pixel.IntensityArray.Zip(pixelData.XYFrames, (intensity, frame) => (intensity, frame))) {
            //            var x_index = frame.XIndexPos;
            //            var y_index = frame.YIndexPos;
            //            var x_um = frame.MotorPositionX;
            //            var y_um = frame.MotorPositionY;

            //            Console.WriteLine(x_index + "\t" + y_index + "\t" + x_um + "\t" + y_um + "\t" + intensity);
            //        }
            //    }
            //}
        }

        public static void TimsOffTest() {
            var filepath = @"E:\6_Projects\PROJECT_ImagingMS\20210122_timsTOF flex-data\Brain-C-1-9AA-OFF\Brain-C-9AA-TIMS-OFF-01.d";
            var reffile = @"E:\6_Projects\PROJECT_ImagingMS\Lipid reference library\20220725_timsTOFpro_TextLibrary_Brain_Neg.txt";
            //var outputfile = @"E:\6_Projects\PROJECT_ImagingMS\20210122_timsTOF flex-data\Brain-C-1-9AA-OFF\Brain-C-9AA-TIMS-OFF-01_msdial.txt";
            var filename = Path.GetFileNameWithoutExtension(filepath);
            var fileDir = Path.GetDirectoryName(filepath);
            var file = new AnalysisFileBean() {
                AnalysisFileId = 0,
                AnalysisFileIncluded = true,
                AnalysisFileName = filename,
                AnalysisFilePath = filepath,
                AnalysisFileAnalyticalOrder = 1,
                AnalysisFileClass = "0",
                AnalysisFileType = AnalysisFileType.Sample,
                DeconvolutionFilePath = fileDir + "\\" + filename + "_test220906" + ".dcl",
                PeakAreaBeanInformationFilePath = fileDir + "\\" + filename + "_test220906" + ".pai"
            };

            var param = new MsdialDimsParameter() {
                TextDBFilePath = reffile,
                IonMode = IonMode.Negative,
                MinimumAmplitude = 10000,
            };

            CommonProcess.ParseLibraries(param, -1,
                out var iupacDB,
                out var mspDB,
                out var txtDB,
                out var isotopeTextDB,
                out var compoundsInTargetMode,
                out var lbmDB);

            RawMeasurement? rawobj = null;
            Console.WriteLine("Reading data...");
            using (var access = new RawDataAccess(filepath, 0, false, true, false)) {
                rawobj = access.GetMeasurement();
            }
            Console.WriteLine("Peak picking...");
            var container = new MsdialDimsDataStorage {
                AnalysisFiles = [file],
                AlignmentFiles = [],
                MspDB = mspDB, TextDB = txtDB, IsotopeTextDB = isotopeTextDB, IupacDatabase = iupacDB, MsdialDimsParameter = param
            };

            var evaluator = new MsScanMatchResultEvaluator(param.TextDbSearchParam);
            var database = new MoleculeDataBase(txtDB, reffile, DataBaseSource.Text, SourceType.TextDB);
            var annotator = new CompMs.MsdialDimsCore.Algorithm.Annotation.DimsTextDBAnnotator(database, param.TextDbSearchParam, param.TextDBFilePath, 1);
            container.DataBases = DataBaseStorage.CreateEmpty();
            container.DataBases.AddMoleculeDataBase(
                database,
                [
                    new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryWithoutIsotopeFactory(annotator, param.TextDbSearchParam))
                ]
            );

            var annotationProcess = BuildAnnotationProcess(container.DataBases);

            MsdialDimsCore.ProcessFile processor = new MsdialDimsCore.ProcessFile(new StandardDataProviderFactory(), container, annotationProcess, evaluator);
            processor.RunAsync(file, ProcessOption.PeakSpotting | ProcessOption.Identification, null, default).Wait();
            var features = MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath);

            RawSpectraOnPixels? pixelData = null;
            var featureElements = features.Select(n => new Raw2DElement() { Mz = n.PeakFeature.Mass }).ToList();
            Console.WriteLine("Reading data...");
            using (var access = new RawDataAccess(filepath, 0, false, true, false)) {
                pixelData = access.GetRawPixelFeatures(featureElements, null);
            }

            foreach (var (feature, pixel) in features.Zip(pixelData.PixelPeakFeaturesList, (feature, pixel) => (Feature: feature, Pixel: pixel))) {
                if (feature.IsReferenceMatched(evaluator)) {
                    Console.WriteLine("Name\t" + feature.Name);
                    Console.WriteLine("MZ\t" + feature.PrecursorMz);

                    var refdata = container.TextDB[feature.MatchResults.Representative.LibraryID];
                    Console.WriteLine("RefMz\t" + refdata.PrecursorMz);

                    Console.WriteLine("PixelCount\t" + pixel.IntensityArray.Length);
                    Console.WriteLine("X_Index\tY_Index\tX_UM\tY_UM\tIntensity");
                    foreach (var (intensity, frame) in pixel.IntensityArray.Zip(pixelData.XYFrames, (intensity, frame) => (intensity, frame))) {
                        var x_index = frame.XIndexPos;
                        var y_index = frame.YIndexPos;
                        var x_um = frame.MotorPositionX;
                        var y_um = frame.MotorPositionY;

                        Console.WriteLine(x_index + "\t" + y_index + "\t" + x_um + "\t" + y_um + "\t" + intensity);
                    }
                }
            }
        }

        private static IAnnotationProcess BuildAnnotationProcess(DataBaseStorage storage) {
            return new StandardAnnotationProcess(
                storage.CreateQueryFactories().MoleculeQueryFactories,
                FacadeMatchResultEvaluator.FromDataBases(storage),
                storage.CreateDataBaseMapper());
        }
    }



}
