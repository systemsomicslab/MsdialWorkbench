using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Export;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.MsResult;

internal sealed class AccumulatedMs2SpectrumModel : DisposableModelBase
{
    private readonly AccumulateSpectraUsecase _accumulateSpectra;
    private readonly ICompoundSearchUsecase<ICompoundResult, PeakSpotModel> _compoundSearch;
    private readonly IWholeChromatogramLoader<(MzRange, MzRange)> _productIonChromatogramLoader;
    private readonly BehaviorSubject<MsSpectrum?> _subject;

    public AccumulatedMs2SpectrumModel(DisplayExtractedIonChromatogram chromatogram, AccumulateSpectraUsecase accumulateSpectra, ICompoundSearchUsecase<ICompoundResult, PeakSpotModel> compoundSearch, IWholeChromatogramLoader<(MzRange, MzRange)> productIonChromatogramLoader) {
        Chromatogram = chromatogram;
        _accumulateSpectra = accumulateSpectra;
        _compoundSearch = compoundSearch;
        _productIonChromatogramLoader = productIonChromatogramLoader;
        _plotDisposable = new SerialDisposable().AddTo(Disposables);
        var subject = new BehaviorSubject<MsSpectrum?>(null).AddTo(Disposables);
        _subject = subject;

        PropertySelector<SpectrumPeak, double> horizontalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Mass);
        PropertySelector<SpectrumPeak, double> verticalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity);
        var graphLabels = new GraphLabels($"Accumulated MS/MS m/z: {chromatogram.Mz}±{chromatogram.Tolerance}", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
        ChartHueItem hueItem = new ChartHueItem(nameof(SpectrumPeak.SpectrumComment), new ConstantBrushMapper(Brushes.Black));
        var spectrumox = new ObservableMsSpectrum(subject, null, Observable.Return<ISpectraExporter?>(null)).AddTo(Disposables);
        ChartSpectrumModel = new SingleSpectrumModel(spectrumox, spectrumox.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), spectrumox.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), hueItem, graphLabels).AddTo(Disposables);
    }

    public DisplayExtractedIonChromatogram Chromatogram { get; }

    public SingleSpectrumModel ChartSpectrumModel { get; }

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

    public ChromatogramsModel? ProductIonChromatogram {
        get => _productIonChromatogram;
        private set => SetProperty(ref _productIonChromatogram, value);
    }
    private ChromatogramsModel? _productIonChromatogram;

    public async Task CalculateMs2Async((double start, double end) baseRange, IEnumerable<(double start, double end)> subtracts, CancellationToken token = default) {
        Scan = await _accumulateSpectra.AccumulateMs2Async(Chromatogram.Mz, Chromatogram.Tolerance, baseRange, subtracts, token).ConfigureAwait(false);
        var anotatedSpot = new AnnotatedSpotModel(
            Chromatogram.DetectPeak(baseRange.start, baseRange.end),
            new MoleculeProperty());
        PeakSpot = new PeakSpotModel(anotatedSpot, Scan);
        PlotComparedSpectrum = new PlotComparedMsSpectrumUsecase(Scan);
    }

    public void CalculateProductIonChromatogram() {
        var axis = ChartSpectrumModel.HorizontalPropertySelectors.AxisItemSelector.SelectedAxisItem.AxisManager;
        var (start, end) = new RangeSelection(SelectedRange).ConvertBy(axis);
        var range = MzRange.FromRange(start, end);

        var chromatogram = _productIonChromatogramLoader.LoadChromatogram((new MzRange(Chromatogram.Mz, Chromatogram.Tolerance), range));
        var displayChromatogram = new DisplayChromatogram(chromatogram, title: $"Precursor m/z: {Chromatogram.Mz}±{Chromatogram.Tolerance}, Product ion: {start}-{end}");
        ProductIonChromatogram = new ChromatogramsModel(string.Empty, displayChromatogram, displayChromatogram.Name, "Time", "Abundance");
    }

    public void SearchCompound() {
        if (PeakSpot is null) {
            return;
        }
        Compounds = _compoundSearch.Search(PeakSpot);
    }
}
