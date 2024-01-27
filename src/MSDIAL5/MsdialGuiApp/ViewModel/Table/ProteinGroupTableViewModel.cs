using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Table
{
    //internal class ProteinGroupTableViewModel : MethodViewModel {
    internal class ProteinGroupTableViewModel : ViewModelBase {
        private readonly ReadOnlyReactivePropertySlim<ProteinResultContainerModel?> _modelProperty;
        private readonly ReactiveCollection<ProteinGroupViewModel> _groups;

        public ProteinGroupTableViewModel(IObservable<ProteinResultContainerModel?> model)
        {
            _modelProperty = new ReadOnlyReactivePropertySlim<ProteinResultContainerModel?>(model).AddTo(Disposables);

            _groups = new ReactiveCollection<ProteinGroupViewModel>().AddTo(Disposables);
            Groups = _groups.ToReadOnlyReactiveCollection().AddTo(Disposables);

            _modelProperty.Select(m => m?.ProteinGroups.Select(group => new ProteinGroupViewModel(group)).ToArray() ?? new ProteinGroupViewModel[0])
                .Subscribe(groups =>
                {
                    _groups.ClearOnScheduler();
                    _groups.AddRangeOnScheduler(groups);
                }).AddTo(Disposables);
            _modelProperty.Subscribe(m => Target = m?.Target).AddTo(Disposables);
        }

        public ReadOnlyReactiveCollection<ProteinGroupViewModel> Groups { get; }

        public IReactiveProperty<object>? Target
        {
            get => _target;
            set => SetProperty(ref _target, value);
        }
        private IReactiveProperty<object>? _target;

    }
}
