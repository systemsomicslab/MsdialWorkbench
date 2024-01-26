using CompMs.CommonMVVM;
using CompMs.Graphics.UI.ProgressBar;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Service
{
    public class TaskProgressCollection : ViewModelBase
    {
        private readonly ReadOnlyObservableCollection<ProgressBarVM> _readOnlyProgressBars;
        private readonly ITaskProcess _taskProcess;

        public TaskProgressCollection() {
            var progressBarMap = new ConcurrentDictionary<object, ProgressBarVM>();
            var progressBars = new ReactiveCollection<ProgressBarVM>().AddTo(Disposables);
            _readOnlyProgressBars = new ReadOnlyObservableCollection<ProgressBarVM>(progressBars);

            var end = new EndTaskProcess(progressBarMap, progressBars, null);
            var progress = new ProgressTaskProcess(progressBarMap, end);
            var start = new StartTaskProcess(progressBarMap, progressBars, progress);
            _taskProcess = start;
        }

        public ReadOnlyObservableCollection<ProgressBarVM> ProgressBars => _readOnlyProgressBars;

        public void Update(ITaskNotification taskNotification) {
            _taskProcess?.Update(taskNotification);
        }

        public IDisposable ShowWhileSwitchOn(IObservable<bool> observableSwitch, string label) {
            return observableSwitch.Where(x => x)
                .Select(_ => TaskNotification.Start(label))
                .Do(Update)
                .SelectMany(task => observableSwitch.Where(x => !x).Take(1).Select(_ => TaskNotification.End(task)))
                .Subscribe(Update);
        }

        interface ITaskProcess {
            void Update(ITaskNotification taskNotification);
        }

        class StartTaskProcess : ITaskProcess
        {
            private readonly ReactiveCollection<ProgressBarVM> _progressBars;
            private readonly ConcurrentDictionary<object, ProgressBarVM> _progressBarMap;
            private readonly ITaskProcess _process;

            public StartTaskProcess(ConcurrentDictionary<object, ProgressBarVM> progressBarMap, ReactiveCollection<ProgressBarVM> progressBars, ITaskProcess process) {
                _progressBars = progressBars;
                _progressBarMap = progressBarMap;
                _process = process;
            }
            
            public void Update(ITaskNotification taskNotification) {
                if (!_progressBarMap.ContainsKey(taskNotification.Identifier)) {
                    if (!taskNotification.Status.HasFlag(TaskStatus.Start)) {
                        return;
                    }
                    var progressBar = new ProgressBarVM();
                    if (_progressBarMap.TryAdd(taskNotification.Identifier, progressBar)) {
                        _progressBars.AddOnScheduler(progressBar);
                        taskNotification.Update(progressBar);
                    }
                }
                _process?.Update(taskNotification);
            }
        }

        class ProgressTaskProcess : ITaskProcess
        {
            private readonly ConcurrentDictionary<object, ProgressBarVM> _progressBarMap;
            private readonly ITaskProcess _process;

            public ProgressTaskProcess(ConcurrentDictionary<object, ProgressBarVM> progressBarMap, ITaskProcess process) {
                _progressBarMap = progressBarMap;
                _process = process;
            }
            
            public void Update(ITaskNotification taskNotification) {
                if (_progressBarMap.TryGetValue(taskNotification.Identifier, out var progressBar)) {
                    if (taskNotification.Status.HasFlag(TaskStatus.Progress)) {
                        taskNotification.Update(progressBar);
                    }
                    _process?.Update(taskNotification);
                }
            }
        }

        class EndTaskProcess : ITaskProcess
        {
            private readonly ReactiveCollection<ProgressBarVM> _progressBars;
            private readonly ConcurrentDictionary<object, ProgressBarVM> _progressBarMap;
            private readonly ITaskProcess? _process;

            public EndTaskProcess(ConcurrentDictionary<object, ProgressBarVM> progressBarMap, ReactiveCollection<ProgressBarVM> progressBars, ITaskProcess? process) {
                _progressBars = progressBars;
                _progressBarMap = progressBarMap;
                _process = process;
            }
            
            public void Update(ITaskNotification taskNotification) {
                if (_progressBarMap.ContainsKey(taskNotification.Identifier)) {
                    if (taskNotification.Status.HasFlag(TaskStatus.End)) {
                        if (_progressBarMap.TryRemove(taskNotification.Identifier, out var progressBar)) {
                            taskNotification.Update(progressBar);
                            _progressBars.RemoveOnScheduler(progressBar);
                        }
                    }
                    _process?.Update(taskNotification);
                }
            }
        }
    }
}
