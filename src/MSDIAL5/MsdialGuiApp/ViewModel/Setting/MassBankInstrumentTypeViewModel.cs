using CompMs.App.Msdial.Utility;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    /// <summary>One separation choice of AC$INSTRUMENT_TYPE; <see cref="Value"/> is empty for no separation.</summary>
    internal sealed class MassBankSeparationChoice
    {
        public MassBankSeparationChoice(string value, string label) {
            Value = value;
            Label = label;
        }

        /// <summary>The prefix written in AC$INSTRUMENT_TYPE, "LC", "GC" or "CE"; empty for none.</summary>
        public string Value { get; }

        public string Label { get; }

        public override string ToString() => Label;
    }

    /// <summary>
    /// Composes AC$INSTRUMENT_TYPE from three choices (separation, ionisation, analyzer) taken from MassBank's
    /// vocabulary, and writes the result to an instrument type property; a valid value in that property selects
    /// the choices again.
    /// </summary>
    /// <remarks>
    /// A value that is not a MassBank instrument type selects nothing and is left as it is until an ionisation and
    /// an analyzer are chosen: it is never mapped to a guess. A valid analyzer that is not among
    /// <see cref="MassBankVocabulary.AnalyzerPresets"/> (e.g. "QQTOF") is added to <see cref="Analyzers"/> so that
    /// it can stay selected.
    ///
    /// <paramref name="isRequired"/> decides whether a missing ionisation or analyzer is a validation error: true
    /// in Project property setting when the project is constrained to MassBank format, never in the project
    /// wizard, where the choices only help to fill the text box.
    /// </remarks>
    internal sealed class MassBankInstrumentTypeViewModel : ViewModelBase
    {
        internal const string IonisationRequired = "Choose the ionisation.";
        internal const string AnalyzerRequired = "Choose the analyzer.";
        internal const string AsEntered = "as entered";

        private static readonly IReadOnlyList<MassBankSeparationChoice> SEPARATIONS =
            new[] { new MassBankSeparationChoice(string.Empty, "(none)"), }
            .Concat(MassBankVocabulary.Separations.Select(s => new MassBankSeparationChoice(s, s)))
            .ToArray();

        private readonly IReactiveProperty<string> _instrumentType;
        private MassBankAnalyzerPreset? _enteredAnalyzer;
        private bool _synchronizing;

        public MassBankInstrumentTypeViewModel(IReactiveProperty<string> instrumentType, IObservable<bool> isRequired) {
            _instrumentType = instrumentType ?? throw new ArgumentNullException(nameof(instrumentType));
            if (isRequired is null) {
                throw new ArgumentNullException(nameof(isRequired));
            }

            Analyzers = new ObservableCollection<MassBankAnalyzerPreset>(MassBankVocabulary.AnalyzerPresets);
            IsRequired = isRequired.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            Separation = new ReactivePropertySlim<string>(string.Empty).AddTo(Disposables);
            Ionisation = new ReactiveProperty<string?>((string?)null)
                .SetValidateNotifyError(v => IsRequired.Value && string.IsNullOrEmpty(v) ? IonisationRequired : null)
                .AddTo(Disposables);
            Analyzer = new ReactiveProperty<MassBankAnalyzerPreset?>((MassBankAnalyzerPreset?)null)
                .SetValidateNotifyError(v => IsRequired.Value && v is null ? AnalyzerRequired : null)
                .AddTo(Disposables);

            instrumentType.Subscribe(SelectFrom).AddTo(Disposables);
            new[] { Separation.ToUnit(), Ionisation.ToUnit(), Analyzer.ToUnit(), }
                .Merge()
                .Where(_ => !_synchronizing)
                .Subscribe(_ => Compose())
                .AddTo(Disposables);
            IsRequired.Subscribe(_ => {
                Ionisation.ForceValidate();
                Analyzer.ForceValidate();
            }).AddTo(Disposables);

            IsValid = instrumentType.Select(MassBankVocabulary.IsValidInstrumentType)
                .ToReadOnlyReactivePropertySlim(MassBankVocabulary.IsValidInstrumentType(instrumentType.Value))
                .AddTo(Disposables);
            Preview = instrumentType.Select(DescribeValue)
                .ToReadOnlyReactivePropertySlim(DescribeValue(instrumentType.Value))
                .AddTo(Disposables);
            ObserveHasErrors = new[] { Ionisation.ObserveHasErrors, Analyzer.ObserveHasErrors, }
                .CombineLatestValuesAreAnyTrue()
                .ToReadOnlyReactivePropertySlim(Ionisation.HasErrors || Analyzer.HasErrors)
                .AddTo(Disposables);
        }

        /// <summary>No separation, then <see cref="MassBankVocabulary.Separations"/>.</summary>
        public IReadOnlyList<MassBankSeparationChoice> Separations => SEPARATIONS;

        public IReadOnlyList<string> Ionisations => MassBankVocabulary.Ionisations;

        /// <summary><see cref="MassBankVocabulary.AnalyzerPresets"/>, and the analyzer of a valid value that is not one of them.</summary>
        public ObservableCollection<MassBankAnalyzerPreset> Analyzers { get; }

        /// <summary>The value of the chosen <see cref="MassBankSeparationChoice"/>: empty for none, else "LC", "GC" or "CE".</summary>
        public ReactivePropertySlim<string> Separation { get; }

        public ReactiveProperty<string?> Ionisation { get; }

        public ReactiveProperty<MassBankAnalyzerPreset?> Analyzer { get; }

        public ReadOnlyReactivePropertySlim<bool> IsRequired { get; }

        /// <summary>True when the instrument type property holds a MassBank instrument type.</summary>
        public ReadOnlyReactivePropertySlim<bool> IsValid { get; }

        /// <summary>The instrument type a record would carry, or why it would not be valid.</summary>
        public ReadOnlyReactivePropertySlim<string> Preview { get; }

        /// <summary>True while a required ionisation or analyzer is missing.</summary>
        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        /// <summary>The problem with <paramref name="value"/> as AC$INSTRUMENT_TYPE; null when it is valid.</summary>
        public static string? Validate(string? value) {
            if (MassBankVocabulary.IsValidInstrumentType(value)) {
                return null;
            }
            return string.IsNullOrEmpty(value)
                ? "Choose the ionisation and the analyzer (the separation may be none)."
                : $"\"{value}\" is not a MassBank instrument type. Choose the separation, ionisation and analyzer.";
        }

        private static string DescribeValue(string? value) {
            return Validate(value) ?? $"AC$INSTRUMENT_TYPE: {value}";
        }

        private void SelectFrom(string? value) {
            _synchronizing = true;
            try {
                if (MassBankVocabulary.TryParseInstrumentType(value, out var separation, out var ionisation, out var analyzer)) {
                    Separation.Value = separation;
                    Ionisation.Value = ionisation;
                    Analyzer.Value = FindOrAddAnalyzer(analyzer);
                }
                else {
                    Separation.Value = string.Empty;
                    Ionisation.Value = null;
                    Analyzer.Value = null;
                }
            }
            finally {
                _synchronizing = false;
            }
        }

        private void Compose() {
            var ionisation = Ionisation.Value;
            var analyzer = Analyzer.Value;
            if (string.IsNullOrEmpty(ionisation) || analyzer is null) {
                return;
            }
            var composed = MassBankVocabulary.ComposeInstrumentType(Separation.Value, ionisation, analyzer.Analyzer);
            if (_instrumentType.Value != composed) {
                _instrumentType.Value = composed;
            }
        }

        private MassBankAnalyzerPreset FindOrAddAnalyzer(string analyzer) {
            var preset = Analyzers.FirstOrDefault(p => p.Analyzer == analyzer);
            if (preset is not null) {
                return preset;
            }
            if (_enteredAnalyzer is not null) {
                Analyzers.Remove(_enteredAnalyzer);
            }
            _enteredAnalyzer = new MassBankAnalyzerPreset(analyzer, AsEntered);
            Analyzers.Add(_enteredAnalyzer);
            return _enteredAnalyzer;
        }
    }
}
