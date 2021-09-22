using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Parser
{
    public delegate IEnumerable<IParseResult<T>> Parser<out T>(Source s);

    public static class Parsers
    {
        public static Parser<T> Return<T>(T x) {
            return s => new[] { new ParseResult<T>(x, s) };
        }

        public static Parser<Unit> ReturnUnit { get; } = Return(Unit.Default);

        private static Parser<IEnumerable<T>> None<T>() => Return(new T[] { });

        private static Parser<IEnumerable<T>> Optional<T>(this Parser<IEnumerable<T>> p) => p.Or(None<T>());

        public static Parser<string> Rest { get; } = s => new[] { new ParseResult<string>(s.Rest(), s) };

        public static Parser<T> Fail<T>() {
            return ParserCache<T>.Fail;
        }

        private static class ParserCache<T>
        {
            internal static readonly Parser<T> Fail;

            static ParserCache() {
                Fail = s => Enumerable.Empty<IParseResult<T>>();
            }
        }

        public static Parser<char> GetChar { get; } =
            s => s.IsEmpty
                ? Enumerable.Empty<IParseResult<char>>()
                : new[] { new ParseResult<char>(s.Head(), s.Next()) };

        public static Parser<char> Satisfy(Predicate<char> predicate) {
            return GetChar.Where(predicate);
        }

        public static Parser<Unit> Char(char c) {
            return Satisfy(x => x == c).Right(ReturnUnit);
        }

        public static Parser<Unit> String(string s) {
            if (string.IsNullOrEmpty(s)) {
                return ReturnUnit;
            }
            else {
                IEnumerable<IParseResult<Unit>> go(Source source) {
                    return string.Compare(s, 0, source.s, source.index, s.Length) == 0
                        ? new[] { new ParseResult<Unit>(Unit.Default, new Source(source.s, source.index + s.Length)), }
                        : Enumerable.Empty<IParseResult<Unit>>();
                }
                return go;
            }
        }

        public static Parser<Unit> Space { get; } = Satisfy(char.IsWhiteSpace).Some().Right(ReturnUnit);

        public static Parser<Unit> LineBreak { get; } = Char('\r').Right(Char('\n').GreedyOr(ReturnUnit)).GreedyOr(Char('\n'));

        public static Parser<T> Token<T>(this Parser<T> p) {
            return Space.Right(p);
        }

        private static Parser<int> Int { get; } = Satisfy(char.IsDigit).Some().Select(ds => int.Parse(new string(ds.ToArray())));
        private static Parser<Func<int, int>> Minus { get; } = Char('-').Right(Return<Func<int, int>>(x => -x)).OrDefault(x => x);
        public static Parser<int> Integer { get; } = Space.OrDefault().Right(Minus.Apply(Int));

        public static Parser<string> Line { get; } = LineImpl;
        private static IEnumerable<IParseResult<string>> LineImpl(Source s) {
            var idx = s.s.IndexOfAny(new[] { '\r', '\n' }, s.index);
            if (idx < 0) {
                idx = s.s.Length;
            }
            yield return new ParseResult<string>(s.s.Substring(s.index, idx - s.index), new Source(s.s, idx));
        }

        public static T Parse<T>(this Parser<T> p, Source s) {
            return p.Invoke(s).First().Result;
        }

        public static IEnumerable<T> Parses<T>(this Parser<T> p, string s) {
            return p.Invoke(new Source(s, 0)).Select(x => x.Result);
        }

        public static T Parse<T>(this Parser<T> p, string s) {
            return Parse(p, new Source(s, 0));
        }

        public static Parser<U> SelectMany<T, U>(this Parser<T> p, Func<T, Parser<U>> func) {
            return s => p.Invoke(s).SelectMany(x => func(x.Result).Invoke(x.Next));
        }

        public static Parser<V> SelectMany<T, U, V>(this Parser<T> p, Func<T, Parser<U>> selector, Func<T, U, V> resultSelector) {
            return p.SelectMany(x => selector(x).Select(y => resultSelector(x, y)));
        }

        public static Parser<U> Select<T, U>(this Parser<T> p, Func<T, U> func) {
            return p.SelectMany(x => Return(func(x)));
        }

        public static Parser<T> Back<T>(this Parser<T> p) {
            return s => p.Invoke(s).Select(x => new ParseResult<T>(x.Result, s));
        }

        public static Parser<T> Where<T>(this Parser<T> p, Predicate<T> predicate) {
            return p.SelectMany(x => predicate(x) ? Return(x) : Fail<T>());
        }

        public static Parser<U> Apply<T, U>(this Parser<Func<T, U>> f, Parser<T> p) {
            return f.SelectMany(x => p, (x, y) => x(y));
        }

        public static Parser<T> Left<T, U>(this Parser<T> p, Parser<U> q) {
            return p.SelectMany(x => q, (x, y) => x);
        }

        public static Parser<U> Right<T, U>(this Parser<T> p, Parser<U> q) {
            return p.SelectMany(x => q);
        }

        public static Parser<T> Or<T>(this Parser<T> p, Parser<T> q) {
            IEnumerable<IParseResult<T>> go(Source s) {
                foreach (var result in p.Invoke(s)) {
                    yield return result;
                }
                foreach (var result in q.Invoke(s)) {
                    yield return result;
                }
            }
            return go;
        }

        public static Parser<T> GreedyOr<T>(this Parser<T> p, Parser<T> q) {
            IEnumerable<IParseResult<T>> go(Source s) {
                var anyResult = false;
                foreach (var result in p.Invoke(s)) {
                    anyResult = true;
                    yield return result;
                }
                if (anyResult) {
                    yield break;
                }
                foreach (var result in q.Invoke(s)) {
                    yield return result;
                }
            }
            return go;
        }

        public static Parser<T> OrDefault<T>(this Parser<T> p, T default_ = default) {
            return p.Or(Return(default_));
        }

        public static Parser<IEnumerable<T>> Many<T>(this Parser<T> p) {
            return Some(p).Optional();
        }

        public static Parser<IEnumerable<T>> Some<T>(this Parser<T> p) {
            return p.SelectMany(x => Many(p), (x, xs) => xs.Prepend(x));
        }

        public static Parser<IEnumerable<T>> ManyWith<T, U>(this Parser<T> p, Parser<U> q) {
            return SomeWith(p, q).Optional();
        }

        public static Parser<IEnumerable<T>> SomeWith<T, U>(this Parser<T> p, Parser<U> q) {
            return p.SelectMany(x => Many(q.Right(p)), (x, xs) => xs.Prepend(x));
        }

        public static Parser<V> Combine<T, U, V>(this Parser<T> p, Parser<U> q, Func<T, U, V> func) {
            return p.SelectMany(x => q, func);
        }

        public static Parser<Tuple<T, U>> Combine<T, U>(this Parser<T> p, Parser<U> q) {
            return p.SelectMany(x => q, Tuple.Create);
        }

        public static Parser<IEnumerable<T>> Sequence<T>(this IEnumerable<Parser<T>> ps) {
            return ps.Aggregate(None<T>(), (acc, p) => acc.SelectMany(xs => p, Enumerable.Append));
        }

        public static Parser<IEnumerable<T>> SequenceWith<T, U>(this IEnumerable<Parser<T>> ps, Parser<U> q) {
            return ps.Select((p, i) => i == 0 ? p : q.Right(p)).Sequence();
        }

        public static Parser<T> ConsumeAll<T>(this Parser<T> p) {
            return s => p.Invoke(s).Where(x => x.Next.IsEmpty);
        }
    }

    public struct Unit
    {
        public static Unit Default { get; } = new Unit();

        public override bool Equals(object obj) {
            return obj is Unit;
        }

        public override int GetHashCode() {
            return 1;
        }
    }

    public class Source // TODO: replace System.ReadOnlySpan<char>
    {
        internal readonly string s;
        internal readonly int index;

        public Source(string s, int index) {
            if (index < 0) {
                throw new ArgumentException(nameof(index));
            }
            this.s = s ?? throw new ArgumentNullException(nameof(s));
            this.index = index;
        }

        public char Head() => s[index];

        public string Rest() {
            return s.Substring(index);
        }

        public bool IsEmpty => s.Length <= index;

        public Source Next() {
            return next ?? (next = new Source(s, index + 1));
        }
        private Source next;
    }

    public interface IParseResult<out T>
    {
        T Result { get; }
        Source Next { get; }
    }

    public class ParseResult<T> : IParseResult<T>
    {
        public ParseResult(T result, Source next) {
            Result = result;
            Next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public T Result { get; }
        public Source Next { get; }
    }
}
