using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Statistics;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    internal sealed class InternalStandardSetViewModel : ViewModelBase
    {
        public InternalStandardSetViewModel(InternalStandardSetModel model) {
            var isEditting = new BooleanNotifier();
            IsEditting = isEditting;
            ApplyCommand = isEditting.ToReactiveCommand().AddTo(Disposables);
            CancelCommand = new ReactiveCommand().AddTo(Disposables);

            var commit = ApplyCommand.ToUnit().ToReactiveProperty(mode: ReactivePropertyMode.RaiseLatestValueOnSubscribe).AddTo(Disposables);
            var discard = CancelCommand.ToUnit().ToReactiveProperty(mode: ReactivePropertyMode.RaiseLatestValueOnSubscribe).AddTo(Disposables);
            Spots = model.Spots.ToReadOnlyReactiveCollection(m => new NormalizationSpotPropertyViewModel(m, commit, discard)).AddTo(Disposables);
            TargetMsMethod = model.TargetMsMethod;

            Spots.ObserveElementPropertyChanged().Subscribe(_ => isEditting.TurnOn()).AddTo(Disposables);
            commit.Subscribe(_ => isEditting.TurnOff()).AddTo(Disposables);
        }

        public ReadOnlyObservableCollection<NormalizationSpotPropertyViewModel> Spots { get; }

        public TargetMsMethod TargetMsMethod { get; }

        public BooleanNotifier IsEditting { get; }

        public ReactiveCommand ApplyCommand { get; }
        public ReactiveCommand CancelCommand { get; }
    }

    internal sealed class NormalizationSpotPropertyViewModel : ViewModelBase
    {
        private readonly NormalizationSpotPropertyModel _model;

        public NormalizationSpotPropertyViewModel(NormalizationSpotPropertyModel model, IObservable<Unit> commit, IObservable<Unit> discard) {
            _model = model;
            InternalStandardId = model.InternalStandardId;
            model.ObserveProperty(m => m.InternalStandardId).Subscribe(id => InternalStandardId = id).AddTo(Disposables);
            commit.Subscribe(_ => model.InternalStandardId = InternalStandardId).AddTo(Disposables);
            discard.Subscribe(_ => InternalStandardId =  model.InternalStandardId).AddTo(Disposables);
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
