using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using CompMs.Graphics.Base;

namespace CompMs.Graphics.Design
{
    public class PenSelector
    {
        private ISelectStrategy strategy;
        private static readonly ISelectStrategy DefaultStrategy;

        static PenSelector() {
            var defaultPen = new Pen(Brushes.Black, 2);
            defaultPen.Freeze();
            DefaultStrategy = new ConstantStrategy(defaultPen);
        }

        public PenSelector() {
            strategy = DefaultStrategy;
        }

        public Pen GetPen(object o) => strategy?.Get(o);

        public void Update(Pen pen) {
            if (pen is null) {
                Reset();
            }
            else {
                strategy = new ConstantStrategy(pen);
            }
        }

        public void Update(IBrushMapper mapper, double thickness) {
            Update(mapper, thickness, null);
        }

        public void Update(IBrushMapper mapper, double thickness, DoubleCollection strokeDashArray) {
            if (mapper is null) {
                Reset();
            }
            else {
                strategy = new BrushMapStrategy(mapper, thickness, strokeDashArray);
            }
        }

        public void Reset() {
            strategy = DefaultStrategy;
        }

        interface ISelectStrategy
        {
            Pen Get(object o);
        }

        class ConstantStrategy : ISelectStrategy
        {
            public ConstantStrategy(Pen pen) {
                Pen = pen;
            }

            private readonly Pen Pen;
            public Pen Get(object o) {
                return Pen;
            }
        }

        class BrushMapStrategy : ISelectStrategy
        {
            public BrushMapStrategy(IBrushMapper mapper, double thickness, DoubleCollection strokeDashArray) {
                Mapper = mapper;
                Thickness = thickness;
                StrokeDashArray = strokeDashArray?.ToArray();
            }

            private readonly IBrushMapper Mapper;
            private readonly double Thickness;
            private readonly double[] StrokeDashArray;
            private readonly Dictionary<object, Pen> cache = new Dictionary<object, Pen>();

            public Pen Get(object o) {
                if (cache.ContainsKey(o)) {
                    return cache[o];
                }
                var brush = Mapper.Map(o);
                var pen = new Pen(brush, Thickness);
                if (StrokeDashArray is { Length: > 0 }) {
                    pen.DashStyle = new DashStyle(new DoubleCollection(StrokeDashArray), 0d);
                }
                pen.Freeze();
                if (cache.Count > 1_000_000) {
                    cache.Clear();
                }
                return cache[o] = pen;
            }
        }
    }
}