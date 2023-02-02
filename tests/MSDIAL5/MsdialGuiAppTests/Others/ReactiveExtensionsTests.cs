using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace CompMs.App.Msdial.Others.Tests
{
    [TestClass]
    public class ReactiveExtensionsTests
    {
        [TestMethod]
        public void ReactiveExtensionsSubscribeTest() {
            var subject1 = new Subject<int>();
            var counter1 = 0;
            var incrementObservable = subject1.Do(_ => Interlocked.Increment(ref counter1));
            incrementObservable.Subscribe(i => Console.WriteLine($"First: {i}, {nameof(counter1)} current {counter1}"));
            incrementObservable.Subscribe(i => Console.WriteLine($"Second: {i}, {nameof(counter1)} current {counter1}"));

            subject1.OnNext(1);
            subject1.OnNext(2);
            subject1.OnNext(3);
            Assert.AreEqual(6, counter1);

            var subject2 = new Subject<int>();
            var counter2 = 0;
            var incrementProperty = subject2.Do(_ => Interlocked.Increment(ref counter2)).ToReactiveProperty();
            incrementProperty.Subscribe(i => Console.WriteLine($"First: {i}, {nameof(counter2)} current {counter2}"));
            incrementProperty.Subscribe(i => Console.WriteLine($"Second: {i}, {nameof(counter2)} current {counter2}"));

            subject2.OnNext(1);
            subject2.OnNext(2);
            subject2.OnNext(3);
            Assert.AreEqual(3, counter2);

            var subject3 = new Subject<int>();
            var counter3 = 0;
            var incrementReadOnlyProperty = subject3.Do(_ => Interlocked.Increment(ref counter3)).ToReadOnlyReactiveProperty();
            incrementReadOnlyProperty.Subscribe(i => Console.WriteLine($"First: {i}, {nameof(counter3)} current {counter3}"));
            incrementReadOnlyProperty.Subscribe(i => Console.WriteLine($"Second: {i}, {nameof(counter3)} current {counter3}"));

            subject3.OnNext(1);
            subject3.OnNext(2);
            subject3.OnNext(3);
            Assert.AreEqual(3, counter3);

            var subject4 = new Subject<int>();
            var counter4 = 0;
            var incrementReadOnlyPropertySlim = subject4.Do(_ => Interlocked.Increment(ref counter4)).ToReadOnlyReactivePropertySlim();
            incrementReadOnlyPropertySlim.Subscribe(i => Console.WriteLine($"First: {i}, {nameof(counter4)} current {counter4}"));
            incrementReadOnlyPropertySlim.Subscribe(i => Console.WriteLine($"Second: {i}, {nameof(counter4)} current {counter4}"));

            subject4.OnNext(1);
            subject4.OnNext(2);
            subject4.OnNext(3);
            Assert.AreEqual(3, counter4);

            var subject5 = new Subject<int>();
            var counter5 = 0;
            var incrementDynamic = subject5.Do(_ => Interlocked.Increment(ref counter5)).Publish();
            incrementDynamic.Subscribe(i => Console.WriteLine($"First: {i}, {nameof(counter5)} current {counter5}"));
            incrementDynamic.Subscribe(i => Console.WriteLine($"Second: {i}, {nameof(counter5)} current {counter5}"));
            incrementDynamic.Connect();

            subject5.OnNext(1);
            subject5.OnNext(2);
            subject5.OnNext(3);
            Assert.AreEqual(3, counter5);
        }
    }
}
