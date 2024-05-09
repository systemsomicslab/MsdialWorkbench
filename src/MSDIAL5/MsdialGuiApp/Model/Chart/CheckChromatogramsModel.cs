using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.MsResult;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Algorithm.PeakPick;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.UI;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Chart;

internal sealed class CheckChromatogramsModel : BindableBase
{
    private readonly AccumulateSpectraUsecase _accumulateSpectra;
    private readonly ICompoundSearchUsecase<ICompoundResult, PeakSpotModel>? _compoundSearch;
    private readonly AdvancedProcessOptionBaseParameter _advancedProcessParameter;
    private readonly IMessageBroker _broker;
    private readonly List<PeakFeatureSearchValue> _displaySettingValueCandidates;
    private readonly ObservableCollection<PeakFeatureSearchValueModel> _displaySettingValues;
    private readonly MsScanCompoundSearchUsecase _scanCompoundSearchUsecase;

    public CheckChromatogramsModel(LoadChromatogramsUsecase loadingChromatograms, AccumulateSpectraUsecase accumulateSpectra, ICompoundSearchUsecase<ICompoundResult, PeakSpotModel>? compoundSearch, AdvancedProcessOptionBaseParameter advancedProcessParameter, AnalysisFileBeanModel file, IMessageBroker broker) {
        LoadChromatogramsUsecase = loadingChromatograms ?? throw new ArgumentNullException(nameof(loadingChromatograms));
        _accumulateSpectra = accumulateSpectra;
        _compoundSearch = compoundSearch;
        _advancedProcessParameter = advancedProcessParameter;
        File = file;
        _broker = broker;
        advancedProcessParameter.DiplayEicSettingValues ??= [];
        var values = advancedProcessParameter.DiplayEicSettingValues.Where(n => n.Mass > 0 && n.MassTolerance > 0).ToList();
        values.AddRange(Enumerable.Repeat(0, 100).Select(_ => new PeakFeatureSearchValue()));
        _displaySettingValueCandidates = values;
        _displaySettingValues = new ObservableCollection<PeakFeatureSearchValueModel>(values.Select(v => new PeakFeatureSearchValueModel(v)));
        DisplayEicSettingValues = new ReadOnlyObservableCollection<PeakFeatureSearchValueModel>(_displaySettingValues);
        _scanCompoundSearchUsecase = new MsScanCompoundSearchUsecase();

        var settings = Properties.Settings.Default;
        if (settings.ChromatogramsViewLayoutTemplate is { } layout) {
            _layout = layout;
        }
        else {
            _layout = new ContainerElement
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Items = [
                    new LeafElement { Width = new(2, System.Windows.GridUnitType.Star), Size = 1, Priorities = [3] },
                    new LeafElement { Width = new(1, System.Windows.GridUnitType.Star), Size = 2, Priorities = [2, 1] },
                ],
            };
        }
    }

    public AnalysisFileBeanModel File { get; }

    public ChromatogramsModel? Chromatograms {
        get => _chromatograms;
        private set => SetProperty(ref _chromatograms, value);
    }
    private ChromatogramsModel? _chromatograms;

    public RangeSelectableChromatogramModel? RangeSelectableChromatogramModel {
        get => _rangeSelectableChromatogramModel;
        private set => SetProperty(ref _rangeSelectableChromatogramModel, value);
    }
    private RangeSelectableChromatogramModel? _rangeSelectableChromatogramModel;

    public AccumulatedMs1SpectrumModel? AccumulatedMs1SpectrumModel {
        get => _accumulatedMs1SpectrumModel;
        private set {
            var prev = _accumulatedMs1SpectrumModel;
            if (SetProperty(ref _accumulatedMs1SpectrumModel, value)) {
                prev?.Dispose();
            }
        }
    }
    private AccumulatedMs1SpectrumModel? _accumulatedMs1SpectrumModel;

    public AccumulatedMs2SpectrumModel? AccumulatedMs2SpectrumModel {
        get => _accumulatedMs2SpectrumModel;
        private set {
            var prev = _accumulatedMs2SpectrumModel;
            if (SetProperty(ref _accumulatedMs2SpectrumModel, value)) {
                prev?.Dispose();
            }
        }
    }
    private AccumulatedMs2SpectrumModel? _accumulatedMs2SpectrumModel;

    public AccumulatedSpecificExperimentMS2SpectrumModel[] AccumulatedSpecificExperimentMS2SpectrumModels {
        get => _accumulatedSpecificExperimentMS2SpectrumModels;
        private set {
            var prevs = _accumulatedSpecificExperimentMS2SpectrumModels;
            if (SetProperty(ref _accumulatedSpecificExperimentMS2SpectrumModels, value)) {
                foreach (var prev in prevs) {
                    prev.Dispose();
                }
            }
        }
    }
    private AccumulatedSpecificExperimentMS2SpectrumModel[] _accumulatedSpecificExperimentMS2SpectrumModels = [];

    public AccumulatedExtractedMs2SpectrumModel[] AccumulatedMs2SpectrumModels {
        get => _accumulatedMs2SpectrumModels;
        private set {
            var prevs = _accumulatedMs2SpectrumModels;
            if (SetProperty(ref _accumulatedMs2SpectrumModels, value)) {
                foreach (var prev in prevs) {
                    prev.Dispose();
                }
            }
        }
    }
    private AccumulatedExtractedMs2SpectrumModel[] _accumulatedMs2SpectrumModels = [];

    public ReadOnlyObservableCollection<PeakFeatureSearchValueModel> DisplayEicSettingValues { get; }

    public LoadChromatogramsUsecase LoadChromatogramsUsecase { get; }

    public IDockLayoutElement Layout {
        get => _layout;
        set => SetProperty(ref _layout, value);
    }
    private IDockLayoutElement _layout;

    public Task ExportAsync(Stream stream, string separator) {
        return Chromatograms?.ExportAsync(stream, separator) ?? Task.CompletedTask;
    }

    public void Update() {
        foreach (var m in DisplayEicSettingValues) {
            m.Commit();
        }
        _advancedProcessParameter.DiplayEicSettingValues.Clear();
        _advancedProcessParameter.DiplayEicSettingValues.AddRange(_displaySettingValueCandidates.Where(n => n.Mass > 0 && n.MassTolerance > 0));
        var displayEICs = _advancedProcessParameter.DiplayEicSettingValues;
        Chromatograms = LoadChromatogramsUsecase.Load(displayEICs);
        if (Chromatograms.AbundanceAxisItemSelector.SelectedAxisItem.AxisManager is BaseAxisManager<double> axis) {
            axis.ChartMargin = new ConstantMargin(0, 60);
        }
        AccumulatedMs1SpectrumModel = new AccumulatedMs1SpectrumModel(_accumulateSpectra, _scanCompoundSearchUsecase, LoadChromatogramsUsecase, File, _broker);
        AccumulatedMs2SpectrumModel = new AccumulatedMs2SpectrumModel(_accumulateSpectra, _scanCompoundSearchUsecase, LoadChromatogramsUsecase, File, _broker);

        RangeSelectableChromatogramModel = new RangeSelectableChromatogramModel(Chromatograms);
        AccumulatedMs2SpectrumModels = Chromatograms.DisplayChromatograms
            .OfType<DisplayExtractedIonChromatogram>()
            .Select(c => new AccumulatedExtractedMs2SpectrumModel(c, _accumulateSpectra, _compoundSearch, LoadChromatogramsUsecase, File, _broker))
            .ToArray();
        AccumulatedSpecificExperimentMS2SpectrumModels = Chromatograms.DisplayChromatograms
            .OfType<DisplaySpecificExperimentChromatogram>()
            .Select(c => new AccumulatedSpecificExperimentMS2SpectrumModel(c, _accumulateSpectra, _scanCompoundSearchUsecase, LoadChromatogramsUsecase, File, _broker))
            .ToArray();
    }

    public async Task AccumulateAsync(CancellationToken token) {
        if (RangeSelectableChromatogramModel is { MainRange: not null } && AccumulatedMs1SpectrumModel is not null) {
            var range = RangeSelectableChromatogramModel.ConvertToRt(RangeSelectableChromatogramModel.MainRange);
            var subs = RangeSelectableChromatogramModel.SubtractRanges.Select(r => RangeSelectableChromatogramModel.ConvertToRt(r)).ToArray();
            await AccumulatedMs1SpectrumModel.CalculateMs1Async(range, subs, token).ConfigureAwait(false);
        }
    }

    public async Task AccumulateAsync(AccumulatedMs2SpectrumModel model, CancellationToken token) {
        if (RangeSelectableChromatogramModel is { MainRange: not null } ) {
            var range = RangeSelectableChromatogramModel.ConvertToRt(RangeSelectableChromatogramModel.MainRange);
            var subs = RangeSelectableChromatogramModel.SubtractRanges.Select(r => RangeSelectableChromatogramModel.ConvertToRt(r)).ToArray();
            await model.CalculateMs2Async(range, subs, token).ConfigureAwait(false);
        }
    }


    public async Task AccumulateAsync(AccumulatedExtractedMs2SpectrumModel model, CancellationToken token) {
        if (RangeSelectableChromatogramModel is { MainRange: not null } ) {
            var range = RangeSelectableChromatogramModel.ConvertToRt(RangeSelectableChromatogramModel.MainRange);
            var subs = RangeSelectableChromatogramModel.SubtractRanges.Select(r => RangeSelectableChromatogramModel.ConvertToRt(r)).ToArray();
            await model.CalculateMs2Async(range, subs, token).ConfigureAwait(false);
        }
    }

    public async Task AccumulateAsync(AccumulatedSpecificExperimentMS2SpectrumModel model, CancellationToken token) {
        if (RangeSelectableChromatogramModel is { MainRange: not null } ) {
            var range = RangeSelectableChromatogramModel.ConvertToRt(RangeSelectableChromatogramModel.MainRange);
            var subs = RangeSelectableChromatogramModel.SubtractRanges.Select(r => RangeSelectableChromatogramModel.ConvertToRt(r)).ToArray();
            await model.CalculateMs2Async(range, subs, token).ConfigureAwait(false);
        }
    }

    public void Clear() {
        foreach (var m in DisplayEicSettingValues) {
            m.ClearChromatogramSearchQuery();
        }
    }

    public bool VisiblePeakLabel {
        get => _visiblePeakLabel;
        set => SetProperty(ref _visiblePeakLabel, value);
    }
    private bool _visiblePeakLabel = true;

    public void DetectPeaks() {
        var detector = new PeakDetection(1, 0d);
        Chromatograms?.DetectPeaks(detector);
    }

    public void AddPeak() {
        if (RangeSelectableChromatogramModel is { SelectedRange: not null } ) {
            var range = RangeSelectableChromatogramModel.ConvertToRt(new RangeSelection(RangeSelectableChromatogramModel.SelectedRange));
            Chromatograms?.AddPeak(range.Item1, range.Item2);
        }
    }

    public void ResetPeaks() {
        Chromatograms?.ResetPeaks();
    }

    public void RemovePeak(DisplayPeakOfChromatogram peak) {
        Chromatograms?.RemovePeak(peak);
    }

    public void ExportPeaks() {
        if (Chromatograms is null) {
            return;
        }
        string filePath = string.Empty;
        var request = new SaveFileNameRequest(f => filePath = f)
        {
            Title = "Save peaks as table",
            Filter = "tab separated values|*.txt|All|*",
            RestoreDirectory = true,
            AddExtension = true,
        };
        _broker.Publish(request);
        if (request.Result == true && Directory.Exists(Path.GetDirectoryName(filePath))) {
            using var writer = new StreamWriter(filePath);
            writer.WriteLine("Name\tTime\tAbundance\tArea");
            foreach (var chromatogram in Chromatograms.DisplayChromatograms) {
                foreach (var peak in chromatogram.Peaks) {
                    writer.WriteLine($"{chromatogram.Name}\t{peak.Time}\t{peak.Intensity}\t{peak.Area}");
                }
            }
        }
    }

    public void SerializeLayout(NodeContainers nodeContainers) {
        if (nodeContainers is { Root: not null }) {
            var settings = Properties.Settings.Default;
            settings.ChromatogramsViewLayoutTemplate = nodeContainers.Convert();
            settings.Save();
        }
    }

    public void DeserializeLayout() {
        var settings = Properties.Settings.Default;
        if (settings.ChromatogramsViewLayoutTemplate is { } layout) {
            Layout = layout;
        }
    }
}
