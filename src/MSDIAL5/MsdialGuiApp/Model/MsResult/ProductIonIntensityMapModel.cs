using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.MsResult;

internal sealed class ProductIonIntensityMapModel : BindableBase
{
    private readonly IObservable<ChromatogramPeakFeature?> _peak;
    private readonly LoadProductIonMapUsecase _loadPIUsecase;

    public ProductIonIntensityMapModel(IObservable<ChromatogramPeakFeature?> peak, MsSpectrumModel msSpectrumModel, LoadProductIonMapUsecase loadPIUsecase) {
        _peak = peak.Replay(1).RefCount();
        _loadPIUsecase = loadPIUsecase;
        MsSpectrumModel = msSpectrumModel;
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

    public async Task LoadIonsAsync(CancellationToken token = default) {
        if (SelectedRange is null) {
            return;
        }

        var peak = await _peak.FirstAsync();
        if (peak is null) {
            return;
        }
        (NearestIon, LoadedIons) = await _loadPIUsecase.LoadProductIonSpectraAsync(peak, SelectedRange, token).ConfigureAwait(false);
        SelectedIon = null;
    }

    public void Reset() {
        LoadedIons = null;
        NearestIon = null;
        SelectedIon = null;
        SelectedRange = null;
    }
}
