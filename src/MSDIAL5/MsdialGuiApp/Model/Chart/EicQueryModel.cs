using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.Chart;

/// <summary>
/// One row of the "Display chromatograms" EIC list: a name, an m/z and its tolerance.
/// A row with neither a name nor an m/z is an empty row and is not drawn.
/// </summary>
internal sealed class EicQueryModel : BindableBase
{
    public const double DefaultMassTolerance = 0.01d;

    public EicQueryModel(string? title = null, double? mass = null, double? massTolerance = null) {
        _title = title ?? string.Empty;
        _mass = mass;
        _massTolerance = massTolerance;
    }

    public string Title {
        get => _title;
        set {
            if (SetProperty(ref _title, value ?? string.Empty)) {
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }
    private string _title;

    public double? Mass {
        get => _mass;
        set {
            if (SetProperty(ref _mass, value)) {
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsValid));
            }
        }
    }
    private double? _mass;

    public double? MassTolerance {
        get => _massTolerance;
        set {
            if (SetProperty(ref _massTolerance, value)) {
                OnPropertyChanged(nameof(IsValid));
            }
        }
    }
    private double? _massTolerance;

    public bool IsEmpty => string.IsNullOrWhiteSpace(Title) && Mass is null;

    public bool IsValid => Mass > 0d && MassTolerance > 0d;

    public PeakFeatureSearchValue ToSearchValue() {
        return new PeakFeatureSearchValue
        {
            Title = Title,
            Mass = Mass ?? 0d,
            MassTolerance = MassTolerance ?? 0d,
        };
    }

    public static EicQueryModel From(PeakFeatureSearchValue value) {
        return new EicQueryModel(value.Title, value.Mass, value.MassTolerance);
    }
}
