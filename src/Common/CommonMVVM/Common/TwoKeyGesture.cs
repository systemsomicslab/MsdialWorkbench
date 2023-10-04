using System;
using System.Windows.Input;
using System.Windows.Markup;

namespace CompMs.CommonMVVM
{
    public sealed class TwoKeyGesture : KeyGesture
    {
        private readonly Key _secondKey;
        private readonly TimeSpan _span;
        private bool _gotFirstGesture;
        private DateTime _gotFirstGestureTime;

        public TwoKeyGesture(ModifierKeys modifier, Key firstKey, Key secondKey) : base(firstKey, modifier) {
            _secondKey = secondKey;
            _span = TimeSpan.FromSeconds(1);
        }

        public TwoKeyGesture(ModifierKeys modifier, Key firstKey, Key secondKey, TimeSpan span) : base(firstKey, modifier) {
            _secondKey = secondKey;
            _span = span;
        }

        public override bool Matches(object targetElement, InputEventArgs inputEventArgs) {
            var now = DateTime.Now;
            if (!(inputEventArgs is KeyEventArgs keyArgs) || keyArgs.IsRepeat) {
                return false;
            }
            if (_gotFirstGesture) {
                _gotFirstGesture = false;
                var result = keyArgs.Key == _secondKey && now - _gotFirstGestureTime <= _span;
                if (result) {
                    inputEventArgs.Handled = true;
                }
                _gotFirstGestureTime = DateTime.MinValue;
                return result;
            }

            _gotFirstGesture = base.Matches(targetElement, inputEventArgs);
            if (_gotFirstGesture) {
                _gotFirstGestureTime = now;
                inputEventArgs.Handled = true;
            }
            return false;
        }

        public override string ToString() {
            return base.ToString() + "," + _secondKey.ToString();
        }
    }

    [MarkupExtensionReturnType(typeof(TwoKeyGesture))]
    public class TwoKeyGestureExtension : MarkupExtension {
        public Key FirstKey { get; set; }
        public Key SecondKey { get; set; }
        public ModifierKeys Modifier { get; set; }
        public TimeSpan Span { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider) {
            return new TwoKeyGesture(Modifier, FirstKey, SecondKey, Span);
        }
    }
}
