using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Statistics;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    internal sealed class InternalStandardSetViewModel : ViewModelBase
    {
        public InternalStandardSetViewModel(InternalStandardSetModel model) {
            ApplyCommand = new ReactiveCommand().AddTo(Disposables);
            CancelCommand = new ReactiveCommand().AddTo(Disposables);

            var commit = ApplyCommand.ToUnit().Publish().RefCount();
            var discard = CancelCommand.ToUnit().Publish().RefCount();
            Spots = model.Spots.ToReadOnlyReactiveCollection(m => new NormalizationSpotPropertyViewModel(m, commit, discard)).AddTo(Disposables);
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
            InternalStandardId = model.InternalStandardId;
            commit.Subscribe(_ => model.InternalStandardId = InternalStandardId);
            discard.Subscribe(_ => InternalStandardId =  model.InternalStandardId);
        }

        public int Id => _model.Id;

        public string Metabolite => _model.Metabolite;
        public string Adduct => _model.Adduct;
        public double Mz => _model.Mz;
        public double Rt => _model.Rt;
        public double Mobility => _model.Mobility;
        public int InternalStandardId {
            get => _internalStandardId;
            set => SetProperty(ref _internalStandardId, value);
        }
        private int _internalStandardId;
    }
}
