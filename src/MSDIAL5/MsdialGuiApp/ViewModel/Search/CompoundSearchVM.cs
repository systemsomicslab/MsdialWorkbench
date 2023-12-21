using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Search
{
    internal class CompoundSearchVM : ViewModelBase
    {
        protected static readonly double MassEPS = 1e-10;
        private readonly ICompoundSearchModel _model;

        public CompoundSearchVM(ICompoundSearchModel model, ICommand setUnknownCommand) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            _model = model;

            MsSpectrumViewModel = new MsSpectrumViewModel(model.MsSpectrumModel).AddTo(Disposables);

            CompoundSearcher = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedCompoundSearcher).AddTo(Disposables);

            ParameterVM = CompoundSearcher
                .Select(searcher => searcher is null ? null : new MsRefSearchParameterBaseViewModel(searcher.MsRefSearchParameter))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            SelectedCompound = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedCompoundResult).AddTo(Disposables);
            SelectedCompound.Subscribe(c =>
            {
                model.SelectedReference = c?.MsReference;
                model.SelectedMatchResult = c?.MatchResult;
            });

            var canSet = SelectedCompound.Select(c => c != null);
            SetConfidenceCommand = canSet.ToReactiveCommand().AddTo(Disposables);
            SetConfidenceCommand.Subscribe(model.SetConfidence);
            SetUnsettledCommand = canSet.ToReactiveCommand().AddTo(Disposables);
            SetUnsettledCommand.Subscribe(model.SetUnsettled);

            SetUnknownCommand = setUnknownCommand ?? canSet.ToReactiveCommand().WithSubscribe(model.SetUnknown).AddTo(Disposables);

            ParameterHasErrors = ParameterVM.SelectSwitch(parameter =>
                parameter is null
                    ? Observable.Return(true)
                    : new[]
                    {
                        parameter.Ms1Tolerance.ObserveHasErrors,
                        parameter.Ms2Tolerance.ObserveHasErrors,
                    }.CombineLatestValuesAreAllFalse()
                    .Inverse())
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            IsBusy = new BusyNotifier();
            SearchCommand = new IObservable<bool>[]{
                IsBusy,
                ParameterHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .ToReactiveCommand()
            .AddTo(Disposables);

            Compounds = ParameterVM.SelectSwitch(parameter =>
                parameter is null
                    ? Observable.Never<Unit>()
                    : new[]
                    {
                        parameter.Ms1Tolerance.ToUnit(),
                        parameter.Ms2Tolerance.ToUnit(),
                        SearchCommand.ToUnit(),
                    }.Merge())
            .Where(_ => !ParameterHasErrors.Value)
            .SelectSwitch(_ => Observable.FromAsync(SearchAsync))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            SearchCommand.Execute();
        }

        public MsSpectrumViewModel MsSpectrumViewModel { get; }

        public IReadOnlyList<CompoundSearcher> CompoundSearchers => _model.CompoundSearchers;

        public ReactivePropertySlim<CompoundSearcher> CompoundSearcher { get; }

        public ReadOnlyReactivePropertySlim<MsRefSearchParameterBaseViewModel> ParameterVM { get; }

        public IFileBean File => _model.File;

        public IPeakSpotModel PeakSpot => _model.PeakSpot;

        public ReadOnlyReactivePropertySlim<CompoundResultCollection> Compounds { get; protected set; }

        public ReactivePropertySlim<ICompoundResult> SelectedCompound { get; }

        public ReactiveCommand SearchCommand { get; protected set; }

        public BusyNotifier IsBusy { get; }

        public ReadOnlyReactivePropertySlim<bool> ParameterHasErrors { get; protected set; }

        protected async Task<CompoundResultCollection> SearchAsync(CancellationToken token) {
            using (IsBusy.ProcessStart()) {
                var result = await Task.Run(_model.Search, token);
                return result;
            }
        }

        public ReactiveCommand SetConfidenceCommand { get; }

        public ReactiveCommand SetUnsettledCommand { get; }

        public ICommand SetUnknownCommand { get; }
    }
}
