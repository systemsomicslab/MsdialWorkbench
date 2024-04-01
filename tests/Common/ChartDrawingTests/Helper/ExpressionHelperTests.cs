using CompMs.Graphics.Core.Base;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
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

    [TestMethod()]
    public void GetConvertToAxisValueExpression_Generic() {
        C c = new C(new B(new A(5)));
        var expressions = ExpressionHelper.GetPropertyGetterExpressions(typeof(C), "B.A.X");
        var axis = new MockAxis();
        var axisValueExpression = ExpressionHelper.GetConvertToAxisValueExpression(expressions.Last());
        var getter = axisValueExpression.Compile();
        var val = getter.Invoke(c, axis);
        Assert.AreEqual(5d, val.Value);
        Assert.IsTrue(axis.GenericMethodCalled);
        Assert.IsFalse(axis.BaseMethodCalled);
    } 

    [TestMethod()]
    public void GetConvertToAxisValueExpression_NotGeneric() {
        C c = new C(new B(new A(5)));
        var expressions = ExpressionHelper.GetPropertyGetterExpressions(typeof(C), "B.A");
        var axis = new MockAxis();
        var axisValueExpression = ExpressionHelper.GetConvertToAxisValueExpression(expressions.Last());
        var getter = axisValueExpression.Compile();
        var val = getter.Invoke(c, axis);
        Assert.AreEqual(0d, val.Value);
        Assert.IsFalse(axis.GenericMethodCalled);
        Assert.IsTrue(axis.BaseMethodCalled);
    } 

    [TestMethod()]
    [ExpectedException(typeof(ArgumentNullException))]
    public void GetConvertToAxisValueExpression_NullGetter_ThrowsException() {
        _ = ExpressionHelper.GetConvertToAxisValueExpression(null);
    }

    [TestMethod()]
    public void GetConvertToAxisValueExpression_InvalidArgument() {
        C c = new C(new B(new A(5)));
        var expressions = ExpressionHelper.GetPropertyGetterExpressions(typeof(C), "B.A.X");
        var axis = new MockAxis();
        var axisValueExpression = ExpressionHelper.GetConvertToAxisValueExpression(expressions.Last());
        var getter = axisValueExpression.Compile();
        var val = getter.Invoke(c.B, axis);
        Assert.IsTrue(val.IsNaN());
        Assert.IsFalse(axis.GenericMethodCalled);
        Assert.IsFalse(axis.BaseMethodCalled);
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

    class MockAxis : IAxisManager<int>
    {
        AxisRange IAxisManager.Range => new(0d, 1d);

        public event EventHandler RangeChanged;

        public event EventHandler InitialRangeChanged;

        bool IAxisManager.Contains(AxisValue value) {
            throw new NotImplementedException();
        }

        bool IAxisManager.ContainsCurrent(AxisValue value) {
            throw new NotImplementedException();
        }

        void IAxisManager.Focus(AxisRange range) {
            throw new NotImplementedException();
        }

        List<LabelTickData> IAxisManager.GetLabelTicks() {
            throw new NotImplementedException();
        }

        void IAxisManager.Recalculate(double drawableLength) {
            throw new NotImplementedException();
        }

        void IAxisManager.Reset() {
            throw new NotImplementedException();
        }

        AxisValue IAxisManager.TranslateFromRenderPoint(double value, bool isFlipped, double drawableLength) {
            throw new NotImplementedException();
        }

        public bool GenericMethodCalled;
        AxisValue IAxisManager<int>.TranslateToAxisValue(int value) {
            GenericMethodCalled = true;
            return new AxisValue(value);
        }

        public bool BaseMethodCalled;
        AxisValue IAxisManager.TranslateToAxisValue(object value) {
            BaseMethodCalled = true;
            return new AxisValue(0d);
        }

        double IAxisManager.TranslateToRenderPoint(AxisValue value, bool isFlipped, double drawableLength) {
            throw new NotImplementedException();
        }

        List<double> IAxisManager<int>.TranslateToRenderPoints(IEnumerable<int> values, bool isFlipped, double drawableLength) {
            throw new NotImplementedException();
        }

        List<double> IAxisManager.TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped, double drawableLength) {
            throw new NotImplementedException();
        }

        List<double> IAxisManager.TranslateToRenderPoints(IEnumerable<AxisValue> values, bool isFlipped, double drawableLength) {
            throw new NotImplementedException();
        }
    }
}