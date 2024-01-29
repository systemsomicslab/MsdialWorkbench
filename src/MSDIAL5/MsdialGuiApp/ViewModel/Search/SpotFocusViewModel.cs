using CompMs.App.Msdial.Model.Search;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Search
{
    public sealed class SpotFocusViewModel : ViewModelBase
    {
        private readonly Subject<Unit> _valueUpdated;
        private readonly ISpotFocus _spotFocus;

        public SpotFocusViewModel(ISpotFocus spotFocus) {
            _spotFocus = spotFocus ?? throw new ArgumentNullException(nameof(spotFocus));

            _valueUpdated = new Subject<Unit>().AddTo(Disposables);

            Value = spotFocus.ToReactivePropertyAsSynchronized(
                m => m.Value,
                op => op.Select(m => m.ToString(spotFocus.Format)),
                op => _valueUpdated.WithLatestFrom(op, (_, vm) => double.TryParse(vm, out var m) ? Observable.Return(m) : Observable.Never<double>()).Switch(),
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => Value)
                .AddTo(Disposables);

            FocusCommand = Value.ObserveHasErrors
                .Inverse()
                .ToReactiveCommand()
                .AddTo(Disposables);
            FocusCommand
                .Where(_ => !Value.HasErrors)
                .Subscribe(Focus)
                .AddTo(Disposables);
        }

        public string Label => _spotFocus.Label;

        public bool IsItalic => _spotFocus.IsItalic;

        [RegularExpression(@"-?\d*.?\d*", ErrorMessage = "Invalid format")]
        public ReactiveProperty<string> Value { get; }

        public ReactiveCommand FocusCommand { get; }

        private void Focus(object? _) {
            _valueUpdated.OnNext(Unit.Default);
            _spotFocus.Focus();
        }
    }
}
