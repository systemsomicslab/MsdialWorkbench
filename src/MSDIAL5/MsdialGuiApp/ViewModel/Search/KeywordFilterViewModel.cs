using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Search
{
    internal sealed class KeywordFilterViewModel : ViewModelBase
    {
        private readonly KeywordFilterModel _model;

        public KeywordFilterViewModel(KeywordFilterModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            Keywords = new ReactivePropertySlim<string>(string.Empty).AddTo(Disposables);
            ClearCommand = new ReactiveCommand().AddTo(Disposables);
            ClearCommand.Subscribe(_ => Clear()).AddTo(Disposables);
            Keywords.Where(keywords => keywords != null)
                .SelectSwitch(keywords => Observable.FromAsync(token => model.SetKeywordsAsync(keywords.Split(), token)))
                .Subscribe()
                .AddTo(Disposables);
            model.ObserveProperty(m => m.Keywords)
                .Subscribe(_ => Keywords.Value = string.Join(" ", model.Keywords))
                .AddTo(Disposables);
        }

        public string Label => _model.Label;
        public ReactivePropertySlim<string> Keywords { get; }
        public ReactiveCommand ClearCommand { get; }

        public void Clear() {
            Keywords.Value = string.Empty;
        }

        public IObservable<Unit> ObserveChanged => Keywords.ToUnit();
    }
}
