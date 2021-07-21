using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Common;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel
{
    class MsRefSearchParameterBaseVM : DynamicViewModelBase<MsRefSearchParameterBase>
    {
        public MsRefSearchParameterBaseVM(MsRefSearchParameterBase innerModel) : base(innerModel) { }
    }

    class MsRefSearchParameterBaseViewModel : ViewModelBase
    {
        // library search parameters

        [Required(ErrorMessage = "Mass begin required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> MassRangeBegin { get; }

        [Required(ErrorMessage = "Mass end tolerance required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> MassRangeEnd { get; }

        [Required(ErrorMessage = "Retention time tolerance required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> RtTolerance { get; }

        [Required(ErrorMessage = "Retention index tolerance required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> RiTolerance { get; }

        [Required(ErrorMessage = "Collision cross section tolerance required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> CcsTolerance { get; }

        [Required(ErrorMessage = "Ms1 tolerance required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0.000001, double.MaxValue, ErrorMessage = "Too small value.")]
        public ReactiveProperty<string> Ms1Tolerance { get; }

        [Required(ErrorMessage = "Ms2 tolerance required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0.0000001, double.MaxValue, ErrorMessage = "Too small value.")]
        public ReactiveProperty<string> Ms2Tolerance { get; }

        [Required(ErrorMessage = "Amplitude cut off required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, 1d, ErrorMessage = "A threshold should be between 0-1.")]
        public ReactiveProperty<string> RelativeAmpCutoff { get; }

        [Required(ErrorMessage = "Amplitude cut off required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        public ReactiveProperty<string> AbsoluteAmpCutoff { get; }

        // option
        public ReactiveProperty<bool> IsUseTimeForAnnotationFiltering { get; }
        public ReactiveProperty<bool> IsUseCcsForAnnotationFiltering { get; }

        // scoring parameters.
        // by [0-1]
        [Required(ErrorMessage = "Weighted dot product cut off required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, 1000d, ErrorMessage = "A threshold should be between 0-1000.")]
        public ReactiveProperty<string> WeightedDotProductCutOff { get; }

        [Required(ErrorMessage = "Dot product cut off required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, 1000d, ErrorMessage = "A threshold should be between 0-1000.")]
        public ReactiveProperty<string> SimpleDotProductCutOff { get; }

        [Required(ErrorMessage = "Reverse dot product cut off required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, 1000d, ErrorMessage = "A threshold should be between 0-1000.")]
        public ReactiveProperty<string> ReverseDotProductCutOff { get; }

        [Required(ErrorMessage = "Matched peaks percentage cut off required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, 100d, ErrorMessage = "A threshold should be between 0-100.")]
        public ReactiveProperty<string> MatchedPeaksPercentageCutOff { get; }

        [Required(ErrorMessage = "Total score cut off required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, 1000d, ErrorMessage = "A threshold should be between 0-1000.")]
        public ReactiveProperty<string> TotalScoreCutoff { get; }

        [Required(ErrorMessage = "Minimum number of matched spectrum required.")]
        [RegularExpression("[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0d, double.MaxValue, ErrorMessage = "Positive value required.")]
        // by absolute value
        public ReactiveProperty<string> MinimumSpectrumMatch { get; }

        // option
        public ReactiveProperty<bool> IsUseTimeForAnnotationScoring { get; }

        public ReactiveProperty<bool> IsUseCcsForAnnotationScoring { get; }

        private readonly MsRefSearchParameterBase model;

        public MsRefSearchParameterBaseViewModel(MsRefSearchParameterBase model) {
            this.model = model;

            MassRangeBegin = new ReactiveProperty<string>(model.MassRangeBegin.ToString())
                .SetValidateAttribute(() => MassRangeBegin)
                .AddTo(Disposables);
            MassRangeBegin.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(MassRangeBegin.Value))
                .Subscribe(x => this.model.MassRangeBegin = x)
                .AddTo(Disposables);
            MassRangeEnd = new ReactiveProperty<string>(model.MassRangeEnd.ToString())
                .SetValidateAttribute(() => MassRangeEnd)
                .AddTo(Disposables);
            MassRangeEnd.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(MassRangeEnd.Value))
                .Subscribe(x => this.model.MassRangeEnd = x)
                .AddTo(Disposables);
            RtTolerance = new ReactiveProperty<string>(model.RtTolerance.ToString())
                .SetValidateAttribute(() => RtTolerance)
                .AddTo(Disposables);
            RtTolerance.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(RtTolerance.Value))
                .Subscribe(x => this.model.RtTolerance = x)
                .AddTo(Disposables);
            RiTolerance = new ReactiveProperty<string>(model.RiTolerance.ToString())
                .SetValidateAttribute(() => RiTolerance)
                .AddTo(Disposables);
            RiTolerance.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(RiTolerance.Value))
                .Subscribe(x => this.model.RiTolerance = x)
                .AddTo(Disposables);
            CcsTolerance = new ReactiveProperty<string>(model.CcsTolerance.ToString())
                .SetValidateAttribute(() => CcsTolerance)
                .AddTo(Disposables);
            CcsTolerance.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(CcsTolerance.Value))
                .Subscribe(x => this.model.CcsTolerance = x)
                .AddTo(Disposables);
            Ms1Tolerance = new ReactiveProperty<string>(model.Ms1Tolerance.ToString())
                .SetValidateAttribute(() => Ms1Tolerance)
                .AddTo(Disposables);
            Ms1Tolerance.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(Ms1Tolerance.Value))
                .Subscribe(x => this.model.Ms1Tolerance = x)
                .AddTo(Disposables);
            Ms2Tolerance = new ReactiveProperty<string>(model.Ms2Tolerance.ToString())
                .SetValidateAttribute(() => Ms2Tolerance)
                .AddTo(Disposables);
            Ms2Tolerance.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(Ms2Tolerance.Value))
                .Subscribe(x => this.model.Ms2Tolerance = x)
                .AddTo(Disposables);
            RelativeAmpCutoff = new ReactiveProperty<string>(model.RelativeAmpCutoff.ToString())
                .SetValidateAttribute(() => RelativeAmpCutoff)
                .AddTo(Disposables);
            RelativeAmpCutoff.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(RelativeAmpCutoff.Value))
                .Subscribe(x => this.model.RelativeAmpCutoff = x)
                .AddTo(Disposables);
            AbsoluteAmpCutoff = new ReactiveProperty<string>(model.AbsoluteAmpCutoff.ToString())
                .SetValidateAttribute(() => AbsoluteAmpCutoff)
                .AddTo(Disposables);
            AbsoluteAmpCutoff.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(AbsoluteAmpCutoff.Value))
                .Subscribe(x => this.model.AbsoluteAmpCutoff = x)
                .AddTo(Disposables);

            IsUseTimeForAnnotationFiltering = new ReactiveProperty<bool>(model.IsUseTimeForAnnotationFiltering).AddTo(Disposables);
            IsUseTimeForAnnotationFiltering.ObserveHasErrors.Where(e => !e)
                .Select(_ => IsUseTimeForAnnotationFiltering.Value)
                .Subscribe(x => this.model.IsUseTimeForAnnotationFiltering = x)
                .AddTo(Disposables);
            IsUseCcsForAnnotationFiltering = new ReactiveProperty<bool>(model.IsUseCcsForAnnotationFiltering).AddTo(Disposables);
            IsUseCcsForAnnotationFiltering.ObserveHasErrors.Where(e => !e)
                .Select(_ => IsUseCcsForAnnotationFiltering.Value)
                .Subscribe(x => this.model.IsUseCcsForAnnotationFiltering = x)
                .AddTo(Disposables);

            WeightedDotProductCutOff = new ReactiveProperty<string>((model.WeightedDotProductCutOff * 1000).ToString())
                .SetValidateAttribute(() => WeightedDotProductCutOff)
                .AddTo(Disposables);
            WeightedDotProductCutOff.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(WeightedDotProductCutOff.Value) / 1000)
                .Subscribe(x => this.model.WeightedDotProductCutOff = x)
                .AddTo(Disposables);
            SimpleDotProductCutOff = new ReactiveProperty<string>((model.SimpleDotProductCutOff * 1000).ToString())
                .SetValidateAttribute(() => SimpleDotProductCutOff)
                .AddTo(Disposables);
            SimpleDotProductCutOff.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(SimpleDotProductCutOff.Value) / 1000)
                .Subscribe(x => this.model.SimpleDotProductCutOff = x)
                .AddTo(Disposables);
            ReverseDotProductCutOff = new ReactiveProperty<string>((model.ReverseDotProductCutOff * 1000).ToString())
                .SetValidateAttribute(() => ReverseDotProductCutOff)
                .AddTo(Disposables);
            ReverseDotProductCutOff.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(ReverseDotProductCutOff.Value) / 1000)
                .Subscribe(x => this.model.ReverseDotProductCutOff = x)
                .AddTo(Disposables);
            MatchedPeaksPercentageCutOff = new ReactiveProperty<string>((model.MatchedPeaksPercentageCutOff * 100).ToString())
                .SetValidateAttribute(() => MatchedPeaksPercentageCutOff)
                .AddTo(Disposables);
            MatchedPeaksPercentageCutOff.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(MatchedPeaksPercentageCutOff.Value) / 100)
                .Subscribe(x => this.model.MatchedPeaksPercentageCutOff = x)
                .AddTo(Disposables);
            TotalScoreCutoff = new ReactiveProperty<string>((model.TotalScoreCutoff * 1000).ToString())
                .SetValidateAttribute(() => TotalScoreCutoff)
                .AddTo(Disposables);
            TotalScoreCutoff.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(TotalScoreCutoff.Value) / 1000)
                .Subscribe(x => this.model.TotalScoreCutoff = x)
                .AddTo(Disposables);
            MinimumSpectrumMatch = new ReactiveProperty<string>(model.MinimumSpectrumMatch.ToString())
                .SetValidateAttribute(() => MinimumSpectrumMatch)
                .AddTo(Disposables);
            MinimumSpectrumMatch.ObserveHasErrors.Where(e => !e)
                .Select(_ => float.Parse(MinimumSpectrumMatch.Value))
                .Subscribe(x => this.model.MinimumSpectrumMatch = x)
                .AddTo(Disposables);

            IsUseTimeForAnnotationScoring = new ReactiveProperty<bool>(model.IsUseTimeForAnnotationScoring).AddTo(Disposables);
            IsUseTimeForAnnotationScoring.ObserveHasErrors.Where(e => !e)
                .Select(_ => IsUseTimeForAnnotationScoring.Value)
                .Subscribe(x => this.model.IsUseTimeForAnnotationScoring = x)
                .AddTo(Disposables);
            IsUseCcsForAnnotationScoring = new ReactiveProperty<bool>(model.IsUseCcsForAnnotationScoring).AddTo(Disposables);
            IsUseCcsForAnnotationScoring.ObserveHasErrors.Where(e => !e)
                .Select(_ => IsUseCcsForAnnotationScoring.Value)
                .Subscribe(x => this.model.IsUseCcsForAnnotationScoring = x)
                .AddTo(Disposables);
        }
    }
}
