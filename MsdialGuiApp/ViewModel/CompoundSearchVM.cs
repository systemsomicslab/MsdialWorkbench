using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel
{
    public class CompoundSearchVM : ViewModelBase
    {
        public CompoundSearchVM(CompoundSearchModel model) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            this.model = model;

            MsSpectrumViewModel = new MsSpectrumViewModel(this.model.MsSpectrumModel).AddTo(Disposables);

            Annotator = this.model.ToReactivePropertySlimAsSynchronized(m => m.Annotator).AddTo(Disposables);
            ParameterVM = Annotator
                .Select(annotator => new MsRefSearchParameterBaseViewModel(annotator.Parameter))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            SelectedCompound = new ReactivePropertySlim<ICompoundResult>()
                .AddTo(Disposables);
            SelectedCompound.Subscribe(c => {
                this.model.SelectedReference = c?.MsReference;
                this.model.SelectedMatchResult = c?.MatchResult;
            });

            var canSet = SelectedCompound.Select(c => c != null);
            SetConfidenceCommand = canSet.ToReactiveCommand().AddTo(Disposables);
            SetConfidenceCommand.Subscribe(this.model.SetConfidence);
            SetUnsettledCommand = canSet.ToReactiveCommand().AddTo(Disposables);
            SetUnsettledCommand.Subscribe(this.model.SetUnsettled);
            SetUnknownCommand = canSet.ToReactiveCommand().AddTo(Disposables);
            SetUnknownCommand.Subscribe(this.model.SetUnknown);

            ParameterHasErrors = ParameterVM.Select(
                parameter => new[]
                {
                    parameter.Ms1Tolerance.ObserveHasErrors,
                    parameter.Ms2Tolerance.ObserveHasErrors,
                }.CombineLatestValuesAreAllFalse()
                .Inverse())
            .Switch()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            IsBusy = new BusyNotifier();
            SearchCommand = new IObservable<bool>[]{
                IsBusy,
                ParameterHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .ToReactiveCommand()
            .AddTo(Disposables);

            Compounds = ParameterVM.Select(parameter => new[]
            {
                parameter.Ms1Tolerance.ToUnit(),
                parameter.Ms2Tolerance.ToUnit(),
                SearchCommand.ToUnit(),
            }.Merge())
            .Switch()
            .Where(_ => !ParameterHasErrors.Value)
            .Select(_ => Observable.FromAsync(SearchAsync))
            .Switch()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
            
            SearchCommand.Execute();
        }

        private readonly CompoundSearchModel model;
        protected static readonly double MassEPS = 1e-10;

        public MsSpectrumViewModel MsSpectrumViewModel { get; }

        public IReadOnlyList<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> Annotators => model.Annotators;

        public ReactivePropertySlim<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> Annotator { get; }

        public ReadOnlyReactivePropertySlim<MsRefSearchParameterBaseViewModel> ParameterVM { get; }

        public IFileBean File => model.File;

        public IMSIonProperty MSIonProperty => model.MSIonProperty;

        public IMoleculeProperty MoleculeProperty => model.MoleculeProperty;

        public ReadOnlyReactivePropertySlim<CompoundResultCollection> Compounds { get; protected set; }

        public ReactivePropertySlim<ICompoundResult> SelectedCompound { get; }

        public ReactiveCommand SearchCommand { get; protected set; }

        public BusyNotifier IsBusy { get; }

        public ReadOnlyReactivePropertySlim<bool> ParameterHasErrors { get; protected set; }

        protected async Task<CompoundResultCollection> SearchAsync(CancellationToken token) {
            using (IsBusy.ProcessStart()) {
                var result = await Task.Run(model.Search, token);
                return result;
            }
        }

        public ReactiveCommand SetConfidenceCommand { get; }

        public ReactiveCommand SetUnsettledCommand { get; }

        public ReactiveCommand SetUnknownCommand { get; }
    }
}
