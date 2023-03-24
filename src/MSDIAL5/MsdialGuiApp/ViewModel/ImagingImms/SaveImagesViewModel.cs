using CompMs.App.Msdial.Model.ImagingImms;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;

namespace CompMs.App.Msdial.ViewModel.ImagingImms
{
    internal sealed class SaveImagesViewModel : ViewModelBase
    {
        private readonly SaveImagesModel _model;
        private readonly IMessageBroker _broker;

        public SaveImagesViewModel(SaveImagesModel model, IMessageBroker broker)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            Path = model.ToReactivePropertyAsSynchronized(m => m.Path).AddTo(Disposables);
            BrowseCommand = new ReactiveCommand().WithSubscribe(Browse).AddTo(Disposables);
            SaveCommand = new AsyncReactiveCommand().WithSubscribe(model.SaveAsync).AddTo(Disposables);
        }

        public ReactiveProperty<string> Path { get; }

        public AsyncReactiveCommand SaveCommand { get; }

        public ReactiveCommand BrowseCommand { get; }
        private void Browse()
        {
            var request = new SaveFileNameRequest(path => Path.Value = path)
            {
                Filter = string.Join("|", new[] { "png|.png", "gif|.gif" }),
            };
            _broker.Publish(request);
        }
    }
}
