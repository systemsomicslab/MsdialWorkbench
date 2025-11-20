using CompMs.Graphics.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.ComponentModel;

namespace CompMs.Graphics.Data.Tests;

[TestClass()]
public class ValueHolderTests
{
    [TestMethod()]
    public void ValueHolder_Property_Changed() {
        var c = new C(new B(new A(1, "a"), new A(2, "b")), new B(new A(3, "c"), new A(4, "d")));
        var accessor = PropertiesAccessor.Build(typeof(C), "B1.A1.X");
        var holder = new ValueHolder {
            Item = c,
            Accessor = accessor,
        };

        Assert.AreEqual(1, holder.Value);
        c.B1.A1.X = 5;
        Assert.AreEqual(5, holder.Value);
        c.B1.A1 = new A(6, "e");
        Assert.AreEqual(6, holder.Value);
        c.B1 = new B(new A(7, "f"), new A(8, "g"));
        Assert.AreEqual(7, holder.Value);
    }

    [TestMethod()]
    public void Detatch_Property_Changed() {
        var c = new C(new B(new A(1, "a"), new A(2, "b")), new B(new A(3, "c"), new A(4, "d")));
        Assert.AreEqual(0, c.Count);
        Assert.AreEqual(0, c.B1.Count);
        Assert.AreEqual(0, c.B1.A1.Count);
        
        var accessor = PropertiesAccessor.Build(typeof(C), "B1.A1.X");
        _ = new ValueHolder
        {
            Item = c,
            Accessor = accessor,
        };
        Assert.AreEqual(1, c.Count);
        Assert.AreEqual(1, c.B1.Count);
        Assert.AreEqual(1, c.B1.A1.Count);

        var oldb = c.B1;
        c.B1 = new B(new A(5, "e"), new A(6, "f"));
        Assert.AreEqual(0, oldb.Count);
        Assert.AreEqual(0, oldb.A1.Count);
        Assert.AreEqual(1, c.B1.Count);
        Assert.AreEqual(1, c.B1.A1.Count);
    }

    [TestMethod]
    public void ValueHolder_Accessor_Changed() {
        var c = new C(new B(new A(1, "a"), new A(2, "b")), new B(new A(3, "c"), new A(4, "d")));
        var accessor = PropertiesAccessor.Build(typeof(C), "B1.A1.X");
        var holder = new ValueHolder
        {
            Item = c,
            Accessor = accessor,
        };
        Assert.AreEqual(1, holder.Value);

        holder.Accessor = PropertiesAccessor.Build(null, "B2.A2.Y");
        Assert.AreEqual("d", holder.Value);
        c.B2.A2.Y = "h";
        Assert.AreEqual("h", holder.Value);
        c.B2.A2 = new A(9, "i");
        Assert.AreEqual("i", holder.Value);
        c.B2 = new B(new A(10, "j"), new A(11, "k"));
        Assert.AreEqual("k", holder.Value);
    }

    [TestMethod]
    public void Detatch_Accessor_Changed() {
        var c = new C(new B(new A(1, "a"), new A(2, "b")), new B(new A(3, "c"), new A(4, "d")));
        Assert.AreEqual(0, c.Count);
        Assert.AreEqual(0, c.B1.Count);
        Assert.AreEqual(0, c.B1.A1.Count);
        
        var accessor = PropertiesAccessor.Build(typeof(C), "B1.A1.X");
        var holder = new ValueHolder
        {
            Item = c,
            Accessor = accessor,
        };
        Assert.AreEqual(1, c.Count);
        Assert.AreEqual(1, c.B1.Count);
        Assert.AreEqual(1, c.B1.A1.Count);

        holder.Accessor = PropertiesAccessor.Build(null, "B2.A2.Y");
        Assert.AreEqual(0, c.B1.Count);
        Assert.AreEqual(0, c.B1.A1.Count);
        Assert.AreEqual(1, c.B2.Count);
        Assert.AreEqual(1, c.B2.A2.Count);
    }

