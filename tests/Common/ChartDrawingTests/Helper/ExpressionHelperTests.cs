using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CompMs.Graphics.Helper.Tests;

[TestClass()]
public class ExpressionHelperTests
{
    [TestMethod()]
    public void GetPropertyGetterExpressionsTest() {
        A a = new(1);
        B b = new(a);
        C c = new(b);

        var expressions = ExpressionHelper.GetPropertyGetterExpressions(typeof(C), "B.A.X");
        var getters = expressions.Select(exp => exp.Compile()).ToArray();

        Assert.AreEqual(3, expressions.Length);
        Assert.AreSame(b, ((Func<C, B>)getters[0]).Invoke(c));
        Assert.AreSame(a, ((Func<C, A>)getters[1]).Invoke(c));
        Assert.AreEqual(1, ((Func<C, int>)getters[2]).Invoke(c));

        Assert.AreSame(b, getters[0].DynamicInvoke(c));
        Assert.AreSame(a, getters[1].DynamicInvoke(c));
        Assert.AreEqual(1, getters[2].DynamicInvoke(c));
    }

    [TestMethod()]
    [ExpectedException(typeof(ArgumentNullException))]
    public void GetPropertyGetterExpressions_NullType_ThrowsException() {
        _ = ExpressionHelper.GetPropertyGetterExpressions(null, "");
    }

    [TestMethod()]
    [ExpectedException(typeof(ArgumentException))]
    public void GetPropertyGetterExpressions_EmptyProperty_ThrowsException() {
        _ = ExpressionHelper.GetPropertyGetterExpressions(typeof(C), "");
    }

    [TestMethod()]
    [ExpectedException(typeof(ArgumentException))]
    public void GetPropertyGetterExpressions_InvalidProperty_ThrowsException() {
        _ = ExpressionHelper.GetPropertyGetterExpressions(typeof(C), "NonExistentProperty");
    }

    [TestMethod()]
    public void GetPropertyGetterExpressions_SingleProperty_Success() {
        C c = new C(new B(new A(1)));
        var expressions = ExpressionHelper.GetPropertyGetterExpressions(typeof(C), "B");
        var getter = (Func<C, B>)expressions.Single().Compile();

        Assert.AreEqual(c.B, getter(c));
    }

    [TestMethod()]
    [ExpectedException(typeof(InvalidCastException))]
    public void GetPropertyGetterExpressions_TypeMismatch_ThrowsException() {
        C c = new C(new B(new A(1)));
        var expressions = ExpressionHelper.GetPropertyGetterExpressions(typeof(C), "B.A.X");
        var getter = (Func<C, string>)expressions.Last().Compile();

        getter(c);
    }

    class A(int X)
    {
        public int X { get; } = X;
    }

    class B(A A) {
        public A A { get; } = A;
    }

    class C(B B) {
        public B B { get; } = B;
    }
}