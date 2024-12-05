using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Search;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Gcms
{
    internal sealed class GcmsAnalysisCompoundSearchViewModel : CompoundSearchVM<Ms1BasedSpectrumFeature>
    {
        private readonly ReadOnlyReactivePropertySlim<bool> _parameterHasErrors;

        public GcmsAnalysisCompoundSearchViewModel(CompoundSearchModel<Ms1BasedSpectrumFeature> model) : base(model) {
            _parameterHasErrors = Observable.Create<MsRefSearchParameterBaseViewModel?>(observer => {
                observer.OnNext(ParameterViewModel.Value);
                observer.OnCompleted();
                return () => { };
            }).Concat(ParameterViewModel).Select(parameter =>
                parameter is null
                    ? Observable.Return(true)
                    : new[]
                    {
                        parameter.RtTolerance.ObserveHasErrors.StartWith(parameter.RtTolerance.HasErrors),
                        parameter.RiTolerance.ObserveHasErrors.StartWith(parameter.RiTolerance.HasErrors),
                        parameter.Ms1Tolerance.ObserveHasErrors.StartWith(parameter.Ms1Tolerance.HasErrors),
                        parameter.MassRangeBegin.ObserveHasErrors.StartWith(parameter.MassRangeBegin.HasErrors),
                        parameter.MassRangeEnd.ObserveHasErrors.StartWith(parameter.MassRangeEnd.HasErrors),
                        parameter.AbsoluteAmpCutoff.ObserveHasErrors.StartWith(parameter.AbsoluteAmpCutoff.HasErrors),
                        parameter.RelativeAmpCutoff.ObserveHasErrors.StartWith(parameter.RelativeAmpCutoff.HasErrors),
                        parameter.TotalScoreCutoff.ObserveHasErrors.StartWith(parameter.TotalScoreCutoff.HasErrors),
                        parameter.IsUseTimeForAnnotationScoring.ObserveHasErrors.StartWith(parameter.IsUseTimeForAnnotationFiltering.HasErrors),
                    }.CombineLatestValuesAreAllFalse().Inverse())
                .Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            SearchCommand = new IObservable<bool>[] {
                _parameterHasErrors,
                model.IsBusy,
            }.CombineLatestValuesAreAllFalse()
            .ToReactiveCommand().AddTo(Disposables);

            ParameterViewModel.Select(parameter =>
                parameter is null
                    ? Observable.Never<Unit>()
                    : new[]
                    {
                        parameter.RtTolerance.ToUnit(),
                        parameter.RiTolerance.ToUnit(),
                        parameter.Ms1Tolerance.ToUnit(),
                        parameter.MassRangeBegin.ToUnit(),
                        parameter.MassRangeEnd.ToUnit(),
                        parameter.AbsoluteAmpCutoff.ToUnit(),
                        parameter.RelativeAmpCutoff.ToUnit(),
                        parameter.TotalScoreCutoff.ToUnit(),
                        parameter.IsUseTimeForAnnotationScoring.ToUnit(),
                        SearchCommand.ToUnit(),
                    }.Merge().StartWith(Unit.Default))
                .Switch()
                .Where(_ => !_parameterHasErrors.Value)
                .Select(_ => Observable.FromAsync(model.SearchAsync)).Switch()
                .Subscribe().AddTo(Disposables);

            _ = model.SearchAsync(default);
        }

        public ReactiveCommand SearchCommand { get; }
    }
}
