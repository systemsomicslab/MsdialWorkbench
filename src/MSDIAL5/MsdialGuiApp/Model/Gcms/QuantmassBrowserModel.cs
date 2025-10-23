using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Gcms;

public sealed class QuantmassBrowserModel : BindableBase
{
    private readonly AlignmentSpotSource _spotSource;

    public QuantmassBrowserModel(AlignmentSpotSource spotSource) {
        _spotSource = spotSource;
    }

    public AlignmentSpotPropertyModelCollection Spots => _spotSource.Spots;

    public AlignmentSpotPropertyModel? SelectedSpot {
        get => _selectedSpot;
        set => SetProperty(ref _selectedSpot, value);
    }
    private AlignmentSpotPropertyModel? _selectedSpot;
}
