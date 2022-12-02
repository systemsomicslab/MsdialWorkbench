using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    public class MsdialImmsParameterViewModel : ParameterBaseVM
    {
        [Required(ErrorMessage = "Drift time begin required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> DriftTimeBegin { get; }

        [Required(ErrorMessage = "Drift time end required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> DriftTimeEnd { get; }

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

        public MsdialImmsParameterViewModel(MsdialImmsParameter innerModel) : base(innerModel) {
            model = innerModel;

            DriftTimeBegin = new ReactiveProperty<string>(model.DriftTimeBegin.ToString())
                .SetValidateAttribute(() => DriftTimeBegin)
                .AddTo(Disposables);
            DriftTimeBegin.Where(_ => !DriftTimeBegin.HasErrors)
                .Select(_ => float.Parse(DriftTimeBegin.Value))
                .Subscribe(x => model.DriftTimeBegin = x)
                .AddTo(Disposables);
            DriftTimeEnd = new ReactiveProperty<string>(model.DriftTimeEnd.ToString())
                .SetValidateAttribute(() => DriftTimeEnd)
                .AddTo(Disposables);
            DriftTimeEnd.Where(_ => !DriftTimeEnd.HasErrors)
                .Select(_ => float.Parse(DriftTimeEnd.Value))
                .Subscribe(x => model.DriftTimeEnd = x)
                .AddTo(Disposables);

            IonMobilityType = new ReactiveProperty<IonMobilityType>(model.IonMobilityType)
                .SetValidateAttribute(() => IonMobilityType)
                .AddTo(Disposables);
            IonMobilityType.Where(_ => !IonMobilityType.HasErrors)
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
            IsAllCalibrantDataImported.Where(_ => !IsAllCalibrantDataImported.HasErrors)
                .Subscribe(_ => model.IsAllCalibrantDataImported = IsAllCalibrantDataImported.Value)
                .AddTo(Disposables);

            DriftTimeAlignmentTolerance = new ReactiveProperty<string>(model.DriftTimeAlignmentTolerance.ToString())
                .SetValidateAttribute(() => DriftTimeAlignmentTolerance)
                .AddTo(Disposables);
            DriftTimeAlignmentTolerance.Where(_ => !DriftTimeAlignmentTolerance.HasErrors)
                .Select(_ => float.Parse(DriftTimeAlignmentTolerance.Value))
                .Subscribe(x => model.DriftTimeAlignmentTolerance = x)
                .AddTo(Disposables);
            DriftTimeAlignmentFactor = new ReactiveProperty<string>(model.DriftTimeAlignmentFactor.ToString())
                .SetValidateAttribute(() => DriftTimeAlignmentFactor)
                .AddTo(Disposables);
            DriftTimeAlignmentFactor.Where(_ => !DriftTimeAlignmentFactor.HasErrors)
                .Select(_ => float.Parse(DriftTimeAlignmentFactor.Value))
                .Subscribe(x => model.DriftTimeAlignmentFactor = x)
                .AddTo(Disposables);
            FileID2CcsCoefficients = model.FileID2CcsCoefficients;
        }
        protected readonly MsdialImmsParameter model;
    }
}