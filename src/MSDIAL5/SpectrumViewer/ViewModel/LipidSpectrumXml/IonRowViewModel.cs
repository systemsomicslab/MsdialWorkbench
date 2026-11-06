using CompMs.App.SpectrumViewer.Model.LipidSpectrumXml;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.SpectrumViewer.ViewModel.LipidSpectrumXml
{
    public class IonRowViewModel : ViewModelBase
    {
        public IonRowViewModel(IonRowModel model) {
            Model = model;
            Mz = Model.ToReactivePropertySlimAsSynchronized(m => m.Mz).AddTo(Disposables);
            Nl = Model.ToReactivePropertySlimAsSynchronized(m => m.Nl).AddTo(Disposables);
            Pi = Model.ToReactivePropertySlimAsSynchronized(m => m.Pi).AddTo(Disposables);
            Intensity = Model.ToReactivePropertySlimAsSynchronized(m => m.Intensity).AddTo(Disposables);
            Comment = Model.ToReactivePropertySlimAsSynchronized(m => m.Comment).AddTo(Disposables);
            IsDiagnostic = Model.ToReactivePropertySlimAsSynchronized(m => m.IsDiagnostic).AddTo(Disposables);
            IsPositionDiagnostic = Model.ToReactivePropertySlimAsSynchronized(m => m.IsPositionDiagnostic).AddTo(Disposables);
            DiagnosticIonGroup = Model.ToReactivePropertySlimAsSynchronized(m => m.DiagnosticIonGroup).AddTo(Disposables);
            DiagnosticIonCount = Model.ToReactivePropertySlimAsSynchronized(m => m.DiagnosticIonCount).AddTo(Disposables);
            DiagnosticIonIntensity = Model.ToReactivePropertySlimAsSynchronized(m => m.DiagnosticIonIntensity).AddTo(Disposables);

            RemoveCommand = new ReactiveCommand().AddTo(Disposables);
        }

        public IonRowModel Model { get; }

        public IonRowKind Kind => Model.Kind;

        public ReactivePropertySlim<string> Mz { get; }
        public ReactivePropertySlim<string> Nl { get; }
        public ReactivePropertySlim<string> Pi { get; }
        public ReactivePropertySlim<string> Intensity { get; }
        public ReactivePropertySlim<string> Comment { get; }
        public ReactivePropertySlim<bool> IsDiagnostic { get; }
        public ReactivePropertySlim<bool> IsPositionDiagnostic { get; }
        public ReactivePropertySlim<string> DiagnosticIonGroup { get; }
        public ReactivePropertySlim<string> DiagnosticIonCount { get; }
        public ReactivePropertySlim<string> DiagnosticIonIntensity { get; }

        public ReactiveCommand RemoveCommand { get; }
    }
}
