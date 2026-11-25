using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    /// <summary>The automatic alignment RT correction's part of the alignment settings page.</summary>
    public sealed class AutomaticRtCorrectionSettingViewModel : ViewModelBase
    {
        private readonly AutomaticRtCorrectionSettingModel _model;

        public AutomaticRtCorrectionSettingViewModel(AutomaticRtCorrectionSettingModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            IsAvailable = model.IsAvailable;
            IsReadOnly = model.IsReadOnly;
            ReferenceChoices = model.ReferenceChoices;

            Execute = model.ToReactivePropertySlimAsSynchronized(m => m.Execute).AddTo(Disposables);
            AnchorMode = model.ToReactivePropertySlimAsSynchronized(m => m.AnchorMode).AddTo(Disposables);
            AnchorLibraryFilePath = model.ToReactivePropertySlimAsSynchronized(m => m.AnchorLibraryFilePath).AddTo(Disposables);
            AnchorPeakSelectionMode = model.ToReactivePropertySlimAsSynchronized(m => m.AnchorPeakSelectionMode).AddTo(Disposables);
            Reference = model.ToReactivePropertySlimAsSynchronized(m => m.Reference).AddTo(Disposables);
            InterpolateBlankByAnalyticalOrder = model.ToReactivePropertySlimAsSynchronized(m => m.InterpolateBlankByAnalyticalOrder).AddTo(Disposables);

            var numbers = new List<ReactiveProperty<string>>();
            ReactiveProperty<string> Float(System.Linq.Expressions.Expression<Func<AutomaticRtCorrectionSettingModel, float>> selector, double minimum, double maximum, string message) {
                var property = model.ToReactivePropertyAsSynchronized(
                    selector,
                    value => value.ToString(CultureInfo.InvariantCulture),
                    text => float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
                    ignoreValidationErrorValue: true)
                    .SetValidateNotifyError(text => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                        && !float.IsNaN(value) && !float.IsInfinity(value) && value >= minimum && value <= maximum ? null : message)
                    .AddTo(Disposables);
                numbers.Add(property);
                return property;
            }
            ReactiveProperty<string> Integer(System.Linq.Expressions.Expression<Func<AutomaticRtCorrectionSettingModel, int>> selector, int minimum, string message) {
                var property = model.ToReactivePropertyAsSynchronized(
                    selector,
                    value => value.ToString(CultureInfo.InvariantCulture),
                    text => int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
                    ignoreValidationErrorValue: true)
                    .SetValidateNotifyError(text => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= minimum ? null : message)
                    .AddTo(Disposables);
                numbers.Add(property);
                return property;
            }

            AnchorPeakSelectionRtWeight = Float(m => m.AnchorPeakSelectionRtWeight, 0d, 1d, "A number between 0 and 1.");
            LocalSupportRtWindow = Float(m => m.LocalSupportRtWindow, 0d, double.MaxValue, "A number of minutes, 0 or more.");
            MinimumAnchorCount = Integer(m => m.MinimumAnchorCount, 2, "A whole number, 2 or more.");
            MaximumAnchorCount = Integer(m => m.MaximumAnchorCount, 2, "A whole number, 2 or more.");
            RtBinWidth = Float(m => m.RtBinWidth, float.Epsilon, double.MaxValue, "A number of minutes above 0.");
            MatchRtTolerance = Float(m => m.MatchRtTolerance, float.Epsilon, double.MaxValue, "A number of minutes above 0.");
            MinimumSampleCoverage = Float(m => m.MinimumSampleCoverage, 0d, 1d, "A number between 0 and 1.");
            IntensityQuantile = Float(m => m.IntensityQuantile, 0d, 1d, "A number between 0 and 1.");
            MaximumPeakWidthQuantile = Float(m => m.MaximumPeakWidthQuantile, 0d, 1d, "A number between 0 and 1.");
            MinimumSignalToNoise = Float(m => m.MinimumSignalToNoise, 0d, double.MaxValue, "A number, 0 or more.");
            MinimumGaussianSimilarity = Float(m => m.MinimumGaussianSimilarity, 0d, 1d, "A number between 0 and 1.");
            MinimumIdealSlope = Float(m => m.MinimumIdealSlope, 0d, 1d, "A number between 0 and 1.");
            OutlierMadThreshold = Float(m => m.OutlierMadThreshold, 0d, double.MaxValue, "A number, 0 or more (0 turns the outlier test off).");
            ReferenceCentralityWeight = Float(m => m.ReferenceCentralityWeight, 0d, 1d, "A number between 0 and 1.");

            UsesLibrary = new[]
            {
                Execute,
                AnchorMode.Select(mode => mode != AutomaticRtCorrectionAnchorMode.Automatic),
            }.CombineLatestValuesAreAllTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
            UsesRtWeight = AnchorPeakSelectionMode.Select(mode => mode == RetentionTimeCorrectionPeakSelectionMode.Weighted)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            BrowseAnchorLibraryCommand = new ReactiveCommand().WithSubscribe(model.BrowseAnchorLibrary).AddTo(Disposables);

            ObserveHasErrors = numbers.Select(number => number.ObserveHasErrors).CombineLatestValuesAreAnyTrue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            ObserveChanges = new IObservable<Unit>[]
            {
                Execute.ToUnit(), AnchorMode.ToUnit(), AnchorLibraryFilePath.ToUnit(), AnchorPeakSelectionMode.ToUnit(),
                Reference.ToUnit(), InterpolateBlankByAnalyticalOrder.ToUnit(),
            }.Concat(numbers.Select(number => number.ToUnit())).Merge();
        }

        public bool IsAvailable { get; }
        public bool IsReadOnly { get; }

        public ReactivePropertySlim<bool> Execute { get; }
        public IReadOnlyList<AutomaticRtCorrectionAnchorMode> AnchorModes { get; } = new[] { AutomaticRtCorrectionAnchorMode.Automatic, AutomaticRtCorrectionAnchorMode.UserOnly, AutomaticRtCorrectionAnchorMode.Hybrid };
        public ReactivePropertySlim<AutomaticRtCorrectionAnchorMode> AnchorMode { get; }
        public ReactivePropertySlim<string> AnchorLibraryFilePath { get; }
        public ReactiveCommand BrowseAnchorLibraryCommand { get; }
        public IReadOnlyList<RetentionTimeCorrectionPeakSelectionMode> PeakSelectionModes { get; } = new[] { RetentionTimeCorrectionPeakSelectionMode.HighestIntensity, RetentionTimeCorrectionPeakSelectionMode.ClosestToReferenceRt, RetentionTimeCorrectionPeakSelectionMode.Weighted };
        public ReactivePropertySlim<RetentionTimeCorrectionPeakSelectionMode> AnchorPeakSelectionMode { get; }
        public ReactiveProperty<string> AnchorPeakSelectionRtWeight { get; }
        public ReadOnlyCollection<AutomaticRtCorrectionReferenceChoice> ReferenceChoices { get; }
        public ReactivePropertySlim<AutomaticRtCorrectionReferenceChoice> Reference { get; }
        public ReactiveProperty<string> LocalSupportRtWindow { get; }
        public ReactiveProperty<string> MinimumAnchorCount { get; }
        public ReactiveProperty<string> MaximumAnchorCount { get; }
        public ReactiveProperty<string> RtBinWidth { get; }
        public ReactiveProperty<string> MatchRtTolerance { get; }
        public ReactiveProperty<string> MinimumSampleCoverage { get; }
        public ReactiveProperty<string> IntensityQuantile { get; }
        public ReactiveProperty<string> MaximumPeakWidthQuantile { get; }
        public ReactiveProperty<string> MinimumSignalToNoise { get; }
        public ReactiveProperty<string> MinimumGaussianSimilarity { get; }
        public ReactiveProperty<string> MinimumIdealSlope { get; }
        public ReactiveProperty<string> OutlierMadThreshold { get; }
        public ReactiveProperty<string> ReferenceCentralityWeight { get; }
        public ReactivePropertySlim<bool> InterpolateBlankByAnalyticalOrder { get; }

        /// <summary>True when the anchor library is read (UserOnly or Hybrid, with the correction on).</summary>
        public ReadOnlyReactivePropertySlim<bool> UsesLibrary { get; }
        /// <summary>True when the RT weight of the anchor peak selection is read.</summary>
        public ReadOnlyReactivePropertySlim<bool> UsesRtWeight { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
        public IObservable<Unit> ObserveChanges { get; }
    }
}
