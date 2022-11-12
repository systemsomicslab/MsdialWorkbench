using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialImmsCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.MsdialImmsImagingCore.Process
{
    public sealed class FileProcess
    {
        public Task RunAsync() {
            var filepath = @"E:\6_Projects\PROJECT_ImagingMS\20211005_Bruker_timsTOFfleX-selected\Eye_Neg\20211005_Eye_Acsl_HZ_KO_Neg\20211005_Eye_Acsl_HZ_KO_Neg.d";
            var reffile = @"E:\6_Projects\PROJECT_ImagingMS\Lipid reference library\20220725_timsTOFpro_TextLibrary_Eye_Neg.txt";
            var outputfile = @"E:\6_Projects\PROJECT_ImagingMS\20211005_Bruker_timsTOFfleX-selected\Eye_Neg\20211005_Eye_Acsl_HZ_KO_Neg\20211005_Eye_Acsl_HZ_KO_Neg.mddata";
            var filename = Path.GetFileNameWithoutExtension(filepath);
            var fileDir = Path.GetDirectoryName(filepath);
            var projectParameter = new ProjectParameter(DateTime.Now, @"E:\6_Projects\PROJECT_ImagingMS\20211005_Bruker_timsTOFfleX-selected\Eye_Neg\20211005_Eye_Acsl_HZ_KO_Neg\", "20211004_Acsl6_leftHZ_rightKO_Eye.mdproject");
            var storage = new ProjectDataStorage(projectParameter);
            var file = new AnalysisFileBean() {
                AnalysisFileId = 0,
                AnalysisFileIncluded = true,
                AnalysisFileName = filename,
                AnalysisFilePath = filepath,
                AnalysisFileAnalyticalOrder = 1,
                AnalysisFileClass = "0",
                AnalysisFileType = AnalysisFileType.Sample,
                DeconvolutionFilePath = Path.Combine(fileDir, filename + "_test221023" + ".dcl"),
                PeakAreaBeanInformationFilePath = Path.Combine(fileDir, filename + "_test221023" + ".pai"),
            };

            var param = new MsdialImmsParameter(isImaging: true, isLabUseOnly: true) {
                ProjectFolderPath = Path.GetDirectoryName(outputfile),
                ProjectFileName = Path.GetFileName(outputfile),
                MachineCategory = MachineCategory.IMMS,
                TextDBFilePath = reffile,
                IonMode = IonMode.Negative,
                MinimumAmplitude = 1000,
                FileID2CcsCoefficients = new Dictionary<int, CoefficientsForCcsCalculation>() {
                    { 0, new CoefficientsForCcsCalculation() { IsBrukerIM = true } }
                }
            };
            param.TextDbSearchParam.CcsTolerance = 20.0F;
            param.TextDbSearchParam.IsUseCcsForAnnotationFiltering = true;

            RawMeasurement rawobj = null;
            using (var access = new RawDataAccess(filepath, 0, getProfileData: false, isImagingMsData: true, isGuiProcess: false)) {
                rawobj = access.GetMeasurement();
            }
            var provider = new StandardDataProviderFactory().Create(rawobj);
            var container = new MsdialImmsDataStorage {
                AnalysisFiles = new List<AnalysisFileBean>() { file }, 
                AlignmentFiles = new List<AlignmentFileBean>(),
                MsdialImmsParameter = param
            };
            var processor = new MsdialImmsCore.Process.FileProcess(container, null, null, null);
            return processor.RunAsync(file, provider);
        }
    }
}
