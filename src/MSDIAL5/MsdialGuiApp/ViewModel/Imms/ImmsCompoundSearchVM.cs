using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Search;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    internal sealed class ImmsCompoundSearchVM : CompoundSearchVM<PeakSpotModel>
    {
        public ImmsCompoundSearchVM(CompoundSearchModel<PeakSpotModel> model) : base(model) {
            ParameterHasErrors = ParameterViewModel.SelectSwitch(parameter =>
                parameter is null
                    ? Observable.Return(true)
                    : new[]
                    {
                        parameter.Ms1Tolerance.ObserveHasErrors,
                        parameter.Ms2Tolerance.ObserveHasErrors,
                        parameter.CcsTolerance.ObserveHasErrors,
                    }.CombineLatestValuesAreAllFalse()
                    .Inverse())
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            SearchCommand = new IObservable<bool>[]{
                model.IsBusy,
                ParameterHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .ToReactiveCommand().AddTo(Disposables);

            ParameterViewModel.SelectSwitch(parameter =>
                parameter is null
                    ? Observable.Never<Unit>()
                    : new[]
                    {
                        parameter.Ms1Tolerance.ToUnit(),
                        parameter.Ms2Tolerance.ToUnit(),
                        parameter.CcsTolerance.ToUnit(),
                        SearchCommand.ToUnit(),
                    }.Merge().StartWith(Unit.Default))
            .Where(_ => !ParameterHasErrors.Value)
            .Select(_ => Observable.FromAsync(model.SearchAsync))
            .Switch()
            .Subscribe()
            .AddTo(Disposables);
            
            _ = model.SearchAsync(default);
        }

        public ReactiveCommand SearchCommand { get; }

        public ReadOnlyReactivePropertySlim<bool> ParameterHasErrors { get; }
    }
}
