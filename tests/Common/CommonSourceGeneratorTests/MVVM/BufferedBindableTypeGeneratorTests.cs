namespace CompMs.CommonSourceGenerator.MVVM.Tests
{
    [TestClass]
    public class BufferedBindableTypeGeneratorTests
    {
        [TestMethod]
        public void TestMethod1() {
            MyClass item = new();
            MyClassModel2 model = new(item);
            List<string?> changedProperties = new();
            model.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            model.Foo = 100;
            CollectionAssert.AreEquivalent(new[] { nameof(model.Foo), nameof(model.FooBar), nameof(model.BarFoo), nameof(model.FooBarBaz) }, changedProperties);
            Assert.AreEqual(0, item.Foo);
            changedProperties.Clear();

            model.Bar = "aaa";
            CollectionAssert.AreEquivalent(new[] { nameof(model.Bar), nameof(model.FooBar), nameof(model.BarFoo), nameof(model.BarBaz), nameof(model.FooBarBaz) }, changedProperties);
            Assert.AreEqual(string.Empty, item.Bar);
            changedProperties.Clear();

            model.Baz = new MyValue { Value = 1000 };
            CollectionAssert.AreEquivalent(new[] { nameof(model.Baz), nameof(model.BarBaz), nameof(model.FooBarBaz) }, changedProperties);
            Assert.AreEqual(1, item.Baz.Value);
            changedProperties.Clear();

            model.Qux = new MyStruct { Value = 10000 };
            CollectionAssert.AreEquivalent(new[] { nameof(model.Qux) }, changedProperties);
            Assert.AreEqual(2, item.Qux.Value);
            changedProperties.Clear();

            model.Commit();
            Assert.AreEqual(100, item.Foo);
            Assert.AreEqual("aaa", item.Bar);
            Assert.AreEqual(1000, item.Baz.Value);
            Assert.AreEqual(10000, item.Qux.Value);
        }
    }

    [BufferedBindableType(typeof(MyClass))]
    internal partial class MyClassModel2 {

    }
}
