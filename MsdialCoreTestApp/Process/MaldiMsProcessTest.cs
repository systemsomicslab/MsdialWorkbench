using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialImmsCore.Algorithm;
using CompMs.MsdialImmsCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.MsdialConsole.Process {
    public sealed class MaldiMsProcessTest {
        private MaldiMsProcessTest() { }
        public static void TimsOnTest() {
            var filepath = @"E:\6_Projects\PROJECT_ImagingMS\20210122_timsTOF flex-data\Brain-C-3-9AA-ON\Brain-C-3-9AA-TIMS-ON-1.d";
            var reffile = @"E:\6_Projects\PROJECT_ImagingMS\Lipid reference library\20220725_timsTOFpro_TextLibrary_Brain_Neg.txt";
            var outputfile = @"E:\6_Projects\PROJECT_ImagingMS\20210122_timsTOF flex-data\Brain-C-3-9AA-ON\Brain-C-3-9AA-TIMS-ON-1_msdial.txt";
            var filename = System.IO.Path.GetFileNameWithoutExtension(filepath);
            var fileDir = System.IO.Path.GetDirectoryName(filepath);
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

            var param = new MsdialImmsParameter() {
                TextDBFilePath = reffile,
                IonMode = IonMode.Negative,
                MinimumAmplitude = 10000,
                FileID2CcsCoefficients = new Dictionary<int, CoefficientsForCcsCalculation>() {
                    { 0, new CoefficientsForCcsCalculation() { IsBrukerIM = true } }
                }
            };

            CommonProcess.ParseLibraries(param, -1, 
                out var iupacDB, 
                out var mspDB, 
                out var txtDB, 
                out var isotopeTextDB, 
                out var compoundsInTargetMode);

            RawMeasurement rawobj = null;
            Console.WriteLine("Reading data...");
            using (var access = new RawDataAccess(filepath, 0, false, true, false)) {
                rawobj = access.GetMeasurement();
            }
            Console.WriteLine("Peak picking...");
            var provider = new StandardDataProviderFactory().Create(rawobj);
            var container = new MsdialImmsDataStorage {
                AnalysisFiles = new List<AnalysisFileBean>() { file }, 
                AlignmentFiles = new List<AlignmentFileBean>(),
                MspDB = mspDB, TextDB = txtDB, IsotopeTextDB = isotopeTextDB, IupacDatabase = iupacDB, MsdialImmsParameter = param
            };

            var evaluator = MsScanMatchResultEvaluator.CreateEvaluator(param.TextDbSearchParam);
            var database = new MoleculeDataBase(txtDB, reffile, DataBaseSource.Text, SourceType.TextDB);
            var annotator = new CompMs.MsdialImmsCore.Algorithm.Annotation.ImmsTextDBAnnotator(database, param.TextDbSearchParam, param.TextDBFilePath, 1);


            container.DataBases = new DataBaseStorage(null, null, null);
            container.DataBases.AddMoleculeDataBase(
                database,
                new List<IAnnotatorParameterPair<IAnnotationQuery, Common.Components.MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> { 
                    new MetabolomicsAnnotatorParameterPair(annotator, param.TextDbSearchParam)
                }
            );

            //var mspAnnotator = new CompMs.MsdialImmsCore.Algorithm.Annotation.ImmsMspAnnotator(new MoleculeDataBase(container.MspDB, "MspDB", DataBaseSource.Msp, SourceType.MspDB), param.MspSearchParam, param.TargetOmics, "MspDB", -1);

            CompMs.MsdialImmsCore.Process.FileProcess.Run(file, container, null, annotator, provider, evaluator, false, null);
            var features = MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath);

            RawSpectraOnPixels pixelData = null;
            var featureElements = features.Select(n => new Raw2DElement() { Mz = n.Mass, Drift = n.ChromXsTop.Value }).ToList();
            Console.WriteLine("Reading data...");
            using (var access = new RawDataAccess(filepath, 0, false, true, false)) {
                pixelData = access.GetRawPixelFeatures(featureElements);
            }

            foreach (var item in features.Zip(pixelData.PixelPeakFeaturesList, (feature, pixel) => (Feature: feature, Pixel: pixel))) {
                var feature = item.Feature;
                var pixel = item.Pixel;
                
                if (feature.IsReferenceMatched(evaluator)) {
                    Console.WriteLine("Name\t" + feature.Name);
                    Console.WriteLine("MZ\t" + feature.PrecursorMz);
                    Console.WriteLine("Drift\t" + feature.ChromXsTop.Value);
                    Console.WriteLine("CCS\t" + feature.CollisionCrossSection);

                    var refdata = container.TextDB[feature.MatchResults.Representative.LibraryID];
                    Console.WriteLine("RefMz\t" + refdata.PrecursorMz);
                    Console.WriteLine("RefCCS\t" + refdata.CollisionCrossSection);

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

        public static void TimsOffTest() {
            var filepath = @"E:\6_Projects\PROJECT_ImagingMS\20210122_timsTOF flex-data\Brain-C-1-9AA-OFF\Brain-C-9AA-TIMS-OFF-01.d";
            var reffile = @"E:\6_Projects\PROJECT_ImagingMS\Lipid reference library\20220725_timsTOFpro_TextLibrary_Brain_Neg.txt";
            var outputfile = @"E:\6_Projects\PROJECT_ImagingMS\20210122_timsTOF flex-data\Brain-C-1-9AA-OFF\Brain-C-9AA-TIMS-OFF-01_msdial.txt";
            var filename = System.IO.Path.GetFileNameWithoutExtension(filepath);
            var fileDir = System.IO.Path.GetDirectoryName(filepath);
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
                out var compoundsInTargetMode);

            RawMeasurement rawobj = null;
            Console.WriteLine("Reading data...");
            using (var access = new RawDataAccess(filepath, 0, false, true, false)) {
                rawobj = access.GetMeasurement();
            }
            Console.WriteLine("Peak picking...");
            var provider = new StandardDataProviderFactory().Create(rawobj);
            var container = new MsdialDimsDataStorage {
                AnalysisFiles = new List<AnalysisFileBean>() { file },
                AlignmentFiles = new List<AlignmentFileBean>(),
                MspDB = mspDB, TextDB = txtDB, IsotopeTextDB = isotopeTextDB, IupacDatabase = iupacDB, MsdialDimsParameter = param
            };

            var evaluator = MsScanMatchResultEvaluator.CreateEvaluator(param.TextDbSearchParam);
            var database = new MoleculeDataBase(txtDB, reffile, DataBaseSource.Text, SourceType.TextDB);
            var annotator = new CompMs.MsdialDimsCore.Algorithm.Annotation.DimsTextDBAnnotator(database, param.TextDbSearchParam, param.TextDBFilePath, 1);
            container.DataBases = new DataBaseStorage(null, null, null);
            container.DataBases.AddMoleculeDataBase(
                database,
                new List<IAnnotatorParameterPair<IAnnotationQuery, Common.Components.MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> {
                    new MetabolomicsAnnotatorParameterPair(annotator, param.TextDbSearchParam)
                }
            );

            //var mspAnnotator = new CompMs.MsdialImmsCore.Algorithm.Annotation.ImmsMspAnnotator(new MoleculeDataBase(container.MspDB, "MspDB", DataBaseSource.Msp, SourceType.MspDB), param.MspSearchParam, param.TargetOmics, "MspDB", -1);

            CompMs.MsdialDimsCore.Process.FileProcess.Run(file, container, null, annotator, provider, evaluator, false, null);
            var features = MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath);

            RawSpectraOnPixels pixelData = null;
            var featureElements = features.Select(n => new Raw2DElement() { Mz = n.Mass, Drift = n.ChromXsTop.Value }).ToList();
            Console.WriteLine("Reading data...");
            using (var access = new RawDataAccess(filepath, 0, false, true, false)) {
                pixelData = access.GetRawPixelFeatures(featureElements);
            }

            foreach (var item in features.Zip(pixelData.PixelPeakFeaturesList, (feature, pixel) => (Feature: feature, Pixel: pixel))) {
                var feature = item.Feature;
                var pixel = item.Pixel;

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

    }



}
