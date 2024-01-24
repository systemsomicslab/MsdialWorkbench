using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Search;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    internal sealed class LcmsCompoundSearchViewModel : CompoundSearchVM<PeakSpotModel>
    {
        public LcmsCompoundSearchViewModel(CompoundSearchModel<PeakSpotModel> model) : base(model) {
            ParameterHasErrors = ParameterViewModel.SelectSwitch(parameter =>
                parameter is null
                    ? Observable.Return(true)
                    : new[]
                    {
                        parameter.Ms1Tolerance.ObserveHasErrors,
                        parameter.Ms2Tolerance.ObserveHasErrors,
                        parameter.RtTolerance.ObserveHasErrors,
                    }.CombineLatestValuesAreAllFalse()
                    .Inverse())
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            SearchCommand = new IObservable<bool>[]
            {
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
                        parameter.RtTolerance.ToUnit(),
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
