using CompMs.App.Msdial.Model.Search;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.Search
{
    public class SpotFocusViewModel : ViewModelBase
    {
        public SpotFocusViewModel(ISpotFocus spotFocus) {
            Label = spotFocus.Label;

            Value = spotFocus.ToReactivePropertyAsSynchronized(m => m.Value)
                .SetValidateAttribute(() => Value)
                .AddTo(Disposables);
            
            FocusCommand = Value.ObserveHasErrors
                .Inverse()
                .ToReactiveCommand()
                .WithSubscribe(spotFocus.Focus)
                .AddTo(Disposables);
        }

        public string Label { get; }

        [RegularExpression(@"\d*.?\d+", ErrorMessage = "Invalid format")]
        public ReactiveProperty<double> Value { get; }

        public ReactiveCommand FocusCommand { get; }
    }
}
