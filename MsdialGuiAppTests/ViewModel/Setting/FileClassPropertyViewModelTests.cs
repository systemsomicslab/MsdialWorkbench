using CompMs.App.Msdial.Model.Setting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Setting.Tests
{
    [TestClass]
    public class FileClassPropertyViewModelTests
    {
        [TestMethod]
        public void CommitTest() {
            var property = new FileClassPropertyModel("Test", Colors.White, 0);
            var viewmodel = new FileClassPropertyViewModel(property);
            Assert.AreEqual("Test", property.Name);
            Assert.AreEqual(Colors.White, viewmodel.Color.Value);
            Assert.AreEqual(0, viewmodel.Order.Value);
            Assert.AreEqual(Colors.White, property.Color);
            Assert.AreEqual(0, property.Order);

            viewmodel.Color.Value = Colors.Black;
            viewmodel.Order.Value = 1;
            Assert.AreEqual(Colors.Black, viewmodel.Color.Value);
            Assert.AreEqual(1, viewmodel.Order.Value);
            Assert.AreEqual(Colors.White, property.Color);
            Assert.AreEqual(0, property.Order);

            viewmodel.Commit();
            Assert.AreEqual(Colors.Black, viewmodel.Color.Value);
            Assert.AreEqual(1, viewmodel.Order.Value);
            Assert.AreEqual(Colors.Black, property.Color);
            Assert.AreEqual(1, property.Order);

            property.Color = Colors.Gray;
            property.Order = 2;
            Assert.AreEqual(Colors.Gray, viewmodel.Color.Value);
            Assert.AreEqual(2, viewmodel.Order.Value);
            Assert.AreEqual(Colors.Gray, property.Color);
            Assert.AreEqual(2, property.Order);

            viewmodel.Color.Value = Colors.Red;
            viewmodel.Order.Value = 3;
            property.Color = Colors.Blue;
            property.Order = 4;
            Assert.AreEqual(Colors.Blue, viewmodel.Color.Value);
            Assert.AreEqual(4, viewmodel.Order.Value);
            Assert.AreEqual(Colors.Blue, property.Color);
            Assert.AreEqual(4, property.Order);

            viewmodel.Commit();
            Assert.AreEqual(Colors.Blue, viewmodel.Color.Value);
            Assert.AreEqual(4, viewmodel.Order.Value);
            Assert.AreEqual(Colors.Blue, property.Color);
            Assert.AreEqual(4, property.Order);
        }

        [TestMethod]
        public void NoChangeCommitTest() {
            var property = new FileClassPropertyModel("Test", Colors.White, 0);
            var viewmodel = new FileClassPropertyViewModel(property);
            viewmodel.Commit();
            Assert.AreEqual(Colors.White, viewmodel.Color.Value);
            Assert.AreEqual(0, viewmodel.Order.Value);
            Assert.AreEqual(Colors.White, property.Color);
            Assert.AreEqual(0, property.Order);

            viewmodel.Commit();
            Assert.AreEqual(Colors.White, viewmodel.Color.Value);
            Assert.AreEqual(0, viewmodel.Order.Value);
            Assert.AreEqual(Colors.White, property.Color);
            Assert.AreEqual(0, property.Order);
        }

        [TestMethod]
        public void DiscardTest() {
            var property = new FileClassPropertyModel("Test", Colors.White, 0);
            var viewmodel = new FileClassPropertyViewModel(property);

            viewmodel.Color.Value = Colors.Black;
            viewmodel.Order.Value = 1;
            Assert.AreEqual(Colors.Black, viewmodel.Color.Value);
            Assert.AreEqual(1, viewmodel.Order.Value);
            Assert.AreEqual(Colors.White, property.Color);
            Assert.AreEqual(0, property.Order);

            viewmodel.Discard();
            Assert.AreEqual(Colors.White, viewmodel.Color.Value);
            Assert.AreEqual(0, viewmodel.Order.Value);
            Assert.AreEqual(Colors.White, property.Color);
            Assert.AreEqual(0, property.Order);

            viewmodel.Discard();
            Assert.AreEqual(Colors.White, viewmodel.Color.Value);
            Assert.AreEqual(0, viewmodel.Order.Value);
            Assert.AreEqual(Colors.White, property.Color);
            Assert.AreEqual(0, property.Order);

            viewmodel.Commit();
            Assert.AreEqual(Colors.White, viewmodel.Color.Value);
            Assert.AreEqual(0, viewmodel.Order.Value);
            Assert.AreEqual(Colors.White, property.Color);
            Assert.AreEqual(0, property.Order);
        }
    }
}
