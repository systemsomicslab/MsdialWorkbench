using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Utility
{
    public static class ObservableExtensions
    {
        public static IObservable<bool> CombineLatestValuesAreAnyTrue(this IEnumerable<IObservable<bool>> source) {
            return source.CombineLatestValuesAreAllFalse().Inverse();
        }

        public static IObservable<U?> DefaultIfNull<T, U>(this IObservable<T?> source, Func<T, U?> ifNotNull) where T: class {
            return source.Publish(src => {
                var x = src.Where(v => v is null).ToConstant(default(U?));
                var y = src.Where(v => v is not null).Select(v => ifNotNull(v!));
                return Observable.Merge(x, y);
            });
        }

        public static IObservable<U> DefaultIfNull<T, U>(this IObservable<T?> source, Func<T, U> ifNotNull, U ifNull) where T: class {
            var x = source.Where(v => v is null).ToConstant(ifNull);
            var y = source.Where(v => v is not null).Select(v => ifNotNull(v!));
            return Observable.Merge(x, y);
        }

        public static IObservable<T> Gate<T>(this IObservable<T> source, IObservable<bool> condition, bool observeWhenEnabled = false) {
            if (observeWhenEnabled) {
                return source.CombineLatest(condition).Where(p => p.Second).Select(p => p.First);
            }
            else {
                return source.WithLatestFrom(condition).Where(p => p.Second).Select(p => p.First);
            }
        }

        public static IObservable<U> ObserveCollectionItems<T, U>(T collection) where T : IEnumerable<U>, INotifyCollectionChanged {
            return collection.ToObservable().Concat(collection.ObserveAddChanged<U>());
        }

        public static IObservable<Unit> ObserveRemoveFrom<T>(this T source, INotifyCollectionChanged collection) {
            return new[]
            {
                collection.ObserveResetChanged<T>(),
                collection.ObserveRemoveChanged<T>().Where(rm => EqualityComparer<T>.Default.Equals(source, rm)).ToUnit(),
            }.Merge().Take(1);
        }

        public static IObservable<V> ObserveUntilRemove<T, U, V>(T collection, Func<U, IObservable<V>> selector) where T : IEnumerable<U>, INotifyCollectionChanged {
            return ObserveCollectionItems<T, U>(collection)
                .SelectMany(item => selector(item).TakeUntil(item.ObserveRemoveFrom(collection)));
        }

        public static IObservable<U> ObserveUntilRemove<T, U>(this ReadOnlyObservableCollection<T> collection, Func<T, IObservable<U>> selector) {
            return ObserveUntilRemove<ReadOnlyObservableCollection<T>, T, U>(collection, selector);
        }

        public static IObservable<U> SelectSwitch<T, U>(this IObservable<T> source, Func<T, IObservable<U>> selector) {
            return source.Select(selector).Switch();
        }

        public static IObservable<T?> TakeNull<T>(this IObservable<T?> source) {
            return source.Where(x => x == null);
        }

        public static IObservable<T> SkipNull<T>(this IObservable<T?> source) {
            return source.Where(x => x != null).Select(x => x!);
        }

        public static IObservable<U> ToConstant<T, U>(this IObservable<T> source, U constant) {
            return source.Select(_ => constant);
        }

        public static IObservable<T> TakeFirstAfterEach<T, U>(this IObservable<T> source, IObservable<U> other) {
            return source.SkipUntil(other).Take(1).Repeat();
        }

        public static ReactiveProperty<TProperty> ToReactivePropertyWithCommit<TSubject, TProperty, TCommit, TDiscard>(this TSubject subject, Expression<Func<TSubject, TProperty>> propertySelector, IObservable<TCommit> commit, IObservable<TDiscard> discard, ReactivePropertyMode mode = ReactivePropertyMode.Default, bool ignoreValidationErrorValue = false) where TSubject : INotifyPropertyChanged {
            return subject.ToReactivePropertyAsSynchronized(
                propertySelector,
                op => op.Select(p => discard.Select(_ => p).StartWith(p)).Switch(),
                op => op.Sample(commit),
                mode,
                ignoreValidationErrorValue
            );
        }

        public static ReactiveProperty<TProperty> ToReactivePropertyWithCommit<TSubject, TProperty, TCommit>(this TSubject subject, Expression<Func<TSubject, TProperty>> propertySelector, IObservable<TCommit> commit, ReactivePropertyMode mode = ReactivePropertyMode.Default, bool ignoreValidationErrorValue = false) where TSubject : INotifyPropertyChanged {
            return subject.ToReactivePropertyWithCommit(propertySelector, commit, Observable.Never<Unit>(), mode, ignoreValidationErrorValue);
        }
    }
}
