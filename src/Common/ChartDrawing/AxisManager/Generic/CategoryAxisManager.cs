using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Graphics.AxisManager.Generic
{
    public class CategoryAxisManager<U, T> : BaseAxisManager<T>
    {
        public CategoryAxisManager(
            IReadOnlyCollection<T> collection,
            Func<T, U> toKey,
            Func<T, string>? toLabel = null)
            : base(CountElement(collection, toKey)) {

            this.collection = collection;
            this.toKey = toKey;
            this.toLabel = toLabel ?? ToString;
            this.converter = ToDictionary(collection, toKey);
            ChartMargin = new RelativeMargin(0.05);
        }

        private static AxisRange CountElement(IReadOnlyCollection<T> collection, Func<T, U> toKey) {
            var set = collection.Select(toKey).ToHashSet();
            return new AxisRange(0d, set.Count);
        }

        private static Dictionary<U, AxisValue> ToDictionary(IEnumerable<T> xs, Func<T, U> toKey) {
            var result = new Dictionary<U, AxisValue>();
            var idx = 0;
            foreach (var x in xs) {
                var key = toKey(x);
                if (!result.ContainsKey(key)) {
                    result[key] = 0.5 + idx++;
                }
            }
            return result;
        }

        protected void UpdateCollection(IReadOnlyCollection<T> collection) {
            this.collection = collection;
            converter = ToDictionary(collection, toKey);
            UpdateInitialRange(CountElement(collection, toKey));
        }

        private static string ToString(T value) {
            return value.ToString();
        }

        private Dictionary<U, AxisValue> converter = new Dictionary<U, AxisValue>();
        private IReadOnlyCollection<T> collection;
        private readonly Func<T, U> toKey;
        private readonly Func<T, string> toLabel;

        public override List<LabelTickData> GetLabelTicks() {
            var result = new List<LabelTickData>();

            foreach(T item in collection)
            {
                result.Add(new LabelTickData()
                {
                    Label = toLabel(item),
                    TickType = TickType.LongTick,
                    Center = converter[toKey(item)],
                    Width = 1d,
                    Source = item,
                });
            }

            return result;
        }

        public override AxisValue TranslateToAxisValue(T value) {
            if (converter.TryGetValue(toKey(value), out var axv)) {
                return axv;
            }
            return AxisValue.NaN;
        }
    }

    public class CategoryAxisManager<T> : CategoryAxisManager<T, T>
    {
        private static T Identity(T value) {
            return value;
        }

        public CategoryAxisManager(IReadOnlyCollection<T> collection, Func<T, string>? toLabel = null)
            : base(collection, Identity, toLabel) {
        }
    }
}
