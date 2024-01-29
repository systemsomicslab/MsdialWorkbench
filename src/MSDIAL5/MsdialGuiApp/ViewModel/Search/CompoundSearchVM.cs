using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Search
{
    internal abstract class CompoundSearchVM<T> : CompoundSearchVM {
        private readonly CompoundSearchModel<T> _model;

        public CompoundSearchVM(CompoundSearchModel<T> model) : base(model)
        {
            _model = model;
        }

        public T PeakSpot => _model.PeakSpot;
    }


    internal abstract class CompoundSearchVM : ViewModelBase, ICompoundSearchViewModel
    {
        protected static readonly double MassEPS = 1e-10;
        private readonly ICompoundSearchModel _model;

        public CompoundSearchVM(ICompoundSearchModel model) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            _model = model;

            ParameterViewModel = model.SearchParameter
                .Select(parameter => parameter is null ? null : new MsRefSearchParameterBaseViewModel(parameter))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            MsSpectrumViewModel = new MsSpectrumViewModel(model.MsSpectrumModel).AddTo(Disposables);

            SearchMethod = model.ToReactivePropertySlimAsSynchronized(m => m.SearchMethod).AddTo(Disposables);

            Compounds = model.ObserveProperty(m => m.CompoundResults)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            SelectedCompound = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedCompoundResult).AddTo(Disposables);

            var canSet = SelectedCompound.Select(c => c != null);
            SetConfidenceCommand = canSet.ToReactiveCommand().AddTo(Disposables);
            SetConfidenceCommand.Subscribe(model.SetConfidence);
            SetUnsettledCommand = canSet.ToReactiveCommand().AddTo(Disposables);
            SetUnsettledCommand.Subscribe(model.SetUnsettled);

            SetUnknownCommand = canSet.ToReactiveCommand().WithSubscribe(model.SetUnknown).AddTo(Disposables);
        }

        public MsSpectrumViewModel MsSpectrumViewModel { get; }

        public IList SearchMethods => _model.SearchMethods;

        public ReactivePropertySlim<object?> SearchMethod { get; }

        public ReadOnlyReactivePropertySlim<MsRefSearchParameterBaseViewModel?> ParameterViewModel { get; }

        public IFileBean File => _model.File;

        public ReadOnlyReactivePropertySlim<IReadOnlyList<ICompoundResult>?> Compounds { get; }

        public ReactivePropertySlim<ICompoundResult?> SelectedCompound { get; }

        public ReactiveCommand SetConfidenceCommand { get; }

        public ReactiveCommand SetUnsettledCommand { get; }

        public ReactiveCommand SetUnknownCommand { get; }
    }
}
