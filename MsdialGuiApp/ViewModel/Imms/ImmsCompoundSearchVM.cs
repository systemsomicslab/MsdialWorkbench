using CompMs.App.Msdial.Model.Search;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    class ImmsCompoundSearchVM : CompoundSearchVM
    {
        public ImmsCompoundSearchVM(CompoundSearchModel model) : base(model) {
            searchUnsubscriber?.Dispose();

            ParameterHasErrors = ParameterVM.SelectMany(parameter =>
                (new[]
                {
                    parameter.Ms1Tolerance.ObserveHasErrors,
                    parameter.Ms2Tolerance.ObserveHasErrors,
                    parameter.CcsTolerance.ObserveHasErrors,
                }).CombineLatestValuesAreAllFalse()
            ).Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            SearchCommand = new IObservable<bool>[]{
                IsBusy,
                ParameterHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .ToReactiveCommand().AddTo(Disposables);

            searchUnsubscriber = new[] {
                SearchCommand.ToUnit()
            }.Merge()
            .CombineLatest(ParameterHasErrors, (_, c) => !c)
            .Where(c => c)
            .Select(_ => SearchAsync())
            .Switch()
            .Subscribe(cs => Compounds = cs)
            .AddTo(Disposables);

            SearchCommand.Execute();
        }
    }
}
