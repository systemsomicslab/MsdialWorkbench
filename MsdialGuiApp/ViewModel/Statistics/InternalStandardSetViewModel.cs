using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Statistics;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Reactive;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    internal sealed class InternalStandardSetViewModel : ViewModelBase
    {
        public InternalStandardSetViewModel(InternalStandardSetModel model) {
            ApplyCommand = new ReactiveCommand().AddTo(Disposables);
            CancelCommand = new ReactiveCommand().AddTo(Disposables);

            Spots = model.Spots.ToReadOnlyReactiveCollection(m => new NormalizationSpotPropertyViewModel(m, ApplyCommand.ToUnit(), CancelCommand.ToUnit())).AddTo(Disposables);
            TargetMsMethod = model.TargetMsMethod;
        }

        public ReadOnlyObservableCollection<NormalizationSpotPropertyViewModel> Spots { get; }

        public TargetMsMethod TargetMsMethod { get; }

        public ReactiveCommand ApplyCommand { get; }
        public ReactiveCommand CancelCommand { get; }
    }

    internal sealed class NormalizationSpotPropertyViewModel : ViewModelBase
    {
        private readonly NormalizationSpotPropertyModel _model;

        public NormalizationSpotPropertyViewModel(NormalizationSpotPropertyModel model, IObservable<Unit> commit, IObservable<Unit> discard) {
            _model = model;
            InternalStandardId = _model.ToReactivePropertyWithCommit(m => m.InternalStandardId, commit, discard).AddTo(Disposables);
        }

        public int Id => _model.Id;

        public string Metabolite => _model.Metabolite;
        public string Adduct => _model.Adduct;
        public double Mz => _model.Mz;
        public double Rt => _model.Rt;
        public double Mobility => _model.Mobility;
        public ReactiveProperty<int> InternalStandardId { get; }
    }
}
