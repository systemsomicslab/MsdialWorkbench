using CompMs.App.Msdial.Model.Chart;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Globalization;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart;

/// <summary>
/// One editable row of the EIC list. Text is written to the model as soon as it is a valid number,
/// so what is shown is what will be drawn on Apply.
/// </summary>
internal sealed class EicQueryViewModel : ViewModelBase
{
    public EicQueryViewModel(EicQueryModel model) {
        Model = model ?? throw new ArgumentNullException(nameof(model));

        Title = new ReactivePropertySlim<string>(model.Title).AddTo(Disposables);
        Title.Subscribe(t => model.Title = t).AddTo(Disposables);

        Mass = new ReactiveProperty<string>(Format(model.Mass), ReactivePropertyMode.Default | ReactivePropertyMode.IgnoreInitialValidationError)
            .SetValidateNotifyError(ValidateMass)
            .AddTo(Disposables);
        MassTolerance = new ReactiveProperty<string>(Format(model.MassTolerance), ReactivePropertyMode.Default | ReactivePropertyMode.IgnoreInitialValidationError)
            .SetValidateNotifyError(ValidateMassTolerance)
            .AddTo(Disposables);

        Mass.Subscribe(t => {
            if (TryParse(t, out var value)) {
                model.Mass = value;
            }
            MassTolerance.ForceValidate();
        }).AddTo(Disposables);
        MassTolerance.Subscribe(t => {
            if (TryParse(t, out var value)) {
                model.MassTolerance = value;
            }
        }).AddTo(Disposables);
        Title.Subscribe(_ => Mass.ForceValidate()).AddTo(Disposables);

        // Values set on the model (e.g. by pasting) show up in the text boxes.
        model.ObserveProperty(m => m.Title, isPushCurrentValueAtFirst: false)
            .Where(t => t != Title.Value)
            .Subscribe(t => Title.Value = t)
            .AddTo(Disposables);
        model.ObserveProperty(m => m.Mass, isPushCurrentValueAtFirst: false)
            .Where(v => !TryParse(Mass.Value, out var current) || current != v)
            .Subscribe(v => Mass.Value = Format(v))
            .AddTo(Disposables);
        model.ObserveProperty(m => m.MassTolerance, isPushCurrentValueAtFirst: false)
            .Where(v => !TryParse(MassTolerance.Value, out var current) || current != v)
            .Subscribe(v => MassTolerance.Value = Format(v))
            .AddTo(Disposables);

        IsEmpty = model.ObserveProperty(m => m.IsEmpty).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

        HasErrors = new[]
        {
            Mass.ObserveHasErrors,
            MassTolerance.ObserveHasErrors,
        }.CombineLatestValuesAreAllFalse()
        .Inverse()
        .ToReadOnlyReactivePropertySlim()
        .AddTo(Disposables);
    }

    public EicQueryModel Model { get; }

    public ReactivePropertySlim<string> Title { get; }
    public ReactiveProperty<string> Mass { get; }
    public ReactiveProperty<string> MassTolerance { get; }

    public ReadOnlyReactivePropertySlim<bool> IsEmpty { get; }
    public ReadOnlyReactivePropertySlim<bool> HasErrors { get; }

    private string? ValidateMass(string? text) {
        if (string.IsNullOrWhiteSpace(text)) {
            return string.IsNullOrWhiteSpace(Title?.Value) ? null : "Enter an m/z for this row.";
        }
        return TryParse(text, out var value) && value > 0d ? null : "Enter a positive number.";
    }

    private string? ValidateMassTolerance(string? text) {
        if (string.IsNullOrWhiteSpace(text)) {
            return string.IsNullOrWhiteSpace(Mass?.Value) ? null : "Enter a mass tolerance.";
        }
        return TryParse(text, out var value) && value > 0d ? null : "Enter a positive number.";
    }

    // Empty text means "no value"; anything else must be a number.
    internal static bool TryParse(string? text, out double? value) {
        if (string.IsNullOrWhiteSpace(text)) {
            value = null;
            return true;
        }
        if (double.TryParse(text!.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) {
            value = v;
            return true;
        }
        value = null;
        return false;
    }

    private static string Format(double? value) {
        return value is double v ? v.ToString(CultureInfo.InvariantCulture) : string.Empty;
    }
}
