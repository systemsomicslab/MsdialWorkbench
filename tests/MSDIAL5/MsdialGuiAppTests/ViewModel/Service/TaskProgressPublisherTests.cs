using CompMs.Graphics.UI.ProgressBar;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Notifiers;

namespace CompMs.App.Msdial.ViewModel.Service.Tests
{
    [TestClass]
    public class TaskProgressPublisherTests
    {
        [TestMethod]
        public void TaskProgressPublisherTest() {
            var task = TaskNotification.Start("Start");
            var broker = new MessageBroker();
            var mock = new MockSubscriber();
            broker.Subscribe<ITaskNotification>(mock.Listen);

            var publisher = new TaskProgressPublisher(broker, task);
            Assert.AreEqual(0, mock.Count);
            Assert.AreEqual(TaskStatus.None, mock.Status);
            using (publisher.Start()) {
                Assert.AreEqual(1, mock.Count);
                Assert.AreEqual(TaskStatus.Start, mock.Status);

                publisher.Progress(0.1d, "Progress 1");
                Assert.AreEqual(2, mock.Count);
                Assert.AreEqual(10, mock.ProgressBar.CurrentValue);
                Assert.AreEqual("Progress 1", mock.ProgressBar.Label);
                Assert.AreEqual(TaskStatus.Start | TaskStatus.Progress, mock.Status);

                publisher.Progress(0.5d, "Progress 2");
                Assert.AreEqual(3, mock.Count);
                Assert.AreEqual(50, mock.ProgressBar.CurrentValue);
                Assert.AreEqual("Progress 2", mock.ProgressBar.Label);
                Assert.AreEqual(TaskStatus.Start | TaskStatus.Progress, mock.Status);

                publisher.Progress(0.2d, "Progress 3");
                Assert.AreEqual(4, mock.Count);
                Assert.AreEqual(20, mock.ProgressBar.CurrentValue);
                Assert.AreEqual("Progress 3", mock.ProgressBar.Label);
                Assert.AreEqual(TaskStatus.Start | TaskStatus.Progress, mock.Status);
            }
            Assert.AreEqual(5, mock.Count);
            Assert.AreEqual(TaskStatus.End, mock.Status);
        }
    }

    class MockSubscriber {
        public MockSubscriber() {
            Count = 0;
            ProgressBar = new ProgressBarVM();
            Status = TaskStatus.None;
        }

        public int Count { get; private set; }
        public TaskStatus Status { get; private set; }

        public ProgressBarVM ProgressBar { get; }

        public void Listen(ITaskNotification taskNotification) {
            taskNotification.Update(ProgressBar);
            Count++;
            Status = taskNotification.Status;
        }
    }
}
