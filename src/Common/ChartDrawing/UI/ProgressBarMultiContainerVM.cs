using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.Graphics.UI.ProgressBar
{
    public sealed class ProgressBarMultiContainerRequest : BindableBase {
        public ProgressBarMultiContainerRequest(Func<ProgressBarMultiContainerVM, Task> asyncAction, IReadOnlyList<string> labels) {
            AsyncAction = asyncAction;
            Labels = labels;
        }

        public Func<ProgressBarMultiContainerVM, Task> AsyncAction { get; }
        public IReadOnlyList<string> Labels { get; }
        public bool? Result { get; set; } = null;
    }

    public sealed class ProgressBarMultiContainerVM : ViewModelBase
    {
        private List<Func<Task>> _actions = new List<Func<Task>>();

        public ProgressBarMultiContainerVM() {
            ProgressBarVMs = new ObservableCollection<ProgressBarVM>();
        }

        public ProgressBarMultiContainerVM(ProgressBarMultiContainerRequest request) {
            _actions.Add(() => request?.AsyncAction?.Invoke(this));
            MaxValue = request.Labels.Count;
            CurrentValue = 0;
            ProgressBarVMs = new ObservableCollection<ProgressBarVM>(request.Labels.Select(label => new ProgressBarVM { Label = label, }));
        }

        public int MaxValue {
            get => maxValue;
            set => SetProperty(ref maxValue, value);
        }

        public int CurrentValue {
            get => currentValue;
            set => SetProperty(ref currentValue, value);
        }
        private int maxValue = 100, currentValue = 0;

        public ObservableCollection<ProgressBarVM> ProgressBarVMs {
            get => progressBarVMs;
            set => SetProperty(ref progressBarVMs, value);
        }
        private ObservableCollection<ProgressBarVM> progressBarVMs;

        public bool? Result {
            get => _result;
            set => SetProperty(ref _result, value);
        }
        private bool? _result = null;

        public void AddAction(Func<Task> action) {
            _actions.Add(action);
        }

        public void Increment() {
            Interlocked.Increment(ref currentValue);
            OnPropertyChanged(nameof(CurrentValue));
        }

        public Task RunAsync() {
            var tasks = new List<Task>();
            foreach (var action in _actions) {
                tasks.Add(action?.Invoke() ?? Task.CompletedTask);
            }
            return Task.WhenAll(tasks);
        }
    }
}
