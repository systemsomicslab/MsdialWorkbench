using CompMs.CommonMVVM;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.Graphics.UI.ProgressBar
{
    public sealed class ProgressBarRequest {
        public ProgressBarRequest(string label, bool isIndeterminate, Func<ProgressBarVM, Task> asyncAction) {
            Label = label;
            IsIndeterminate = isIndeterminate;
            AsyncAction = asyncAction;
        }

        public string Label { get; }
        public bool IsIndeterminate { get; }

        public Func<ProgressBarVM, Task> AsyncAction { get; }

        public bool? Result { get; set; }
    }

    public sealed class ProgressBarVM : ViewModelBase
    {
        public ProgressBarVM() {

        }

        public ProgressBarVM(ProgressBarRequest request) {
            Label = request.Label;
            IsIndeterminate = request.IsIndeterminate;
        }

        public string Label {
            get => _label;
            set => SetProperty(ref _label, value);
        }
        private string _label = string.Empty;

        public int CurrentValue {
            get => _currentValue;
            set => SetProperty(ref _currentValue, value);
        }
        private int _currentValue = 0;

        public void Increment() {
            Interlocked.Increment(ref _currentValue);
            OnPropertyChanged(nameof(CurrentValue));
        }

        public bool IsIndeterminate {
            get => _isIndeterminate;
            set => SetProperty(ref _isIndeterminate, value);
        }
        private bool _isIndeterminate = false;
    }
}
