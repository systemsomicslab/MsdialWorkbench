using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel
{
    class CompoundSearchVM : ViewModelBase
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

            SelectedCompound = new ReactivePropertySlim<CompoundResult>()
                .AddTo(Disposables);
            SelectedCompound.Subscribe(c => {
                this.model.SelectedReference = c?.msReference;
                this.model.SelectedMatchResult = c?.matchResult;
            });

            var canSet = SelectedCompound.Select(c => c != null);
            SetConfidenceCommand = canSet.ToReactiveCommand().AddTo(Disposables);
            SetConfidenceCommand.Subscribe(this.model.SetConfidence);
            SetUnsettledCommand = canSet.ToReactiveCommand().AddTo(Disposables);
            SetUnsettledCommand.Subscribe(this.model.SetUnsettled);
            SetUnknownCommand = canSet.ToReactiveCommand().AddTo(Disposables);
            SetUnknownCommand.Subscribe(this.model.SetUnknown);

            ParameterHasErrors = ParameterVM.SelectMany(parameter =>
                (new[]
                {
                    parameter.Ms1Tolerance.ObserveHasErrors,
                    parameter.Ms2Tolerance.ObserveHasErrors,
                }).CombineLatestValuesAreAllFalse()
            ).Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            SearchCommand = new IObservable<bool>[]{
                IsBusy,
                ParameterHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .ToReactiveCommand().AddTo(Disposables);

            searchUnsubscriber = ParameterHasErrors
                .Where(hasErrors => !hasErrors)
                .Select(_ => new[]
                {
                    SearchCommand.ToUnit(),
                    ParameterVM.SelectMany(parameter => new[]
                    {
                        parameter.Ms1Tolerance.ToUnit(),
                        parameter.Ms2Tolerance.ToUnit(),
                    }.Merge())
                }.Merge())
                .Switch()
                .Select(_ => SearchAsync())
                .Switch()
                .Subscribe(cs => Compounds = cs)
                .AddTo(Disposables);

            SearchCommand.Execute();
        }

        private readonly CompoundSearchModel model;
        protected static readonly double MassEPS = 1e-10;

        public MsSpectrumViewModel MsSpectrumViewModel { get; }

        public IReadOnlyList<IAnnotatorContainer> Annotators => model.Annotators;

        public ReactivePropertySlim<IAnnotatorContainer> Annotator { get; }

        public ReadOnlyReactivePropertySlim<MsRefSearchParameterBaseViewModel> ParameterVM { get; }

        public IFileBean File => model.File;

        public IMSIonProperty MSIonProperty => model.MSIonProperty;

        public IMoleculeProperty MoleculeProperty => model.MoleculeProperty;

        public IReadOnlyList<CompoundResult> Compounds {
            get => compounds;
            set => SetProperty(ref compounds, value);
        }
        private IReadOnlyList<CompoundResult> compounds = new ObservableCollection<CompoundResult>();

        public ReactivePropertySlim<CompoundResult> SelectedCompound { get; }

        public ReactiveCommand SearchCommand { get; protected set; }

        protected IDisposable searchUnsubscriber;

        public ReactivePropertySlim<bool> IsBusy { get; } = new ReactivePropertySlim<bool>(false);
        public ReadOnlyReactivePropertySlim<bool> ParameterHasErrors { get; protected set; }

        protected async Task<IReadOnlyList<CompoundResult>> SearchAsync() {
            IsBusy.Value = true;
            var result = await Task.Run(model.Search);
            IsBusy.Value = false;
            return result;
        }

        public ReactiveCommand SetConfidenceCommand { get; }

        public ReactiveCommand SetUnsettledCommand { get; }

        public ReactiveCommand SetUnknownCommand { get; }
    }
}
