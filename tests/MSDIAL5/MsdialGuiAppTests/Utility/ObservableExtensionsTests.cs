using CompMs.CommonMVVM;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.Utility.Tests;

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

    [TestMethod]
    public void ToConstant_ShouldTransformAllItemsToConstant()
    {
        var scheduler = new TestScheduler();
        var source = scheduler.CreateColdObservable(
            new Recorded<Notification<int>>(100, Notification.CreateOnNext(1)),
            new Recorded<Notification<int>>(200, Notification.CreateOnNext(2)),
            new Recorded<Notification<int>>(300, Notification.CreateOnNext(3)),
            new Recorded<Notification<int>>(400, Notification.CreateOnCompleted<int>())
        );

        const string constantValue = "Constant";
        var result = scheduler.Start(() =>
            source.ToConstant(constantValue),
            ReactiveTest.Created, ReactiveTest.Subscribed, ReactiveTest.Disposed
        );

        var expectedMessages = new[]
        {
            new Recorded<Notification<string>>(300, Notification.CreateOnNext(constantValue)),
            new Recorded<Notification<string>>(400, Notification.CreateOnNext(constantValue)),
            new Recorded<Notification<string>>(500, Notification.CreateOnNext(constantValue)),
            new Recorded<Notification<string>>(600, Notification.CreateOnCompleted<string>())
        };

        ReactiveAssert.AreElementsEqual(expectedMessages, result.Messages);
    }

    [TestMethod]
    public void ToConstant_WithEmptySource_ShouldCompleteWithoutEmission()
    {
        var scheduler = new TestScheduler();
        var source = scheduler.CreateColdObservable(new Recorded<Notification<int>>(100, Notification.CreateOnCompleted<int>()));

        const string constantValue = "Constant";
        var result = scheduler.Start(() =>
            source.ToConstant(constantValue),
            ReactiveTest.Created, ReactiveTest.Subscribed, ReactiveTest.Disposed
        );

        var expectedMessages = new[]
        {
            new Recorded<Notification<string>>(300, Notification.CreateOnCompleted<string>())
        };

        ReactiveAssert.AreElementsEqual(expectedMessages, result.Messages);
    }

    [TestMethod]
    public void DefaultIfNull_WithNullValues_ShouldEmitDefault()
    {
        var scheduler = new TestScheduler();
        var source = scheduler.CreateColdObservable(
            new Recorded<Notification<string?>>(100, Notification.CreateOnNext<string?>(null)),
            new Recorded<Notification<string?>>(200, Notification.CreateOnNext<string?>("Hello")),
            new Recorded<Notification<string?>>(300, Notification.CreateOnNext<string?>(null)),
            new Recorded<Notification<string?>>(400, Notification.CreateOnCompleted<string?>())
        );

        var result = scheduler.Start(() =>
            source.DefaultIfNull<string, string>(s => s?.ToUpper()),
            ReactiveTest.Created, ReactiveTest.Subscribed, ReactiveTest.Disposed
        );

        // The expected messages include the transformed "Hello" to "HELLO", and nulls as default values
        var expectedMessages = new[]
        {
            new Recorded<Notification<string?>>(300, Notification.CreateOnNext<string?>(null)),
            new Recorded<Notification<string?>>(400, Notification.CreateOnNext<string?>("HELLO")),
            new Recorded<Notification<string?>>(500, Notification.CreateOnNext<string?>(null)),
            new Recorded<Notification<string?>>(600, Notification.CreateOnCompleted<string?>())
        };

        ReactiveAssert.AreElementsEqual(expectedMessages, result.Messages);
    }

    [TestMethod]
    public void DefaultIfNull_WithoutNullValues_ShouldTransformAll()
    {
        var scheduler = new TestScheduler();
        var source = scheduler.CreateColdObservable(
            new Recorded<Notification<string?>>(100, Notification.CreateOnNext<string?>("World")),
            new Recorded<Notification<string?>>(200, Notification.CreateOnNext<string?>("Hello")),
            new Recorded<Notification<string?>>(300, Notification.CreateOnCompleted<string?>())
        );

        var result = scheduler.Start(() =>
            source.DefaultIfNull<string, string>(s => s?.ToUpper()),
            ReactiveTest.Created, ReactiveTest.Subscribed, ReactiveTest.Disposed
        );

        // Expect all items to be transformed
        var expectedMessages = new[]
        {
            new Recorded<Notification<string?>>(300, Notification.CreateOnNext<string?>("WORLD")),
            new Recorded<Notification<string?>>(400, Notification.CreateOnNext<string?>("HELLO")),
            new Recorded<Notification<string?>>(500, Notification.CreateOnCompleted<string?>())
        };

        ReactiveAssert.AreElementsEqual(expectedMessages, result.Messages);
    }

    [TestMethod]
    public void DefaultIfNull_WithEmptySource_ShouldCompleteWithoutEmission()
    {
        var scheduler = new TestScheduler();
        var source = scheduler.CreateColdObservable(new Recorded<Notification<string?>>(100, Notification.CreateOnCompleted<string?>()));

        var result = scheduler.Start(() =>
            source.DefaultIfNull<string, string>(s => s?.ToUpper()),
            ReactiveTest.Created, ReactiveTest.Subscribed, ReactiveTest.Disposed
        );

        // Expect only the OnCompleted message
        var expectedMessages = new[]
        {
            new Recorded<Notification<string?>>(300, Notification.CreateOnCompleted<string?>())
        };

        ReactiveAssert.AreElementsEqual(expectedMessages, result.Messages);
    }

    [TestMethod]
    public void DefaultIfNull_ObserveProperty() {
        var value = new TestValue();
        value.Value = 100;

        var scheduler = new TestScheduler();

        var source = scheduler.CreateHotObservable(
            new Recorded<Notification<int>>(90, Notification.CreateOnNext(200)),
            new Recorded<Notification<int>>(210, Notification.CreateOnNext(300)),
            new Recorded<Notification<int>>(310, Notification.CreateOnCompleted<int>())
        );
        source.Subscribe(v => value.Value = v);

        var result = scheduler.Start(() =>
            value.ObserveProperty(m => m.Value).Select(v => v == 0 ? null : v.ToString()).DefaultIfNull(v => v),
            ReactiveTest.Created, ReactiveTest.Subscribed, ReactiveTest.Disposed
        );

        var expectedMessages = new[]
        {
            new Recorded<Notification<string?>>(200, Notification.CreateOnNext<string?>("200")),
            new Recorded<Notification<string?>>(210, Notification.CreateOnNext<string?>("300")),
        };

        ReactiveAssert.AreElementsEqual(expectedMessages, result.Messages);
    }

    [TestMethod]
    public void ObserveProperty() {
        var value = new TestValue();
        value.Value = 100;

        var scheduler = new TestScheduler();

        var source = scheduler.CreateHotObservable(
            new Recorded<Notification<int>>(90, Notification.CreateOnNext(200)),
            new Recorded<Notification<int>>(210, Notification.CreateOnNext(300)),
            new Recorded<Notification<int>>(310, Notification.CreateOnCompleted<int>())
        );
        source.Subscribe(v => value.Value = v);

        var result = scheduler.Start(() =>
            value.ObserveProperty(m => m.Value).Select(v => v == 0 ? null : v.ToString()),
            ReactiveTest.Created, ReactiveTest.Subscribed, ReactiveTest.Disposed
        );

        var expectedMessages = new[]
        {
            new Recorded<Notification<string>>(200, Notification.CreateOnNext<string>("200")),
            new Recorded<Notification<string>>(210, Notification.CreateOnNext<string>("300")),
        };

        ReactiveAssert.AreElementsEqual(expectedMessages, result.Messages);
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