using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Export;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.MsResult;

internal sealed class AccumulatedMs2SpectrumModel : DisposableModelBase
{
    private readonly AccumulateSpectraUsecase _accumulateSpectra;
    private readonly ICompoundSearchUsecase<ICompoundResult, PeakSpotModel> _compoundSearch;
    private readonly IWholeChromatogramLoader<(MzRange, MzRange)> _productIonChromatogramLoader;
    private readonly IMessageBroker _broker;
    private readonly BehaviorSubject<MsSpectrum?> _subject;

    public AccumulatedMs2SpectrumModel(DisplayExtractedIonChromatogram chromatogram, AccumulateSpectraUsecase accumulateSpectra, ICompoundSearchUsecase<ICompoundResult, PeakSpotModel> compoundSearch, IWholeChromatogramLoader<(MzRange, MzRange)> productIonChromatogramLoader, IMessageBroker broker) {
        Chromatogram = chromatogram;
        _accumulateSpectra = accumulateSpectra;
        _compoundSearch = compoundSearch;
        _productIonChromatogramLoader = productIonChromatogramLoader;
        _broker = broker;
        _plotDisposable = new SerialDisposable().AddTo(Disposables);
        _subject = new BehaviorSubject<MsSpectrum?>(null).AddTo(Disposables);
    }

    public DisplayExtractedIonChromatogram Chromatogram { get; }

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

    public AxisRange? ProductIonRange {
        get => _productIonRange;
        set => SetProperty(ref _productIonRange, value);
    }
    private AxisRange? _productIonRange;

    public async Task CalculateMs2Async((double start, double end) baseRange, IEnumerable<(double start, double end)> subtracts, CancellationToken token = default) {
        Scan = await _accumulateSpectra.AccumulateMs2Async(Chromatogram.Mz, Chromatogram.Tolerance, baseRange, subtracts, token).ConfigureAwait(false);
        var anotatedSpot = new AnnotatedSpotModel(
            Chromatogram.DetectPeak(baseRange.start, baseRange.end),
            new MoleculeProperty());
        PeakSpot = new PeakSpotModel(anotatedSpot, Scan);
        PlotComparedSpectrum = new PlotComparedMsSpectrumUsecase(Scan);

        _compoundSearch.Search(PeakSpot);
        CalculateProductIonTotalIonChromatogram();
    }

    public void CalculateProductIonTotalIonChromatogram() {
        if (PlotComparedSpectrum is null) {
            return;
        }
        var range = MzRange.FromRange(0d, double.MaxValue);

        var displayChromatogram = _productIonChromatogramLoader.LoadChromatogram((new MzRange(Chromatogram.Mz, Chromatogram.Tolerance), range));
        displayChromatogram.Name = $"TIC Precursor m/z: {Chromatogram.Mz}±{Chromatogram.Tolerance}";
        ProductIonChromatogram = new ChromatogramsModel(string.Empty, displayChromatogram, displayChromatogram.Name, "Time", "Abundance");
    }

    public void CalculateProductIonChromatogram() {
        if (PlotComparedSpectrum is null) {
            return;
        }
        var axis = PlotComparedSpectrum.MsSpectrumModel.UpperSpectrumModel.HorizontalPropertySelectors.AxisItemSelector.SelectedAxisItem.AxisManager;
        var (start, end) = new RangeSelection(SelectedRange).ConvertBy(axis);
        var range = MzRange.FromRange(start, end);

        var displayChromatogram = _productIonChromatogramLoader.LoadChromatogram((new MzRange(Chromatogram.Mz, Chromatogram.Tolerance), range));
        displayChromatogram.Name = $"Precursor m/z: {Chromatogram.Mz}±{Chromatogram.Tolerance}, Product ion: {start}-{end}";
        ProductIonChromatogram = new ChromatogramsModel(string.Empty, displayChromatogram, displayChromatogram.Name, "Time", "Abundance");
    }

    public void DetectPeaks() {
        var detector = new PeakDetection(1, 0d);
        ProductIonChromatogram?.DetectPeaks(detector);
    }

    public void AddPeak() {
        if (ProductIonRange is not null && ProductIonChromatogram is not null) {
            var range = new RangeSelection(ProductIonRange).ConvertBy(ProductIonChromatogram.ChromAxisItemSelector.SelectedAxisItem.AxisManager);
            ProductIonChromatogram?.AddPeak(range.Item1, range.Item2);
            ProductIonRange = null;
        }
    }

    public void ResetPeaks() {
        ProductIonChromatogram?.ResetPeaks();
    }

    public void SearchCompound() {
        if (PeakSpot is null) {
            return;
        }
        Compounds = _compoundSearch.Search(PeakSpot);
    }

    public void Export() {
        if (PeakSpot is null || Scan is null) {
            return;
        }
        var builder = new NistRecordBuilder();
        builder.SetIonPropertyProperties(PeakSpot.PeakSpot.MSIon);
        builder.SetMSProperties(PeakSpot.PeakSpot.MSIon);
        builder.SetScan(Scan);
        var request = new SaveFileNameRequest(filename => {
            using var stream = File.Open(filename, FileMode.Create);
            builder.Export(stream);
        });
        _broker.Publish(request);
    }
}
