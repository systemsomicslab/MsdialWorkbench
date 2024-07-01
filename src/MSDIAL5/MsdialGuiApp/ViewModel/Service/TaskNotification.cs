using System;
using CompMs.Graphics.UI.ProgressBar;

namespace CompMs.App.Msdial.ViewModel.Service
{
    [Flags]
    public enum TaskStatus {
        None = 0x0,
        Start = 0x1,
        Progress = 0x2,
        End = 0x4,
    }

    public interface ITaskNotification {
        object Identifier { get; }
        TaskStatus Status { get; }
        void Update(ProgressBarVM progressBar);
    }

    public static class TaskNotification {
        public static ITaskNotification Start() {
            object identifier = new object();
            return new TaskStartNotification(identifier, null);
        }

        public static ITaskNotification Start(string label) {
            object identifier = new object();
            return new TaskStartNotification(identifier, label);
        }

        public static ITaskNotification Progress(this ITaskNotification task, double progressionRate) {
            return new TaskProgressionNotification(task.Identifier, progressionRate, null);
        }

        public static ITaskNotification Progress(this ITaskNotification task, double progressionRate, string label) {
            return new TaskProgressionNotification(task.Identifier, progressionRate, label);
        }

        public static ITaskNotification End(this ITaskNotification task) {
            return new TaskEndNotification(task.Identifier);
        }
    }

    internal class TaskProgressionNotification : ITaskNotification
    {
        private readonly double _progressRate;
        private readonly string? _label;

        public TaskProgressionNotification(object identifier, double progressionRate, string? label) {
            Identifier = identifier;
            _progressRate = progressionRate;
            _label = label;
        }

        public void Update(ProgressBarVM progressBar) {
            progressBar.IsIndeterminate = false;
            progressBar.CurrentValue = (int)(_progressRate * 100);
            if (_label is not null) {
                progressBar.Label = _label;
            }
        }

        public object Identifier { get; }

        public TaskStatus Status => TaskStatus.Progress | TaskStatus.Start;
    }

    internal class TaskStartNotification : ITaskNotification
    {
        private readonly string? _label;

        public TaskStartNotification(object identifier, string? label) {
            Identifier = identifier;
            _label = label;
        }

        public void Update(ProgressBarVM progressBar) {
            progressBar.IsIndeterminate = true;
            if (_label is not null) {
                progressBar.Label = _label;
            }
        }

        public object Identifier { get; }

        public TaskStatus Status => TaskStatus.Start;
    }

    internal class TaskEndNotification : ITaskNotification
    {
        public TaskEndNotification(object identifier) {
            Identifier = identifier;
        }

        public void Update(ProgressBarVM progressBar) {
            progressBar.IsIndeterminate = false;
            progressBar.CurrentValue = 100;
        }

        public object Identifier { get; }

        public TaskStatus Status => TaskStatus.End;
    }
}
