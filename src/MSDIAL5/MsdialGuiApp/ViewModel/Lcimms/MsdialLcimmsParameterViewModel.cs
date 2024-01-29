using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    public class MsdialLcimmsParameterViewModel : ParameterBaseVM
    {
        private readonly MsdialLcImMsParameter model;

        [Required(ErrorMessage = "Drift time begin required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> DriftTimeBegin { get; }

        [Required(ErrorMessage = "Drift time end required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> DriftTimeEnd { get; }

        [Required(ErrorMessage = "Accumulated retention time range required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> AccumulatedRtRange { get; }

        public ReactivePropertySlim<bool> IsAccumulateMS2Spectra { get; }

        [Required(ErrorMessage = "IonMobilityType required.")]
        public ReactiveProperty<IonMobilityType> IonMobilityType { get; }

        public ReadOnlyReactivePropertySlim<bool> IsTIMS { get; }

        public ReadOnlyReactivePropertySlim<bool> IsDTIMS { get; }

        public ReadOnlyReactivePropertySlim<bool> IsTWIMS { get; }

        public ReadOnlyReactivePropertySlim<bool> IsCCS { get; }

        public ReactiveProperty<bool> IsAllCalibrantDataImported { get; }

        [Required(ErrorMessage = "Drift time alignment tolerance required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> DriftTimeAlignmentTolerance { get; }

        [Required(ErrorMessage = "Drift time alignment factor required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> DriftTimeAlignmentFactor { get; }

        public IReadOnlyDictionary<int, CoefficientsForCcsCalculation> FileID2CcsCoefficients { get; }

        public Subject<Unit> CommitTrigger { get; } = new Subject<Unit>();

        public ReadOnlyReactivePropertySlim<bool> HasErrors { get; }

        public IObservable<Unit> CommitAsObservable => CommitTrigger.Where(_ => !HasErrors.Value);

        public MsdialLcimmsParameterViewModel(MsdialLcImMsParameter innerModel) : base(innerModel) {
            model = innerModel;

            DriftTimeBegin = new ReactiveProperty<string>(model.DriftTimeBegin.ToString())
                .SetValidateAttribute(() => DriftTimeBegin)
                .AddTo(Disposables);
            CommitAsObservable
                .Select(_ => float.Parse(DriftTimeBegin.Value))
                .Subscribe(x => model.DriftTimeBegin = x)
                .AddTo(Disposables);
            DriftTimeEnd = new ReactiveProperty<string>(model.DriftTimeEnd.ToString())
                .SetValidateAttribute(() => DriftTimeEnd)
                .AddTo(Disposables);
            CommitAsObservable
                .Select(_ => float.Parse(DriftTimeEnd.Value))
                .Subscribe(x => model.DriftTimeEnd = x)
                .AddTo(Disposables);
            AccumulatedRtRange = new ReactiveProperty<string>(model.AccumulatedRtRange.ToString())
                .SetValidateAttribute(() => AccumulatedRtRange)
                .AddTo(Disposables);
            CommitAsObservable
                .Select(_ => float.Parse(AccumulatedRtRange.Value))
                .Subscribe(x => model.AccumulatedRtRange = x)
                .AddTo(Disposables);

            IsAccumulateMS2Spectra = new ReactivePropertySlim<bool>(model.IsAccumulateMS2Spectra).AddTo(Disposables);
            CommitAsObservable
                .Subscribe(_ => model.IsAccumulateMS2Spectra = IsAccumulateMS2Spectra.Value)
                .AddTo(Disposables);
            IonMobilityType = new ReactiveProperty<IonMobilityType>(model.IonMobilityType)
                .SetValidateAttribute(() => IonMobilityType)
                .AddTo(Disposables);
            CommitAsObservable
                .Subscribe(_ => model.IonMobilityType = IonMobilityType.Value)
                .AddTo(Disposables);
            IsTIMS = IonMobilityType.Select(t => t == CompMs.Common.Enum.IonMobilityType.Tims)
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsDTIMS = IonMobilityType.Select(t => t == CompMs.Common.Enum.IonMobilityType.Dtims)
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsTWIMS = IonMobilityType.Select(t => t == CompMs.Common.Enum.IonMobilityType.Twims)
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsCCS = IonMobilityType.Select(t => t == CompMs.Common.Enum.IonMobilityType.CCS)
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsAllCalibrantDataImported = new ReactiveProperty<bool>(model.IsAllCalibrantDataImported).AddTo(Disposables);
            CommitAsObservable
                .Subscribe(_ => model.IsAllCalibrantDataImported = IsAllCalibrantDataImported.Value)
                .AddTo(Disposables);

            DriftTimeAlignmentTolerance = new ReactiveProperty<string>(model.DriftTimeAlignmentTolerance.ToString())
                .SetValidateAttribute(() => DriftTimeAlignmentTolerance)
                .AddTo(Disposables);
            CommitAsObservable
                .Select(_ => float.Parse(DriftTimeAlignmentTolerance.Value))
                .Subscribe(x => model.DriftTimeAlignmentTolerance = x)
                .AddTo(Disposables);
            DriftTimeAlignmentFactor = new ReactiveProperty<string>(model.DriftTimeAlignmentFactor.ToString())
                .SetValidateAttribute(() => DriftTimeAlignmentFactor)
                .AddTo(Disposables);
            CommitAsObservable
                .Select(_ => float.Parse(DriftTimeAlignmentFactor.Value))
                .Subscribe(x => model.DriftTimeAlignmentFactor = x)
                .AddTo(Disposables);
            FileID2CcsCoefficients = model.FileID2CcsCoefficients;

            HasErrors = new[]
            {
                DriftTimeBegin.ObserveHasErrors,
                DriftTimeEnd.ObserveHasErrors,
                AccumulatedRtRange.ObserveHasErrors,
                IonMobilityType.ObserveHasErrors,
                DriftTimeAlignmentTolerance.ObserveHasErrors,
                DriftTimeAlignmentFactor.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public void Commit() {
            CommitTrigger.OnNext(Unit.Default);
        }
    }
}
