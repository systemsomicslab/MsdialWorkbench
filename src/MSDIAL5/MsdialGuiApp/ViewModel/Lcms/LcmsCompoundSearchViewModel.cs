using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Search;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    internal sealed class LcmsCompoundSearchViewModel : CompoundSearchVM
    {
        public LcmsCompoundSearchViewModel(IEsiCompoundSearchModel model, ICommand setUnknownCommand) : base(model, setUnknownCommand) {
            ParameterHasErrors = ParameterVM.SelectSwitch(parameter =>
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
                IsBusy,
                ParameterHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .ToReactiveCommand().AddTo(Disposables);

            Compounds = ParameterVM.SelectSwitch(parameter =>
                parameter is null
                    ? Observable.Never<Unit>()
                    : new[]
                    {
                        parameter.Ms1Tolerance.ToUnit(),
                        parameter.Ms2Tolerance.ToUnit(),
                        parameter.RtTolerance.ToUnit(),
                    }.Merge())
                .Where(_ => !ParameterHasErrors.Value)
                .SelectSwitch(_ => Observable.FromAsync(SearchAsync))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            SearchCommand.Execute();
        }
    }
}
