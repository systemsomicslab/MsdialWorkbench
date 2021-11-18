using CompMs.App.SpectrumViewer.Model;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.SpectrumViewer.ViewModel
{
    public class SpectrumGenerationRuleViewModel : ViewModelBase
    {
        public SpectrumGenerationRuleViewModel(SpectrumGenerationRuleModel model) {
            Model = model;

            Intensity = Model.ToReactivePropertySlimAsSynchronized(m => m.Intensity).AddTo(Disposables);
            Comment = Model.ToReactivePropertySlimAsSynchronized(m => m.Comment).AddTo(Disposables);
            Variables = Model.Variables.ToReadOnlyReactiveCollection().AddTo(Disposables);
            Variable = Model.ToReactivePropertySlimAsSynchronized(m => m.Variable).AddTo(Disposables);

            RemoveCommand = new ReactiveCommand().AddTo(Disposables);
        }

        public SpectrumGenerationRuleModel Model { get; }
        public ReactivePropertySlim<double> Intensity { get; }
        public ReactivePropertySlim<string> Comment { get; }
        public ReadOnlyReactiveCollection<MzVariableModel> Variables { get; }
        public ReactivePropertySlim<MzVariableModel> Variable { get; }

        public ReactiveCommand RemoveCommand { get; }
    }
}
