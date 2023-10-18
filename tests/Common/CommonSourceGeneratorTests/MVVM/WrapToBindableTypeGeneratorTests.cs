namespace CompMs.CommonSourceGenerator.MVVM.Tests
{
    [TestClass]
    public class WrapToBindableTypeGeneratorTests
    {
        [TestMethod]
        public void TestMethod1() {
            MyClass item = new();
            MyClassModel model = new(item);
            string? changedProperty = string.Empty;
            model.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

            model.Foo = 100;
            Assert.AreEqual(nameof(model.Foo), changedProperty);
            Assert.AreEqual(100, item.Foo);

            model.Bar = "aaa";
            Assert.AreEqual(nameof(model.Bar), changedProperty);
            Assert.AreEqual("aaa", item.Bar);

            model.Baz = new MyValue { Value = 1000 };
            Assert.AreEqual(nameof(model.Baz), changedProperty);
            Assert.AreEqual(1000, item.Baz.Value);

            model.Qux = new MyStruct { Value = 10000 };
            Assert.AreEqual(nameof(model.Qux), changedProperty);
            Assert.AreEqual(10000, item.Qux.Value);
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
    }

    [WrapToBindableType(typeof(MyClass))]
    internal partial class MyClassModel {

    }
}