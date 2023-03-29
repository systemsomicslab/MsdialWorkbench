using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.App.Msdial.Model.Service.Tests
{
    [TestClass()]
    public class UndoManagerTests
    {
        [TestMethod()]
        public void UndoManagerTest1() {
            var manager = new UndoManager();
            var command = new MockUndoCommand();
            manager.Add(command);
            Assert.AreEqual(0, command.UndoCount);
            manager.Undo();
            Assert.AreEqual(1, command.UndoCount);
            Assert.AreEqual(0, command.RedoCount);
            manager.Redo();
            Assert.AreEqual(1, command.RedoCount);
            manager.Undo();
            Assert.AreEqual(2, command.UndoCount);
            manager.Redo();
            Assert.AreEqual(2, command.RedoCount);
        }

        [TestMethod()]
        public void UndoManagerTest2() {
            var manager = new UndoManager();
            var command1 = new MockUndoCommand();
            var command2 = new MockUndoCommand();
            var command3 = new MockUndoCommand();
            manager.Add(command1);
            manager.Add(command2);
            manager.Undo();
            Assert.AreEqual(1, command2.UndoCount);
            manager.Add(command3);
            manager.Undo();
            Assert.AreEqual(1, command3.UndoCount);
            manager.Undo();
            Assert.AreEqual(1, command1.UndoCount);
            manager.Redo();
            Assert.AreEqual(1, command1.RedoCount);
            manager.Redo();
            Assert.AreEqual(1, command3.RedoCount);
        }

        class MockUndoCommand : IDoCommand {
            public int UndoCount = 0;
            public int RedoCount = 0;

            public void Do() {
                RedoCount++;
            }

            public void Undo() {
                UndoCount++;
            }
        }
    }
}