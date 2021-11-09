using CompMs.Common.Enum;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;

namespace CompMs.App.SpectrumViewer.ViewModel
{
    public class LipidQueryBeanViewModel : ViewModelBase
    {
        private readonly LipidQueryBean model;

        public LipidQueryBeanViewModel(LipidQueryBean model) {
            this.model = model;

            IonMode = new ReactivePropertySlim<IonMode>(model.IonMode).AddTo(Disposables);
            IonMode.Subscribe(ionMode => this.model.IonMode = ionMode);
            CollisionType = new ReactivePropertySlim<CollisionType>(model.CollisionType).AddTo(Disposables);
            CollisionType.Subscribe(collisionType => this.model.CollisionType = collisionType);
            SolventType = new ReactivePropertySlim<SolventType>(model.SolventType).AddTo(Disposables);
            SolventType.Subscribe(soventType => this.model.SolventType = soventType);
            LbmQueries = new ObservableCollection<LbmQuery>(this.model.LbmQueries);
        }

        public ReactivePropertySlim<IonMode> IonMode { get; }       

        public ReactivePropertySlim<CollisionType> CollisionType { get; }

        public ReactivePropertySlim<SolventType> SolventType { get; }

        public ObservableCollection<LbmQuery> LbmQueries { get; }
    }
}