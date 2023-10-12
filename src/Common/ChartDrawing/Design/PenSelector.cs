using System.Collections.Generic;
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
            if (mapper is null) {
                Reset();
            }
            else {
                strategy = new BrushMapStrategy(mapper, thickness);
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
            public BrushMapStrategy(IBrushMapper mapper, double thickness) {
                Mapper = mapper;
                Thickness = thickness;
            }

            private readonly IBrushMapper Mapper;
            private readonly double Thickness;
            private readonly Dictionary<object, Pen> cache = new Dictionary<object, Pen>();

            public Pen Get(object o) {
                if (cache.ContainsKey(o)) {
                    return cache[o];
                }
                var brush = Mapper.Map(o);
                var pen = new Pen(brush, Thickness);
                pen.Freeze();
                if (cache.Count > 1_000_000) {
                    cache.Clear();
                }
                return cache[o] = pen;
            }
        }
    }
}