using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
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
    class CompoundSearchVM<T> : ViewModelBase where T : IMSProperty, IMoleculeProperty, IIonProperty
    {
        public CompoundSearchVM(CompoundSearchModel<T> model) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            this.model = model;

            MsSpectrumViewModel = new MsSpectrumViewModel(model.MsSpectrumModel);
            ParameterVM = new MsRefSearchParameterVM(this.model.Parameter);

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

            var ms1Tol = ParameterVM.ObserveProperty(m => m.Ms1Tolerance).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var ms2Tol = ParameterVM.ObserveProperty(m => m.Ms2Tolerance).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var condition = new[]
            {
                ms1Tol.Select(tol => tol >= MassEPS),
                ms2Tol.Select(tol => tol >= MassEPS),
            }.CombineLatestValuesAreAllTrue()
            .Merge(Observable.Return(true));
            SearchCommand = IsBusy.Inverse()
                .CombineLatest(condition, (a, b) => a && b)
                .ToReactiveCommand().AddTo(Disposables);

            searchUnsubscriber = new[] {
                ms1Tol.ToUnit(),
                ms2Tol.ToUnit(),
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

        public CompoundSearchVM(
            AnalysisFileBean analysisFile,
            T peakFeature, MSDecResult msdecResult,
            IReadOnlyList<IsotopicPeak> isotopes,
            IAnnotator<T, MSDecResult> annotator,
            MsRefSearchParameterBase parameter = null)
            : this(new CompoundSearchModel<T>(analysisFile, peakFeature, msdecResult, isotopes, annotator, parameter)) {

        }

        public CompoundSearchVM(
            AlignmentFileBean alignmentFile,
            T spot, MSDecResult msdecResult,
            IReadOnlyList<IsotopicPeak> isotopes,
            IAnnotator<T, MSDecResult> annotator,
            MsRefSearchParameterBase parameter = null)
            : this(new CompoundSearchModel<T>(alignmentFile, spot, msdecResult, isotopes, annotator, parameter)) {

        }

        private readonly CompoundSearchModel<T> model;
        protected static readonly double MassEPS = 1e-10;

        public MsSpectrumViewModel MsSpectrumViewModel { get; }

        public MsRefSearchParameterVM ParameterVM {
            get => parameterVM;
            set => SetProperty(ref parameterVM, value);
        }
        private MsRefSearchParameterVM parameterVM;

        public int FileID => model.File.FileID;
        public string FileName => model.File.FileName;
        public double AccurateMass => model.Property.PrecursorMz;
        public string AdductName => model.Property.AdductType.AdductIonName;
        public string MetaboliteName => model.Property.Name;

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
        public float Ms1Tolerance {
            get => innerModel.Ms1Tolerance;
            set {
                if (innerModel.Ms1Tolerance != value) {
                    innerModel.Ms1Tolerance = value;
                    OnPropertyChanged(nameof(Ms1Tolerance));
                }
            }
        }

        public float Ms2Tolerance {
            get => innerModel.Ms2Tolerance;
            set {
                if (innerModel.Ms2Tolerance != value) {
                    innerModel.Ms2Tolerance = value;
                    OnPropertyChanged(nameof(Ms2Tolerance));
                }
            }
        }

        public float CcsTolerance {
            get => innerModel.CcsTolerance;
            set {
                if (innerModel.CcsTolerance != value) {
                    innerModel.CcsTolerance = value;
                    OnPropertyChanged(nameof(CcsTolerance));
                }
            }
        }

        internal readonly MsRefSearchParameterBase innerModel;

        public MsRefSearchParameterVM(MsRefSearchParameterBase innerModel) {
            this.innerModel = innerModel;
        }
    }
}
