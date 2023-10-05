using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.Utility.Tests
{
    [TestClass()]
    public class ObservableExtensionsTests
    {
        [TestMethod()]
        public void ToReactivePropertyWithCommitTest() {
            var commit = new Subject<Unit>();
            var model = new TestValue();
            var property = model.ToReactivePropertyWithCommit(m => m.Value, commit);
            Assert.AreEqual(0, model.Value);
            Assert.AreEqual(0, property.Value);

            // Before committing, value is not converted back.
            property.Value = 1;
            Assert.AreEqual(0, model.Value);
            Assert.AreEqual(1, property.Value);

            // After commit, convert the value back.
            commit.OnNext(Unit.Default);
            Assert.AreEqual(1, property.Value);
            Assert.AreEqual(1, model.Value);

            // After model change, value is converted.
            model.Value = 2;
            Assert.AreEqual(2, property.Value);
            Assert.AreEqual(2, model.Value);

            // Model changes take priority.
            property.Value = 3;
            model.Value = 4;
            Assert.AreEqual(4, property.Value);
            Assert.AreEqual(4, model.Value);

            // After model change, property change is forgotten.
            commit.OnNext(Unit.Default);
            Assert.AreEqual(4, property.Value);
            Assert.AreEqual(4, model.Value);

            // Only the last property change is converted back.
            var list = new List<Unit>();
            model.PropertyChanged += (_1, _2) => list.Add(Unit.Default);
            property.Value = 5;
            property.Value = 6;
            commit.OnNext(Unit.Default);
            Assert.AreEqual(6, property.Value);
            Assert.AreEqual(6, model.Value);
            Assert.AreEqual(1, list.Count);
        }

        [TestMethod]
        public void ToReactivePropertyWithCommitNoChangeCommitTest() {
            var commit = new Subject<Unit>();
            var model = new TestValue();
            var property = model.ToReactivePropertyWithCommit(p => p.Value, commit);
            // If the property has not been changed, it will remain the same after commit.
            commit.OnNext(Unit.Default);
            Assert.AreEqual(0, property.Value);
            Assert.AreEqual(0, model.Value);

            commit.OnNext(Unit.Default);
            Assert.AreEqual(0, property.Value);
            Assert.AreEqual(0, model.Value);
        }

        [TestMethod]
        public void ToReactivePropertyWithCommitDiscardTest() {
            var commit = new Subject<Unit>();
            var discard = new Subject<Unit>();
            var model = new TestValue();
            var property = model.ToReactivePropertyWithCommit(p => p.Value, commit, discard);

            property.Value = 1;
            Assert.AreEqual(1, property.Value);
            Assert.AreEqual(0, model.Value);

            // Property changes disappear after discard.
            discard.OnNext(Unit.Default);
            Assert.AreEqual(0, property.Value);
            Assert.AreEqual(0, model.Value);

            discard.OnNext(Unit.Default);
            Assert.AreEqual(0, property.Value);
            Assert.AreEqual(0, model.Value);

            // Commit does not convert back missing changes.
            commit.OnNext(Unit.Default);
            Assert.AreEqual(0, property.Value);
            Assert.AreEqual(0, model.Value);
        }

        [TestMethod()]
        public void GateTest() {
            var ox = new Subject<int>();
            var sw = new Subject<bool>();
            var property = ox.Gate(sw).ToReadOnlyReactivePropertySlim(1);

            Assert.AreEqual(1, property.Value);

            sw.OnNext(true);
            ox.OnNext(2);
            Assert.AreEqual(2, property.Value);

            ox.OnNext(3);
            Assert.AreEqual(3, property.Value);

            sw.OnNext(false);
            ox.OnNext(4);
            Assert.AreEqual(3, property.Value);

            ox.OnNext(5);
            Assert.AreEqual(3, property.Value);

            sw.OnNext(true);
            Assert.AreEqual(3, property.Value);

            ox.OnNext(6);
            Assert.AreEqual(6, property.Value);
        }

        [TestMethod()]
        public void GateWithObserveWhenEnabledTest() {
            var ox = new Subject<int>();
            var sw = new Subject<bool>();
            var property = ox.Gate(sw, observeWhenEnabled: true).ToReadOnlyReactivePropertySlim(1);

            Assert.AreEqual(1, property.Value);

            sw.OnNext(true);
            ox.OnNext(2);
            Assert.AreEqual(2, property.Value);

            ox.OnNext(3);
            Assert.AreEqual(3, property.Value);

            sw.OnNext(false);
            ox.OnNext(4);
            Assert.AreEqual(3, property.Value);

            ox.OnNext(5);
            Assert.AreEqual(3, property.Value);

            sw.OnNext(true);
            Assert.AreEqual(5, property.Value);

            ox.OnNext(6);
            Assert.AreEqual(6, property.Value);
        }

        [TestMethod]
        public void DefaultIfNullTest() {
            var ox = new Subject<TestValue>();
            var property = ox.DefaultIfNull(v => v.Value, -1).ToReadOnlyReactivePropertySlim(0);

            Assert.AreEqual(0, property.Value);

            ox.OnNext(null);
            Assert.AreEqual(-1, property.Value);

            ox.OnNext(new TestValue { Value = 2, });
            Assert.AreEqual(2, property.Value);

            ox.OnNext(null);
            Assert.AreEqual(-1, property.Value);

            ox.OnNext(new TestValue { Value = 3, });
            Assert.AreEqual(3, property.Value);
        }
    }

    class TestValue : BindableBase
    {
        public int Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
        private int _value;
    }
}