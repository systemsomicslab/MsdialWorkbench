using CompMs.Common.Enum;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    public class PeakPickBaseParameterViewModel : ViewModelBase
    {
        private readonly PeakPickBaseParameter model;

        public ReactivePropertySlim<SmoothingMethod> SmoothingMethod { get; }

        [Required(ErrorMessage = "Smoothing level required.")]
        [RegularExpression("[1-9][0-9]*", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> SmoothingLevel { get; }

        [Required(ErrorMessage = "Minimum amplitude required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> MinimumAmplitude { get; }

        [Required(ErrorMessage = "Minimum datapoints required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> MinimumDatapoints { get; }

        [Required(ErrorMessage = "Mass slicing width required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> MassSliceWidth { get; }

        [Required(ErrorMessage = "Retention time begin required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> RetentionTimeBegin { get; }

        [Required(ErrorMessage = "Retention time end required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> RetentionTimeEnd { get; }

        [Required(ErrorMessage = "Ms1 range begin required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> MassRangeBegin { get; }

        [Required(ErrorMessage = "Ms1 range end required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> MassRangeEnd { get; }

        [Required(ErrorMessage = "Ms2 range begin required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> Ms2MassRangeBegin { get; }

        [Required(ErrorMessage = "Ms2 range end required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> Ms2MassRangeEnd { get; }

        [Required(ErrorMessage = "Ms1 centroid tolerance required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> CentroidMs1Tolerance { get; }

        [Required(ErrorMessage = "Ms2 centroid tolerance required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> CentroidMs2Tolerance { get; }

        [Required(ErrorMessage = "Max charge number required.")]
        [RegularExpression("[1-9]+[0-9]*", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> MaxChargeNumber { get; }

        public ReactivePropertySlim<bool> IsBrClConsideredForIsotopes { get; }

        public ObservableCollection<MzSearchQueryVM> ExcludedMassList { get; }

        public Subject<Unit> CommitTrigger { get; } = new Subject<Unit>();

        public ReadOnlyReactivePropertySlim<bool> HasErrors { get; }

        public IObservable<Unit> CommitAsObservable => CommitTrigger.Where(_ => !HasErrors.Value);

        public PeakPickBaseParameterViewModel(PeakPickBaseParameter model) {
            this.model = model;

            SmoothingMethod = new ReactivePropertySlim<SmoothingMethod>(this.model.SmoothingMethod)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => SmoothingMethod)
                .Subscribe(x => this.model.SmoothingMethod = x)
                .AddTo(Disposables);

            SmoothingLevel = new ReactiveProperty<string>(this.model.SmoothingLevel.ToString())
                .SetValidateAttribute(() => SmoothingLevel)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => SmoothingLevel)
                .Subscribe(x => this.model.SmoothingLevel = int.Parse(x))
                .AddTo(Disposables);

            MinimumAmplitude = new ReactiveProperty<string>(this.model.MinimumAmplitude.ToString())
                .SetValidateAttribute(() => MinimumAmplitude)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => MinimumAmplitude)
                .Subscribe(x => this.model.MinimumAmplitude = double.Parse(x))
                .AddTo(Disposables);

            MinimumDatapoints = new ReactiveProperty<string>(this.model.MinimumDatapoints.ToString())
                .SetValidateAttribute(() => MinimumDatapoints)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => MinimumDatapoints)
                .Subscribe(x => this.model.MinimumDatapoints = double.Parse(x))
                .AddTo(Disposables);

            MassSliceWidth = new ReactiveProperty<string>(this.model.MassSliceWidth.ToString())
                .SetValidateAttribute(() => MassSliceWidth)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => MassSliceWidth)
                .Subscribe(x => this.model.MassSliceWidth = float.Parse(x))
                .AddTo(Disposables);

            RetentionTimeBegin = new ReactiveProperty<string>(this.model.RetentionTimeBegin.ToString())
                .SetValidateAttribute(() => RetentionTimeBegin)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => RetentionTimeBegin)
                .Subscribe(x => this.model.RetentionTimeBegin = float.Parse(x))
                .AddTo(Disposables);

            RetentionTimeEnd = new ReactiveProperty<string>(this.model.RetentionTimeEnd.ToString())
                .SetValidateAttribute(() => RetentionTimeEnd)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => RetentionTimeEnd)
                .Subscribe(x => this.model.RetentionTimeEnd = float.Parse(x))
                .AddTo(Disposables);

            MassRangeBegin = new ReactiveProperty<string>(this.model.MassRangeBegin.ToString())
                .SetValidateAttribute(() => MassRangeBegin)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => MassRangeBegin)
                .Subscribe(x => this.model.MassRangeBegin = float.Parse(x))
                .AddTo(Disposables);

            MassRangeEnd = new ReactiveProperty<string>(this.model.MassRangeEnd.ToString())
                .SetValidateAttribute(() => MassRangeEnd)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => MassRangeEnd)
                .Subscribe(x => this.model.MassRangeEnd = float.Parse(x))
                .AddTo(Disposables);

            Ms2MassRangeBegin = new ReactiveProperty<string>(this.model.Ms2MassRangeBegin.ToString())
                .SetValidateAttribute(() => Ms2MassRangeBegin)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => Ms2MassRangeBegin)
                .Subscribe(x => this.model.Ms2MassRangeBegin = float.Parse(x))
                .AddTo(Disposables);

            Ms2MassRangeEnd = new ReactiveProperty<string>(this.model.Ms2MassRangeEnd.ToString())
                .SetValidateAttribute(() => Ms2MassRangeEnd)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => Ms2MassRangeEnd)
                .Subscribe(x => this.model.Ms2MassRangeEnd = float.Parse(x))
                .AddTo(Disposables);

            CentroidMs1Tolerance = new ReactiveProperty<string>(this.model.CentroidMs1Tolerance.ToString())
                .SetValidateAttribute(() => CentroidMs1Tolerance)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => CentroidMs1Tolerance)
                .Subscribe(x => this.model.CentroidMs1Tolerance = float.Parse(x))
                .AddTo(Disposables);

            CentroidMs2Tolerance = new ReactiveProperty<string>(this.model.CentroidMs2Tolerance.ToString())
                .SetValidateAttribute(() => CentroidMs2Tolerance)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => CentroidMs2Tolerance)
                .Subscribe(x => this.model.CentroidMs2Tolerance = float.Parse(x))
                .AddTo(Disposables);

            MaxChargeNumber = new ReactiveProperty<string>(this.model.MaxChargeNumber.ToString())
                .SetValidateAttribute(() => MaxChargeNumber)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => MaxChargeNumber)
                .Subscribe(x => this.model.MaxChargeNumber = int.Parse(x))
                .AddTo(Disposables);

            IsBrClConsideredForIsotopes = new ReactivePropertySlim<bool>(this.model.IsBrClConsideredForIsotopes)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => IsBrClConsideredForIsotopes)
                .Subscribe(x => this.model.IsBrClConsideredForIsotopes = x).AddTo(Disposables);

            ExcludedMassList = new ObservableCollection<MzSearchQueryVM>(
                this.model.ExcludedMassList.Select(mass => new MzSearchQueryVM { Mass = mass.Mass, Tolerance = mass.MassTolerance, })
                );
            CommitAsObservable
                .Select(_ => ExcludedMassList
                    .Where(query => query.IsValid)
                    .Select(query => new MzSearchQuery { Mass = query.Mass!.Value, MassTolerance = query.Tolerance!.Value })
                    .ToList())
                .Subscribe(queries => this.model.ExcludedMassList = queries)
                .AddTo(Disposables);

            HasErrors = new[]
            {
                SmoothingLevel.ObserveHasErrors,
                MinimumAmplitude.ObserveHasErrors,
                MinimumDatapoints.ObserveHasErrors,
                MassSliceWidth.ObserveHasErrors,
                RetentionTimeBegin.ObserveHasErrors,
                RetentionTimeEnd.ObserveHasErrors,
                MassRangeBegin.ObserveHasErrors,
                MassRangeEnd.ObserveHasErrors,
                Ms2MassRangeBegin.ObserveHasErrors,
                Ms2MassRangeEnd.ObserveHasErrors,
                CentroidMs1Tolerance.ObserveHasErrors,
                CentroidMs2Tolerance.ObserveHasErrors,
                MaxChargeNumber.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim();
        }

        public void Commit() {
            CommitTrigger.OnNext(Unit.Default);
        }
    }
}
