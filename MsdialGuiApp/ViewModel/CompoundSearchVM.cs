using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
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
            ParameterVM = new MsRefSearchParameterVM(this.model.Parameter).AddTo(Disposables);

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

            var ms1Tol = ParameterVM.Ms1Tolerance;
            var ms2Tol = ParameterVM.Ms2Tolerance;
            var condition = new[]
            {
                ms1Tol.ObserveHasErrors.Inverse(),
                ms2Tol.ObserveHasErrors.Inverse(),
            }.CombineLatestValuesAreAllTrue();
            SearchCommand = IsBusy.Inverse()
                .CombineLatest(condition, (a, b) => a && b)
                .ToReactiveCommand().AddTo(Disposables);

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

        private readonly CompoundSearchModel model;
        protected static readonly double MassEPS = 1e-10;

        public MsSpectrumViewModel MsSpectrumViewModel { get; }

        public MsRefSearchParameterVM ParameterVM {
            get => parameterVM;
            set => SetProperty(ref parameterVM, value);
        }
        private MsRefSearchParameterVM parameterVM;

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

namespace CompMs.App.Msdial.ViewModel
{
    public class MsRefSearchParameterVM : ViewModelBase
    {
        public MsRefSearchParameterVM(MsRefSearchParameterBase innerModel) {
            this.innerModel = innerModel;

            Ms1Tolerance = new ReactiveProperty<string>(innerModel.Ms1Tolerance.ToString())
                .SetValidateAttribute(() => Ms1Tolerance);
            Ms2Tolerance = new ReactiveProperty<string>(innerModel.Ms2Tolerance.ToString())
                .SetValidateAttribute(() => Ms2Tolerance);
            CcsTolerance = new ReactiveProperty<string>(innerModel.CcsTolerance.ToString())
                .SetValidateAttribute(() => CcsTolerance);

            Ms1Tolerance.ObserveHasErrors.Inverse()
                .Where(c => c)
                .Subscribe(_ => innerModel.Ms1Tolerance = float.Parse(Ms1Tolerance.Value))
                .AddTo(Disposables);
            Ms2Tolerance.ObserveHasErrors.Inverse()
                .Where(c => c)
                .Subscribe(_ => innerModel.Ms2Tolerance = float.Parse(Ms2Tolerance.Value))
                .AddTo(Disposables);
            CcsTolerance.ObserveHasErrors.Inverse()
                .Where(c => c)
                .Subscribe(_ => innerModel.CcsTolerance = float.Parse(CcsTolerance.Value))
                .AddTo(Disposables);
        }

        internal readonly MsRefSearchParameterBase innerModel;

        [Required(ErrorMessage = "Ms1 tolerance required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0.00001, double.MaxValue, ErrorMessage = "Too small tolerance.")]
        public ReactiveProperty<string> Ms1Tolerance { get; }

        [Required(ErrorMessage = "Ms2 tolerance required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0.00001, double.MaxValue, ErrorMessage = "Too small tolerance.")]
        public ReactiveProperty<string> Ms2Tolerance { get; }

        [Required(ErrorMessage = "Ccs tolerance required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        [Range(0.00001, double.MaxValue, ErrorMessage = "Too small tolerance.")]
        public ReactiveProperty<string> CcsTolerance { get; }
    }
}
