using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialImmsCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialImmsImagingCore.Process;

public sealed class FileProcess
{
    private readonly MsdialImmsCore.Process.FileProcess _processor;

    public FileProcess(
        IMsdialDataStorage<MsdialImmsParameter> storage,
        IDataProviderFactory<AnalysisFileBean> dataProviderFactory,
        IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? mspAnnotator,
        IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>? textDBAnnotator,
        IMatchResultEvaluator<MsScanMatchResult> evaluator) {
        _processor = new MsdialImmsCore.Process.FileProcess(storage, dataProviderFactory, mspAnnotator, textDBAnnotator, evaluator);
    }

    public async Task RunAsync(AnalysisFileBean file, IDataProvider provider, Action<int>? reportAction = null, CancellationToken token = default) {
        await _processor.RunAsync(file, provider, reportAction is not null ? new Progress<int>(reportAction) : null, token).ConfigureAwait(false);

        var chromPeakFeatures = await file.LoadChromatogramPeakFeatureCollectionAsync(token).ConfigureAwait(false);
        var _elements = chromPeakFeatures.Items.Select(item => new Raw2DElement(item.PeakFeature.Mass, item.PeakFeature.ChromXsTop.Drift.Value)).ToList();
        var pixels = RetrieveRawSpectraOnPixels(file, _elements, true);
    }

    public async Task RunAsyncTest(AnalysisFileBean file, IDataProvider provider, Action<int>? reportAction = null, CancellationToken token = default) {
        await _processor.RunAsync(file, provider, reportAction is not null ? new Progress<int>(reportAction) : null, token).ConfigureAwait(false);

        var chromPeakFeatures = await file.LoadChromatogramPeakFeatureCollectionAsync(token).ConfigureAwait(false);
        var _elements = chromPeakFeatures.Items.Select(item => new Raw2DElement(item.PeakFeature.Mass, item.PeakFeature.ChromXsTop.Drift.Value)).ToList();
        var pixels = RetrieveRawSpectraOnPixels(file, _elements, true);

        foreach (var element in pixels.PixelPeakFeaturesList) {
            if (Math.Abs(element.Mz - 885.5472) < 0.01) {
                Console.WriteLine(element.Mz + "\t" + element.Drift);
                var frames = pixels.XYFrames;
                for (int i = 0; i < element.IntensityArray.Length; i++) {
                    Console.WriteLine(frames[i].XIndexPos + "\t" + frames[i].YIndexPos + "\t" + element.IntensityArray[i]);
                }
            }
        }
    }


    private RawSpectraOnPixels RetrieveRawSpectraOnPixels(AnalysisFileBean file, List<Raw2DElement> targetElements, bool isNewFileProcess) {
        if (targetElements.IsEmptyOrNull()) return null;
        using (RawDataAccess rawDataAccess = new RawDataAccess(file.AnalysisFilePath, 0, true, true, true, 10, 0.02, 0.015)) {
            return rawDataAccess.GetRawPixelFeatures(targetElements, file.GetMaldiFrames(), isNewFileProcess)
                ?? new RawSpectraOnPixels { PixelPeakFeaturesList = new List<RawPixelFeatures>(0), XYFrames = new List<MaldiFrameInfo>(0), };
        }
    }

    public Task RunAsyncTest() {
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
        var providerFactory = new StandardDataProviderFactory().ContraMap((AnalysisFileBean f) => {
            using var access = new RawDataAccess(f.AnalysisFilePath, 0, getProfileData: false, isImagingMsData: true, isGuiProcess: false);
            return access.GetMeasurement();
        });
        var provider = new StandardDataProviderFactory().Create(rawobj);
        var container = new MsdialImmsDataStorage {
            AnalysisFiles = new List<AnalysisFileBean>() { file }, 
            AlignmentFiles = new List<AlignmentFileBean>(),
            MsdialImmsParameter = param
        };
        var processor = new FileProcess(container, providerFactory, null, null, null);
        return processor.RunAsync(file, provider);
    }
}
