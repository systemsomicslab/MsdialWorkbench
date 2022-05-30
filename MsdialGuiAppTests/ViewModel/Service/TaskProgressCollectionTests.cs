using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.App.Msdial.ViewModel.Service.Tests
{
    [TestClass()]
    public class TaskProgressCollectionTests
    {
        [TestMethod()]
        public void UpdateTest() {
            var collection = new TaskProgressCollection();
            Assert.AreEqual(0, collection.ProgressBars.Count);

            var task1 = TaskNotification.Start();
            collection.Update(task1);
            Assert.AreEqual(1, collection.ProgressBars.Count);
            Assert.AreEqual(0, collection.ProgressBars[0].CurrentValue);
            Assert.IsTrue(collection.ProgressBars[0].IsIndeterminate);

            collection.Update(TaskNotification.Progress(task1, 0.5));
            Assert.AreEqual(1, collection.ProgressBars.Count);
            Assert.AreEqual(50, collection.ProgressBars[0].CurrentValue);
            Assert.IsFalse(collection.ProgressBars[0].IsIndeterminate);

            var task2 = TaskNotification.Start();
            collection.Update(TaskNotification.Progress(task2, 0.8));
            Assert.AreEqual(2, collection.ProgressBars.Count);
            Assert.AreEqual(80, collection.ProgressBars[1].CurrentValue);
            Assert.IsFalse(collection.ProgressBars[1].IsIndeterminate);

            collection.Update(TaskNotification.End(task2));
            Assert.AreEqual(1, collection.ProgressBars.Count);

            collection.Update(TaskNotification.End(task1));
            Assert.AreEqual(0, collection.ProgressBars.Count);
        }
    }
}