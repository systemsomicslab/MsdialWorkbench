using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.MsResult;

internal sealed class ProductIonIntensityMapModel : BindableBase
{
    private readonly IObservable<ChromatogramPeakFeature?> _peak;
    private readonly LoadProductIonMapUsecase _loadPIUsecase;
    private readonly BusyNotifier _busyNotifier;

    public ProductIonIntensityMapModel(IObservable<ChromatogramPeakFeature?> peak, MsSpectrumModel msSpectrumModel, LoadProductIonMapUsecase loadPIUsecase) {
        _peak = peak.Replay(1).RefCount();
        _loadPIUsecase = loadPIUsecase;
        MsSpectrumModel = msSpectrumModel;
        _busyNotifier = new BusyNotifier();
    }

    public MzRange? SelectedRange {
        get => _selectedRange;
        set => SetProperty(ref _selectedRange, value);
    }
    private MzRange? _selectedRange;

    public List<MappedIon>? LoadedIons {
        get => _loadedIons;
        set => SetProperty(ref _loadedIons, value);
    }
    private List<MappedIon>? _loadedIons;

    public MappedIon? NearestIon {
        get => _nearestIon;
        set => SetProperty(ref _nearestIon, value);
    }
    private MappedIon? _nearestIon;

    public MappedIon? SelectedIon {
        get => _selectedIon;
        set => SetProperty(ref _selectedIon, value);
    }
    private MappedIon? _selectedIon;

    public MsSpectrumModel MsSpectrumModel { get; }

    public IObservable<bool> IsBusy => _busyNotifier.AsObservable();

    public async Task LoadIonsAsync(CancellationToken token = default) {
        if (SelectedRange is null) {
            return;
        }

        using (_busyNotifier.ProcessStart()) {

            token.ThrowIfCancellationRequested();
            var peak = await _peak.FirstAsync();
            if (peak is null) {
                return;
            }

            token.ThrowIfCancellationRequested();
            var results = await _loadPIUsecase.LoadProductIonSpectraAsync(peak, SelectedRange, token).ConfigureAwait(false);

            (NearestIon, LoadedIons) = results;
            SelectedIon = null;
        }
    }

    public async Task WriteIonsAsync(Stream stream, CancellationToken token = default) {
        using var writer = new StreamWriter(stream) {
            AutoFlush = true
        };

        var sb = new StringBuilder();
        foreach (var ion in LoadedIons ?? []) {
            sb.AppendLine($"{ion.ID},{ion.Time},{ion.ExperimentID},{ion.Mz},{ion.Intensity}");
        }

        token.ThrowIfCancellationRequested();
        await writer.WriteLineAsync("CycleID,ScanTime,ExperimentID,Mz,Intensity").ConfigureAwait(false);
        await writer.WriteAsync(sb.ToString());
    }

    public void Reset() {
        LoadedIons = null;
        NearestIon = null;
        SelectedIon = null;
        SelectedRange = null;
    }
}
