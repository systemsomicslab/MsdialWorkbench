using Reactive.Bindings.Notifiers;
using System;

namespace CompMs.App.Msdial.ViewModel.Service
{
    internal class TaskProgressPublisher
    {
        private readonly IMessageBroker _messageBroker;
        private readonly ITaskNotification _taskNotification;

        public TaskProgressPublisher(IMessageBroker messageBroker, ITaskNotification taskNotification) {
            _messageBroker = messageBroker ?? MessageBroker.Default;
            _taskNotification = taskNotification;
        }

        public TaskProgressPublisher(IMessageBroker messageBroker, string initialLabel) : this(messageBroker, TaskNotification.Start(initialLabel)) {

        }

        public IDisposable Start() {
            _messageBroker.Publish(_taskNotification);
            return new Unsubscriber(this);
        }

        public void Progress(double progressRate, string label) {
            _messageBroker.Publish(TaskNotification.Progress(_taskNotification, progressRate, label));
        }

        public void End() {
            _messageBroker.Publish(TaskNotification.End(_taskNotification));
        }

        class Unsubscriber : IDisposable
        {
            private readonly TaskProgressPublisher _taskProgressPublisher;

            public Unsubscriber(TaskProgressPublisher taskProgressPublisher) {
                _taskProgressPublisher = taskProgressPublisher;
            }

            public void Dispose() {
                _taskProgressPublisher?.End();
            }
        }
    }
}
