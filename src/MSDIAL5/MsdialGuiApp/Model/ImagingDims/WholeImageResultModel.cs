using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Imaging;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.RawDataHandler.Core;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.ImagingDims;

internal sealed class WholeImageResultModel : DisposableModelBase, IWholeImageResultModel
{
    private readonly List<Raw2DElement> _elements;
    private readonly AnalysisFileBeanModel _file;
    private readonly MaldiFrames _maldiFrames;
    private readonly MsfinderSearcherFactory _msfinderSearcherFactory;
    private readonly RawIntensityOnPixelsLoader _rawIntensityLoader;
    private readonly RoiModel _wholeRoi;

    public WholeImageResultModel(AnalysisFileBeanModel file, MaldiFrames maldiFrames, RoiModel wholeRoi, IMsdialDataStorage<MsdialDimsParameter> storage, IMatchResultEvaluator<MsScanMatchResult> evaluator, IDataProviderFactory<AnalysisFileBean> providerFactory, FilePropertiesModel projectBaseParameterModel, IMessageBroker broker) {
        var peakFilter = new PeakFilterModel(DisplayFilter.All);
        var filterEnabled = FilterEnableStatus.All & ~FilterEnableStatus.Rt & ~FilterEnableStatus.Dt & ~FilterEnableStatus.Protein;
        var peakFiltering = new PeakSpotFiltering<ChromatogramPeakFeatureModel>(filterEnabled).AddTo(Disposables);
        _msfinderSearcherFactory = new MsfinderSearcherFactory(storage.DataBases, storage.DataBaseMapper, storage.Parameter, "MS-FINDER").AddTo(Disposables);
        var analysisModel = new DimsAnalysisModel(file, providerFactory.Create(file.File), evaluator, storage.DataBases, storage.DataBaseMapper, storage.Parameter, peakFilter, peakFiltering, projectBaseParameterModel, _msfinderSearcherFactory, broker).AddTo(Disposables);
        AnalysisModel = analysisModel;

        _elements = analysisModel.Ms1Peaks.Select(item => new Raw2DElement(item.Mass, item.Drift.Value)).ToList();
        ImagingRoiModel = new ImagingRoiModel($"ROI{wholeRoi.Id}", wholeRoi, null, analysisModel.Ms1Peaks, analysisModel.Target, _elements).AddTo(Disposables);
        ImagingRoiModel.Select();
        var peakIds = analysisModel.Ms1Peaks.Select((peak, index) => (peak, index)).ToDictionary(p => p.peak.MasterPeakID, p => p.index);
        _rawIntensityLoader = wholeRoi.GetIntensityOnPixelsLoader(_elements).AddTo(Disposables);
        IntensityImagePlaceholder = new IntensityImagePlaceholderModel(maldiFrames, _rawIntensityLoader);
        analysisModel.Target
            .Subscribe(p => {
                if (p is null) {
                    IntensityImagePlaceholder.ResetImage();
                }
                else {
                    var title = $"m/z {p.Mass}";
                    if (!string.IsNullOrEmpty(p.Name)) {
                        title = $"{p.Name}, {title}";
                    }
                    _ = IntensityImagePlaceholder.EnsureImageAsync(peakIds[p.MasterPeakID], title);
                }
            }).AddTo(Disposables);
        _file = file;
        _maldiFrames = maldiFrames;
        _wholeRoi = wholeRoi;
    }

    public DimsAnalysisModel AnalysisModel { get; }

    public ImagingRoiModel ImagingRoiModel { get; }

    public AnalysisPeakPlotModel PeakPlotModel => AnalysisModel.PlotModel;

    public ObservableCollection<ChromatogramPeakFeatureModel> Peaks => AnalysisModel.Ms1Peaks;

    public IntensityImagePlaceholderModel IntensityImagePlaceholder { get; }

    public ReactivePropertySlim<ChromatogramPeakFeatureModel?> Target => AnalysisModel.Target;

    public ImagingRoiModel CreateImagingRoiModel(RoiModel roi)
    {
        var result = new ImagingRoiModel($"ROI{roi.Id}", roi, _wholeRoi, AnalysisModel.Ms1Peaks, AnalysisModel.Target, _elements);
        result.Select();
        return result;
    }

    public async Task<bool> SaveIntensitiesAsync(CancellationToken token = default) {
        var filePath = string.Empty;
        var dialog = new SaveFileDialog
        {
            Filter = "Pixel intensity file|*.csv",
            DefaultExt = "csv",
            FileName = "pixel_intensities.csv",
        };
        if (dialog.ShowDialog() == true) {
            filePath = dialog.FileName;
        }
        if (string.IsNullOrEmpty(filePath)) {
            return false;
        }

        using var handle = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        using var writer = new StreamWriter(handle, UTF8Encoding.Default);
        var header = string.Join(",", new[] { "ID", "Name", "m/z", }.Concat(_maldiFrames.Infos.Select(info => $"{info.XIndexPos}_{info.YIndexPos}")));
        await writer.WriteLineAsync(header).ConfigureAwait(false);
        for (int i = 0; i < AnalysisModel.Ms1Peaks.Count; i++) {
            var peak = AnalysisModel.Ms1Peaks[i];
            var pixels = await _rawIntensityLoader.LoadAsync(i, token);
            var row = string.Format("{0},{1},{2},", peak.MasterPeakID, peak.Name, peak.Mz.Value) + string.Join(",", pixels.PixelPeakFeaturesList[0].IntensityArray);
            await writer.WriteLineAsync(row);
        }

        return true;
    }

    public void ResetRawSpectraOnPixels() {
        using var rawDataAccess = new RawDataAccess(_file.AnalysisFilePath, 0, getProfileData: true, isImagingMsData: true, isGuiProcess: true);
        rawDataAccess.SaveRawPixelFeatures(_elements, _maldiFrames.Infos.ToList());
    }

    public MaldiFrames GetFramesFromPositions(HashSet<(int, int)> sets) {
        return new MaldiFrames(_maldiFrames.Infos.Where(info => sets.Contains((info.XIndexPos, info.YIndexPos))), _maldiFrames);
    }

    public Task SaveAsync(CancellationToken token = default)
    {
        return AnalysisModel.SaveAsync(token);
    }
}