    [TestMethod]
    public void ValueHolder_Source_Changed() {
        var c = new C(new B(new A(1, "a"), new A(2, "b")), new B(new A(3, "c"), new A(4, "d")));
        var accessor = PropertiesAccessor.Build(typeof(C), "B1.A1.X");
        var holder = new ValueHolder
        {
            Item = c,
            Accessor = accessor,
        };
        Assert.AreEqual(1, holder.Value);

        holder.Item = c = new C(new B(new A(5, "e"), new A(6, "f")), new B(new A(7, "g"), new A(8, "h")));
        Assert.AreEqual(5, holder.Value);
        c.B1.A1.X = 9;
        Assert.AreEqual(9, holder.Value);
        c.B1.A1 = new A(10, "i");
        Assert.AreEqual(10, holder.Value);
        c.B1 = new B(new A(11, "j"), new A(12, "k"));
        Assert.AreEqual(11, holder.Value);
    }

    [TestMethod]
    public void Detatch_Source_Changed() {
        var c = new C(new B(new A(1, "a"), new A(2, "b")), new B(new A(3, "c"), new A(4, "d")));
        Assert.AreEqual(0, c.Count);
        Assert.AreEqual(0, c.B1.Count);
        Assert.AreEqual(0, c.B1.A1.Count);
        
        var accessor = PropertiesAccessor.Build(typeof(C), "B1.A1.X");
        var holder = new ValueHolder
        {
            Item = c,
            Accessor = accessor,
        };
        Assert.AreEqual(1, c.Count);
        Assert.AreEqual(1, c.B1.Count);
        Assert.AreEqual(1, c.B1.A1.Count);

        var oldc = c;
        holder.Item = c = new C(new B(new A(5, "e"), new A(6, "f")), new B(new A(7, "g"), new A(8, "h")));
        Assert.AreEqual(0, oldc.Count);
        Assert.AreEqual(0, oldc.B1.Count);
        Assert.AreEqual(0, oldc.B1.A1.Count);
        Assert.AreEqual(1, c.Count);
        Assert.AreEqual(1, c.B1.Count);
        Assert.AreEqual(1, c.B1.A1.Count);
    }

    [TestMethod]
    public void DisposeTest() {
        var c = new C(new B(new A(1, "a"), new A(2, "b")), new B(new A(3, "c"), new A(4, "d")));
        var accessor = PropertiesAccessor.Build(typeof(C), "B1.A1.X");
        Assert.AreEqual(0, c.Count);
        Assert.AreEqual(0, c.B1.Count);
        Assert.AreEqual(0, c.B1.A1.Count);
        Assert.AreEqual(0, c.B2.Count);
        Assert.AreEqual(0, c.B1.A2.Count);
        
        var holder = new ValueHolder
        {
            Item = c,
            Accessor = accessor,
        };
        Assert.AreEqual(1, c.Count);
        Assert.AreEqual(1, c.B1.Count);
        Assert.AreEqual(1, c.B1.A1.Count);
        Assert.AreEqual(0, c.B2.Count);
        Assert.AreEqual(0, c.B1.A2.Count);

        holder.Dispose();
        Assert.AreEqual(0, c.Count);
        Assert.AreEqual(0, c.B1.Count);
        Assert.AreEqual(0, c.B1.A1.Count);
        Assert.AreEqual(0, c.B2.Count);
        Assert.AreEqual(0, c.B1.A2.Count);
    }

    [TestMethod]
    public void Source_Null() {
        var accessor = PropertiesAccessor.Build(typeof(C), "B1.A1.X");
        var holder = new ValueHolder
        {
            Accessor = accessor,
        };
        Assert.AreEqual(null, holder.Value);
    }

    class BindableBase : INotifyPropertyChanged {
        public int Count => PropertyChanged?.GetInvocationList().Length ?? 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "") {
            if (!EqualityComparer<T>.Default.Equals(storage, value)) {
                storage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    class A(int X, string Y) : BindableBase {
        public int X { get => _x; set => SetProperty(ref _x, value); }
        private int _x = X;
        public string Y { get => _y; set => SetProperty(ref _y, value); }
        private string _y = Y;
    }

    class B(A A1, A A2) : BindableBase {
        private A _a1 = A1, _a2 = A2;
        public A A1 { get => _a1; set => SetProperty(ref _a1, value); }
        public A A2 { get => _a2; set => SetProperty(ref _a2, value); }
    }

    class C(B B1, B B2) : BindableBase {
        private B _b1 = B1, _b2 = B2;
        public B B1 { get => _b1; set => SetProperty(ref _b1, value); }
        public B B2 { get => _b2; set => SetProperty(ref _b2, value); }
    }
}