using CompMs.Graphics.Base;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace CompMs.Graphics.AxisManager
{
    public class DelegateBrushMapper<T> : BrushMapper<T>
    {
        public DelegateBrushMapper(Func<T, Brush> func) {
            this.func = func;
        }

        public DelegateBrushMapper(Func<T, Brush> func, bool enableCache) {
            this.func = func;
            this.enableCache = enableCache;
        }

        private readonly bool enableCache;
        private readonly Dictionary<T, Brush> cache = new Dictionary<T, Brush>();
        private readonly Func<T, Brush> func;

        public override Brush Map(T key) {
            if (enableCache) {
                if (cache.ContainsKey(key)) {
                    return cache[key];
                }
                return cache[key] = func(key);
            }
            return func(key);
        }

        public void CacheReflesh() {
            cache.Clear();
        }
    }
}
