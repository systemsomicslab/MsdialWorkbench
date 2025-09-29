using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore;
using CompMs.MsdialDimsCore.Export;
using CompMs.MsdialDimsCore.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.ImagingDims;

internal sealed class ImagingDimsMethodModel : MethodModelBase, IMethodModel
{
    private readonly IMessageBroker _broker;
    private readonly FacadeMatchResultEvaluator _evaluator;
    private readonly FilePropertiesModel _projectBaseParameter;
    private readonly IDataProviderFactory<AnalysisFileBean> _providerFactory;
    private readonly IMsdialDataStorage<MsdialDimsParameter> _storage;

    public ImagingDimsMethodModel(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, AlignmentFileBeanModelCollection alignmentFileBeanModelCollection, IMsdialDataStorage<MsdialDimsParameter> storage, FilePropertiesModel projectBaseParameter, StudyContextModel studyContext, IMessageBroker broker)
        : base(analysisFileBeanModelCollection, alignmentFileBeanModelCollection, projectBaseParameter)
    {
        _storage = storage;
        _broker = broker;
        _projectBaseParameter = projectBaseParameter;
        StudyContext = studyContext;
        _evaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
        _providerFactory = new StandardDataProviderFactory().ContraMap((AnalysisFileBean file) => file.LoadRawMeasurement(isImagingMsData: true, isGuiProcess: true, retry: 5, sleepMilliSeconds: 5000));
        ImageModels = [];
        Image = ImageModels.FirstOrDefault();

        ParameterExporModel = new ParameterExportModel(storage.DataBases, storage.Parameter, broker);
    }

    public ImagingDimsImageModel? Image {
        get => _image;
        set => SetProperty(ref _image, value);
    }
    private ImagingDimsImageModel? _image;

    public ObservableCollection<ImagingDimsImageModel> ImageModels { get; }

    public ParameterExportModel ParameterExporModel { get; }

    public StudyContextModel StudyContext { get; }

    public override async Task RunAsync(ProcessOption option, CancellationToken token)
    {
        var parameter = _storage.Parameter;
        var starttimestamp = DateTime.Now.ToString("yyyyMMddHHmm");
        var stopwatch = Stopwatch.StartNew();

        var files = AnalysisFileModelCollection.IncludedAnalysisFiles;

        //var dimsprovider = _storage.Parameter.ProviderFactoryParameter.Create(retry: 5, isGuiProcess: true);

        if (option.HasFlag(ProcessOption.Identification)) {
            var queryFatoires = _storage.CreateAnnotationQueryFactoryStorage();
            var annotationProcess = new StandardAnnotationProcess(queryFatoires.MoleculeQueryFactories, _evaluator, _storage.DataBaseMapper);
            var processor = new ProcessFile(_providerFactory, _storage, annotationProcess, _evaluator);
            var runner = new ProcessRunner(processor, 2);
            await runner.RunAllAsync(_storage.AnalysisFiles, option, Enumerable.Repeat<IProgress<int>?>(null, _storage.AnalysisFiles.Count), null, token).ConfigureAwait(false);
            foreach (var file in files) {
                var model = new ImagingDimsImageModel(file, _storage, _evaluator, _providerFactory, _projectBaseParameter, _broker);
                ImageModels.Add(model);
                if (option.HasFlag(ProcessOption.PeakSpotting)) {
                    model.ImageResult.ResetRawSpectraOnPixels();
                }
            }
        }

        stopwatch.Stop();
        var ts = stopwatch.Elapsed;
        AutoParametersSave(starttimestamp, ts, parameter);
        await LoadAnalysisFileAsync(files.FirstOrDefault(), token).ConfigureAwait(false);
    }

    protected override IAlignmentModel? LoadAlignmentFileCore(AlignmentFileBeanModel alignmentFileModel)
    {
        return null;
    }

    protected override IAnalysisModel? LoadAnalysisFileCore(AnalysisFileBeanModel analysisFile)
    {
        Image = ImageModels.FirstOrDefault(image => image.File == analysisFile);
        return null;
    }

    public override Task LoadAsync(CancellationToken token) {
        foreach (var file in AnalysisFileModelCollection.AnalysisFiles) {
            ImageModels.Add(new ImagingDimsImageModel(file, _storage, _evaluator, _providerFactory, _projectBaseParameter, _broker));
        }
        var analysisFile = AnalysisFileModelCollection.IncludedAnalysisFiles.FirstOrDefault();
        if (analysisFile is not null) {
            Image = ImageModels.FirstOrDefault(image => image.File == analysisFile);
        }
        return Task.CompletedTask;
    }

    public override Task SaveAsync()
    {
        if (Image is null || Image.ImageResult is null)
        {
            return Task.CompletedTask;
        }

        return Image.ImageResult.SaveAsync();
    }

    public AnalysisResultExportModel CreateExportAnalysisModel() {
        var spectraTypes = new[]
        {
            new SpectraType(
                ExportspectraType.deconvoluted,
                new DimsAnalysisMetadataAccessor(_storage.DataBaseMapper, _storage.Parameter, ExportspectraType.deconvoluted),
                _providerFactory),
        };
        var spectraFormats = new[]
        {
            new SpectraFormat(ExportSpectraFileFormat.txt, new AnalysisCSVExporterFactory(separator: "\t")),
        };
        var models = new IMsdialAnalysisExport[]
        {
            new MsdialAnalysisTableExportModel(spectraTypes, spectraFormats, _broker),
            new SpectraTypeSelectableMsdialAnalysisExportModel(new Dictionary<ExportspectraType, IAnalysisExporter<ChromatogramPeakFeatureCollection>> {
                [ExportspectraType.deconvoluted] = new AnalysisMspExporter(_storage.DataBaseMapper, _storage.Parameter),
                [ExportspectraType.centroid] = new AnalysisMspExporter(_storage.DataBaseMapper, _storage.Parameter, file => new CentroidMsScanPropertyLoader(_storage.Parameter.ProviderFactoryParameter.Create().Create(file.LoadRawMeasurement(isImagingMsData: true, isGuiProcess: true, retry: 5, sleepMilliSeconds: 5000)), _storage.Parameter.MS2DataType)),
            })
            {
                FilePrefix = "Msp",
                FileSuffix = "msp",
                Label = "Nist format (*.msp)"
            },
            new SpectraTypeSelectableMsdialAnalysisExportModel(new Dictionary<ExportspectraType, IAnalysisExporter<ChromatogramPeakFeatureCollection>> {
                [ExportspectraType.deconvoluted] = new AnalysisMgfExporter(file => new MSDecLoader(file.DeconvolutionFilePath, file.DeconvolutionFilePathList)),
                [ExportspectraType.centroid] = new AnalysisMgfExporter(file => new CentroidMsScanPropertyLoader(_storage.Parameter.ProviderFactoryParameter.Create().Create(file.LoadRawMeasurement(isImagingMsData: true, isGuiProcess: true, retry: 5, sleepMilliSeconds: 5000)), _storage.Parameter.MS2DataType)),
            })
            {
                FilePrefix = "Mgf",
                FileSuffix = "mgf",
                Label = "MASCOT format (*.mgf)"
            },
            new MsdialAnalysisMassBankRecordExportModel(_storage.Parameter.ProjectParam, StudyContext),
        };

        return new AnalysisResultExportModel(AnalysisFileModelCollection, _storage.Parameter.ProjectParam.ProjectFolderPath, _broker, models);
    }
}
