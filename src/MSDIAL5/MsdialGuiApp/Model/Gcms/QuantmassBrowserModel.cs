using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialGcMsApi.Algorithm;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Gcms;

public sealed class QuantmassBrowserModel : BindableBase
{
    private readonly AlignmentSpotSource _spotSource;
    private readonly PeakQuantCalculation _requantification;
    private readonly AnalysisFileBeanModelCollection _fileCollection;
    private readonly AlignmentFileBeanModel _alignmentFile;
    private readonly ChromatogramSerializer<ChromatogramSpotInfo> _chromatogramSpotSerializer;

    public QuantmassBrowserModel(AlignmentSpotSource spotSource, PeakQuantCalculation requantification, AnalysisFileBeanModelCollection fileCollection, AlignmentFileBeanModel alignmentFile, ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer) {
        _spotSource = spotSource;
        _requantification = requantification;
        _fileCollection = fileCollection;
        _alignmentFile = alignmentFile;
        _chromatogramSpotSerializer = chromatogramSpotSerializer;
    }

    public AlignmentSpotPropertyModelCollection Spots => _spotSource.Spots;

    public AlignmentSpotPropertyModel? SelectedSpot {
        get => _selectedSpot;
        set => SetProperty(ref _selectedSpot, value);
    }
    private AlignmentSpotPropertyModel? _selectedSpot;

    public async Task RunQuantificationAsync(CancellationToken token = default) {
        await _requantification.RecalculatePeakQuantificationAsync(_spotSource.Spots.Items.Select(s => s.innerModel).ToList(), _fileCollection.AnalysisFiles.Select(f => f.File).ToArray(), _alignmentFile.AlignmentFileBean, _chromatogramSpotSerializer, token).ConfigureAwait(false);
    }
}
