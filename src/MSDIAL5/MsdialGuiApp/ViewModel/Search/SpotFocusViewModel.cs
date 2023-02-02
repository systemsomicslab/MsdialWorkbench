using CompMs.App.Msdial.Model.Search;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Search
{
    public class SpotFocusViewModel : ViewModelBase
    {
        public SpotFocusViewModel(ISpotFocus spotFocus) {
            Value = spotFocus.ObserveProperty(m => m.Value)
                .Select(x => x.ToString(spotFocus.Format))
                .ToReactiveProperty()
                .SetValidateAttribute(() => Value)
                .AddTo(Disposables);
            Value.Throttle(TimeSpan.FromSeconds(1))
                .Where(_ => !Value.HasErrors)
                .Subscribe(_ => spotFocus.Value = double.TryParse(Value.Value, out var x) ? x : 0d)
                .AddTo(Disposables);

            FocusCommand = Value.ObserveHasErrors
                .Inverse()
                .ToReactiveCommand()
                .AddTo(Disposables);
            FocusCommand
                .Where(_ => !Value.HasErrors)
                .Do(_ => spotFocus.Value = double.TryParse(Value.Value, out var x) ? x : 0d)
                .Subscribe(_ => spotFocus.Focus())
                .AddTo(Disposables);
            
            Label = spotFocus.Label;
            IsItalic = spotFocus.IsItalic;
        }

        public string Label { get; }

        public bool IsItalic { get; }

        [RegularExpression(@"-?\d*.?\d+", ErrorMessage = "Invalid format")]
        public ReactiveProperty<string> Value { get; }

        public ReactiveCommand FocusCommand { get; }
    }
}
