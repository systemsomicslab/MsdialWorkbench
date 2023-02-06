using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace CompMs.Graphics.Design
{
    public sealed class GradientBrushMapper<T> : IBrushMapper<T> where T : IConvertible
    {
        private static readonly GradientStopCollection GRADIENT_STOPS;

        static GradientBrushMapper() {
            GRADIENT_STOPS  = new GradientStopCollection
            {
                new GradientStop(Colors.White, 0d),
                new GradientStop(Colors.Black, 1d),
            };

            GRADIENT_STOPS.Freeze();
        }

        private readonly GradientStopCollection _gradientStops;
        private readonly IAxisManager<double> _ax;

        public GradientBrushMapper(IAxisManager<double> ax, IList<GradientStop> gradientStops = null) {
            if (gradientStops != null && gradientStops.Count <= 0) {
                throw new ArgumentException(nameof(gradientStops));
            }
            _gradientStops = gradientStops is null ? GRADIENT_STOPS : new GradientStopCollection(gradientStops.OrderBy(gs => gs.Offset));
            if (!_gradientStops.IsFrozen) {
                _gradientStops.Freeze();
            }

            _ax = ax;
        }

        public GradientBrushMapper(double min, double max, IList<GradientStop> gradientStops = null) {
            if (gradientStops != null && gradientStops.Count <= 0) {
                throw new ArgumentException(nameof(gradientStops));
            }
            _gradientStops = gradientStops is null ? GRADIENT_STOPS : new GradientStopCollection(gradientStops.OrderBy(gs => gs.Offset));
            if (!_gradientStops.IsFrozen) {
                _gradientStops.Freeze();
            }

            _ax = new ContinuousAxisManager<double>(min, max);
        }

        public GradientStopCollection GradientStops => _gradientStops;

        public IAxisManager<double> AxisManager => _ax;

        public Brush Map(T key) {
            var value = Convert.ToDouble(key);
            var offset = Math.Max(0, Math.Min(1, (value - _ax.Range.Minimum.Value) / _ax.Range.Delta));
            var color = GetGradientColor(_gradientStops, offset);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public Brush Map(object key) {
            if (!(key is T convertible)) {
                return null;
            }
            return Map(convertible);
        }

        private static Color BlendColors(Color ca, Color cb, double factor) {
            var f = (float)factor;
            return cb * f + ca * (1 - f);
        }

        private static Color GetGradientColor(IList<GradientStop> gsc, double offset) {
            var lower = gsc.TakeWhile(gs => gs.Offset <= offset).LastOrDefault();
            var higher = gsc.SkipWhile(gs => gs.Offset <= offset).FirstOrDefault();
            if (lower is null) {
                return higher.Color;
            }
            if (higher is null) {
                return lower.Color;
            }
            var o = (offset - lower.Offset) / (higher.Offset - lower.Offset);
            return BlendColors(lower.Color, higher.Color, o);
        }
    }
}
