namespace CompMs.CommonSourceGenerator.MVVM.Tests
{
    [TestClass]
    public class WrapToBindableTypeGeneratorTests
    {
        [TestMethod]
        public void TestMethod1() {
            MyClass item = new();
            MyClassModel model = new(item);
            List<string?> changedProperties = new();
            model.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            model.Foo = 100;
            CollectionAssert.AreEquivalent(new[] { nameof(model.Foo), nameof(model.FooBar), nameof(model.BarFoo), nameof(model.FooBarBaz) }, changedProperties);
            Assert.AreEqual(100, item.Foo);
            changedProperties.Clear();

            model.Bar = "aaa";
            CollectionAssert.AreEquivalent(new[] { nameof(model.Bar), nameof(model.FooBar), nameof(model.BarFoo), nameof(model.BarBaz), nameof(model.FooBarBaz) }, changedProperties);
            Assert.AreEqual("aaa", item.Bar);
            changedProperties.Clear();

            model.Baz = new MyValue { Value = 1000 };
            CollectionAssert.AreEquivalent(new[] { nameof(model.Baz), nameof(model.BarBaz), nameof(model.FooBarBaz) }, changedProperties);
            Assert.AreEqual(1000, item.Baz.Value);
            changedProperties.Clear();

            model.Qux = new MyStruct { Value = 10000 };
            CollectionAssert.AreEquivalent(new[] { nameof(model.Qux) }, changedProperties);
            Assert.AreEqual(10000, item.Qux.Value);
            changedProperties.Clear();
        }
    }

    internal class MyValue {
        public int Value { get; set; }
    }

    internal struct MyStruct {
        public int Value { get; set; }
    }

    internal class MyClass {
        public int Foo { get; set; }
        public string Bar { get; set; } = string.Empty;
        public MyValue Baz { get; set; } = new MyValue { Value = 1 };
        public MyStruct Qux { get; set; } = new MyStruct { Value = 2 };

        public string FooBar => $"{Foo}{Bar}";

        public string BarFoo {
            get {
                return $"{Bar}{Foo}";
            }
        }

        public string BarBaz => Bar + Baz.Value;

        public string FooBarBaz => Foo + BarBaz;
    }

    [WrapToBindableType(typeof(MyClass))]
    internal partial class MyClassModel {

    }
}