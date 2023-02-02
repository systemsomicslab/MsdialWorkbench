using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.SpectrumViewer.Common
{
    public static class ObservableExtension
    {
        enum CalmFlag
        {
            Add = 0,
            Remove = 1,
        }

        public static (IObservable<T>, IObservable<T>) Calm<T>(IObservable<T> add, IObservable<T> remove, TimeSpan span) {
            var values = add.WithLatestFrom(Observable.Return(CalmFlag.Add))
                .Merge(remove.WithLatestFrom(Observable.Return(CalmFlag.Remove)))
                .Buffer(span)
                .SelectMany(CalmImpl);
            return (values.Where(v => v.Second == CalmFlag.Add).Select(v => v.First),
                values.Where(v => v.Second == CalmFlag.Remove).Select(v => v.First));
        }

        private static IEnumerable<(T First, CalmFlag Second)> CalmImpl<T>(IList<(T First, CalmFlag Second)> xs) {
            var ys = new Dictionary<(T, CalmFlag), int>();
            foreach (var x in xs) {
                var y = (x.First, x.Second ^ (CalmFlag)1);
                if (ys.TryGetValue(y, out var n) && n >= 1) {
                    --ys[y];
                }
                else if (ys.ContainsKey(x)) {
                    ++ys[x];
                }
                else {
                    ys.Add(x, 1);
                }
            }
            return ys.SelectMany(y => Enumerable.Repeat(y.Key, y.Value));
        }
    }
}
