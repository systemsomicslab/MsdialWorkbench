using CompMs.App.Msdial.Model.Search;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    class ImmsCompoundSearchVM : CompoundSearchVM
    {
        public ImmsCompoundSearchVM(CompoundSearchModel model) : base(model) {
            searchUnsubscriber?.Dispose();

            var ms1Tol = ParameterVM.Ms1Tolerance;
            var ms2Tol = ParameterVM.Ms2Tolerance;
            var ccsTol = ParameterVM.CcsTolerance;
            var condition = new[]
            {
                ms1Tol.ObserveHasErrors.Inverse(),
                ms2Tol.ObserveHasErrors.Inverse(),
                ccsTol.ObserveHasErrors.Inverse(),
            }.CombineLatestValuesAreAllTrue();

            searchUnsubscriber = new[] {
                SearchCommand.ToUnit()
            }.Merge()
            .CombineLatest(condition, (_, c) => c)
            .Where(c => c)
            .Select(_ => SearchAsync())
            .Switch()
            .Subscribe(cs => Compounds = cs)
            .AddTo(Disposables);

            SearchCommand.Execute();
        }
    }
}
