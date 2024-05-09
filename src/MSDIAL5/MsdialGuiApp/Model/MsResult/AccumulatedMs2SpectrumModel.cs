using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.UI;
using CompMs.MsdialCore.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.MsResult;

internal sealed class AccumulatedMs2SpectrumModel : DisposableModelBase
{
    private readonly AccumulateSpectraUsecase _accumulateSpectra;
    private readonly MsScanCompoundSearchUsecase _compoundSearch;
    private readonly LoadChromatogramsUsecase _loadingChromatograms;
    private readonly IMessageBroker _broker;
    private readonly BehaviorSubject<MsSpectrum?> _subject;

    public AccumulatedMs2SpectrumModel(AccumulateSpectraUsecase accumulateSpectra, MsScanCompoundSearchUsecase compoundSearch, LoadChromatogramsUsecase loadingChromatograms, AnalysisFileBeanModel fileModel, IMessageBroker broker) {
        _subject = new BehaviorSubject<MsSpectrum?>(null).AddTo(Disposables);
        _plotDisposable = new SerialDisposable().AddTo(Disposables);
        _accumulateSpectra = accumulateSpectra;
        _compoundSearch = compoundSearch;
        _loadingChromatograms = loadingChromatograms;
        FileModel = fileModel;
        _broker = broker;
        SearchParameter = compoundSearch.ObserveProperty(m => m.SearchParameter).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

        var settings = Properties.Settings.Default;
        if (settings.AccumulatedSpectrumViewLayoutTemplate is { } layout) {
            _layout = layout;
        }
        else {
            _layout = new ContainerElement
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Items = [
                    new LeafElement { Width = new(2, System.Windows.GridUnitType.Star), Size = 1, Priorities = [3] },
                    new ContainerElement {
                        Orientation = System.Windows.Controls.Orientation.Vertical,
                        Width = new(3, System.Windows.GridUnitType.Star),
                        Items = [
                            new LeafElement { Height = new(1, System.Windows.GridUnitType.Star), Size = 1, Priorities = [2] },
                            new LeafElement { Height = new(1, System.Windows.GridUnitType.Star), Size = 1, Priorities = [1] },
                        ]
                    },
                ],
            };
        }
    }

    public MSScanProperty? Scan {
        get => _scan;
        private set {
            if (SetProperty(ref _scan, value)) {
                if (_scan is not null) {
                    _subject.OnNext(new MsSpectrum(_scan.Spectrum));
                }
                else {
                    _subject.OnNext(null);
                }
            }
        }
    }
    private MSScanProperty? _scan;

    public PeakSpotModel? PeakSpot {
        get => _peakSpot;
        private set => SetProperty(ref _peakSpot, value);
    }
    private PeakSpotModel? _peakSpot;

    public PlotComparedMsSpectrumUsecase? PlotComparedSpectrum {
        get => _plotComparedSpectrum;
        private set {
            if (SetProperty(ref _plotComparedSpectrum, value)) {
                _plotDisposable.Disposable = value;
            }
        }
    }
    private PlotComparedMsSpectrumUsecase? _plotComparedSpectrum;
    private SerialDisposable _plotDisposable;

    public IReadOnlyList<ICompoundResult>? Compounds {
        get => _compounds;
        private set => SetProperty(ref _compounds, value);
    }
    private IReadOnlyList<ICompoundResult>? _compounds;

    public ICompoundResult? SelectedCompound {
        get => _selectedCompound;
        set {
            if (SetProperty(ref _selectedCompound, value)) {
                PlotComparedSpectrum?.UpdateReference(value?.MsReference);
            }
        }
    }
    private ICompoundResult? _selectedCompound;

    public AxisRange? SelectedRange {
        get => _selectedRange;
        set => SetProperty(ref _selectedRange, value);
    }
    private AxisRange? _selectedRange;

    public ChromatogramsModel? ExtractedIonChromatogram {
        get => _extractedIonChromatogram;
        private set => SetProperty(ref _extractedIonChromatogram, value);
    }
    private ChromatogramsModel? _extractedIonChromatogram;

    public AxisRange? ExtractIonRange {
        get => _productIonRange;
        set => SetProperty(ref _productIonRange, value);
    }
    private AxisRange? _productIonRange;

    public IList SearchMethods => _compoundSearch.SearchMethods;

    public object? SearchMethod {
        get => _compoundSearch.SearchMethod;
        set {
            if (_compoundSearch.SearchMethod != value) {
                _compoundSearch.SearchMethod = value;
                OnPropertyChanged(nameof(SearchMethod));
            }
        }
    }

    public ReadOnlyReactivePropertySlim<MsRefSearchParameterBase?> SearchParameter { get; }

    public IDockLayoutElement Layout {
        get => _layout;
        set => SetProperty(ref _layout, value);
    }
    public AnalysisFileBeanModel FileModel { get; }

    private IDockLayoutElement _layout;

    public async Task CalculateMs2Async((double start, double end) baseRange, IEnumerable<(double start, double end)> subtracts, CancellationToken token = default) {
        Scan = await _accumulateSpectra.AccumulateMs2Async(baseRange, subtracts, token).ConfigureAwait(false);
        PlotComparedSpectrum = new PlotComparedMsSpectrumUsecase(Scan);
        CalculateTotalIonChromatogram();
    }

    public void CalculateTotalIonChromatogram() {
        ExtractedIonChromatogram = _loadingChromatograms.LoadMS2Tic();
        if (ExtractedIonChromatogram.AbundanceAxisItemSelector.SelectedAxisItem.AxisManager is BaseAxisManager<double> chromAxis) {
            chromAxis.ChartMargin = new ConstantMargin(0, 60);
        }
    }

    public void CalculateExtractedIonChromatogram() {
        if (PlotComparedSpectrum is null || SelectedRange is null) {
            return;
        }
        var axis = PlotComparedSpectrum.MsSpectrumModel.UpperSpectrumModel.HorizontalPropertySelectors.AxisItemSelector.SelectedAxisItem.AxisManager;
        var (start, end) = new RangeSelection(SelectedRange).ConvertBy(axis);
        var range = MzRange.FromRange(start, end);
        ExtractedIonChromatogram = _loadingChromatograms.LoadMS2Eic(range);
        if (ExtractedIonChromatogram.AbundanceAxisItemSelector.SelectedAxisItem.AxisManager is BaseAxisManager<double> chromAxis) {
            chromAxis.ChartMargin = new ConstantMargin(0, 60);
        }
    }

    public void DetectPeaks() {
        var detector = new PeakDetection(1, 0d);
        ExtractedIonChromatogram?.DetectPeaks(detector);
    }

    public void AddPeak() {
        if (ExtractIonRange is not null && ExtractedIonChromatogram is not null) {
            var range = new RangeSelection(ExtractIonRange).ConvertBy(ExtractedIonChromatogram.ChromAxisItemSelector.SelectedAxisItem.AxisManager);
            ExtractedIonChromatogram?.AddPeak(range.Item1, range.Item2);
            ExtractIonRange = null;
        }
    }

    public void ResetPeaks() {
        ExtractedIonChromatogram?.ResetPeaks();
    }

    public void SearchCompound() {
        if (Scan is null) {
            return;
        }
        Compounds = _compoundSearch.Search(Scan);
    }

    public void ImportDatabase() {
        var request = new OpenFileRequest(filename => {
            if (File.Exists(filename)) {
                _compoundSearch.AddDataBase(filename);
            }
        })
        {
            Title = "Load nist format database",
            Filter = "NIST format|*.msp",
            RestoreDirectory = true,
        };
        _broker.Publish(request);
    }

    public void Export() {
        if (Scan is null) {
            return;
        }
        var builder = new NistRecordBuilder();
        builder.SetScan(Scan);
        var request = new SaveFileNameRequest(filename => {
            using var stream = File.Open(filename, FileMode.Create);
            builder.Export(stream);
        })
        {
            Title = "Save spectrum",
            Filter = "NIST format|*.msp|All|*",
            RestoreDirectory = true,
            AddExtension = true,
        };
        _broker.Publish(request);
    }

    public void ExportCompounds() {
        if (Compounds is null || Compounds.Count == 0) {
            return;
        }
        var request = new SaveFileNameRequest(filename => {
            using var stream = File.Open(filename, FileMode.Create);
            using var writer = new StreamWriter(stream, Encoding.ASCII, 4096, true);
            writer.WriteLine("Name,Retention time,Weighted dot product,Reverse dot product,Total score,Comment");
            foreach (var compound in Compounds) {
                var reference = compound.MsReference;
                var match = compound.MatchResult;
                writer.WriteLine($"{reference.Name},{reference.ChromXs.RT.Value},{match.WeightedDotProduct},{match.ReverseDotProduct},{match.TotalScore},'{reference.Comment}'");
            }
        })
        {
            Title = "Save compounds",
            Filter = "Comma separated values|*.csv|All|*",
            RestoreDirectory = true,
            AddExtension = true,
        };
        _broker.Publish(request);
    }

    public void SerializeLayout(NodeContainers nodeContainers) {
        if (nodeContainers is { Root: not null }) {
            var settings = Properties.Settings.Default;
            settings.AccumulatedSpectrumViewLayoutTemplate = nodeContainers.Convert();
            settings.Save();
        }
    }

    public void DeserializeLayout() {
        var settings = Properties.Settings.Default;
        if (settings.AccumulatedSpectrumViewLayoutTemplate is { } layout) {
            Layout = layout;
        }
    }
}
