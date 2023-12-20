using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Gcms
{
    internal sealed class GcmsAnalysisCompoundSearchViewModel : ViewModelBase, ICompoundSearchViewModel
    {
        private readonly GcmsAnalysisCompoundSearchModel _model;
        private readonly BusyNotifier _isBusy;
        private readonly ReadOnlyReactivePropertySlim<bool> _parameterHasErrors;

        public GcmsAnalysisCompoundSearchViewModel(GcmsAnalysisCompoundSearchModel model) {
            _model = model;

            SelectedCompound = new ReactivePropertySlim<ICompoundResult>().AddTo(Disposables);
            SelectedCompound.Subscribe(c => {
                model.SelectedReference = c?.MsReference;
                model.SelectedMatchResult = c?.MatchResult;
            }).AddTo(Disposables);
            ParameterVM = Observable.Never<MsRefSearchParameterBaseViewModel>()
                .ToReadOnlyReactivePropertySlim(new MsRefSearchParameterBaseViewModel(model.SearchParameter))
                .AddTo(Disposables);

            _parameterHasErrors = Observable.Create<MsRefSearchParameterBaseViewModel>(observer => {
                observer.OnNext(ParameterVM.Value);
                observer.OnCompleted();
                return () => { };
            }).Concat(ParameterVM).Select(parameter =>
                parameter is null
                    ? Observable.Return(true)
                    : new[]
                    {
                        model.RetentionType switch
                        {
                            RetentionType.RT => parameter.RtTolerance.ObserveHasErrors.StartWith(parameter.RtTolerance.HasErrors),
                            RetentionType.RI => parameter.RiTolerance.ObserveHasErrors.StartWith(parameter.RiTolerance.HasErrors),
                            _ => throw new NotImplementedException(),
                        },
                        parameter.Ms1Tolerance.ObserveHasErrors.StartWith(parameter.Ms1Tolerance.HasErrors),
                        parameter.MassRangeBegin.ObserveHasErrors.StartWith(parameter.MassRangeBegin.HasErrors),
                        parameter.MassRangeEnd.ObserveHasErrors.StartWith(parameter.MassRangeEnd.HasErrors),
                        parameter.AbsoluteAmpCutoff.ObserveHasErrors.StartWith(parameter.AbsoluteAmpCutoff.HasErrors),
                        parameter.RelativeAmpCutoff.ObserveHasErrors.StartWith(parameter.RelativeAmpCutoff.HasErrors),
                        parameter.TotalScoreCutoff.ObserveHasErrors.StartWith(parameter.TotalScoreCutoff.HasErrors),
                        parameter.IsUseTimeForAnnotationScoring.ObserveHasErrors.StartWith(parameter.IsUseTimeForAnnotationFiltering.HasErrors),
                    }.CombineLatestValuesAreAllFalse().Inverse())
                .Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            _isBusy = new BusyNotifier();
            var runSearch = Observable.Defer(() => Observable.FromAsync(SearchAsync));
            ParameterVM.Select(parameter =>
                parameter is null
                    ? Observable.Never<Unit>()
                    : new[]
                    {
                        parameter.RtTolerance.ToUnit(),
                        parameter.RiTolerance.ToUnit(),
                        parameter.Ms1Tolerance.ToUnit(),
                        parameter.MassRangeBegin.ToUnit(),
                        parameter.MassRangeEnd.ToUnit(),
                        parameter.AbsoluteAmpCutoff.ToUnit(),
                        parameter.RelativeAmpCutoff.ToUnit(),
                        parameter.TotalScoreCutoff.ToUnit(),
                        parameter.IsUseTimeForAnnotationScoring.ToUnit(),
                    }.Merge().StartWith(Unit.Default))
                .Switch()
                .Where(_ => !_parameterHasErrors.Value)
                .Select(_ => runSearch).Switch()
                .Subscribe().AddTo(Disposables);

            MsSpectrumViewModel = new MsSpectrumViewModel(model.MsSpectrumModel).AddTo(Disposables);

            SearchCommand = new IObservable<bool>[] {
                _parameterHasErrors,
                _isBusy,
            }.CombineLatestValuesAreAllFalse()
            .ToReactiveCommand().AddTo(Disposables);
            SearchCommand.Select(_ => runSearch).Switch().Subscribe().AddTo(Disposables);
            var canSet = SelectedCompound.Select(c => !(c is null));
            SetConfidenceCommand = canSet.ToReactiveCommand().WithSubscribe(model.SetConfidence).AddTo(Disposables);
            SetUnsettledCommand = canSet.ToReactiveCommand().WithSubscribe(model.SetUnsettled).AddTo(Disposables);
            SetUnknownCommand = canSet.ToReactiveCommand().WithSubscribe(model.SetUnknown).AddTo(Disposables);

            _ = SearchAsync();
        }

        public IFileBean File => _model.File;

        public Ms1BasedSpectrumFeature SpectrumFeature => _model.SpectrumFeature;

        public CompoundResultCollection Compounds {
            get => _compounds;
            private set => SetProperty(ref _compounds, value);
        }
        private CompoundResultCollection _compounds; 

        public ReactivePropertySlim<ICompoundResult> SelectedCompound { get; }

        public ReadOnlyReactivePropertySlim<MsRefSearchParameterBaseViewModel> ParameterVM { get; }

        public MsSpectrumViewModel MsSpectrumViewModel { get; }

        public ReactiveCommand SearchCommand { get; }

        private async Task SearchAsync(CancellationToken token = default) {
            using (_isBusy.ProcessStart()) {
                Compounds = await Task.Run(_model.Search, token).ConfigureAwait(false);
            }
        }

        public ReactiveCommand SetConfidenceCommand { get; }
        public ReactiveCommand SetUnsettledCommand { get; }
        public ReactiveCommand SetUnknownCommand { get; }
    }
}
