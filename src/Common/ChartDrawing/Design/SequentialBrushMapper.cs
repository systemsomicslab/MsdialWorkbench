using CompMs.Graphics.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace CompMs.Graphics.Design
{
    public class SequentialBrushMapper : BrushMapper<int>
    {
        public SequentialBrushMapper(IEnumerable<Brush> brushes) {
            this.brushes = brushes.ToList();
            loop = true;
        }

        public SequentialBrushMapper(IEnumerable<Brush> brushes, Brush remains) {
            this.brushes = brushes.ToList();
            this.remains = remains;
        }

        public SequentialBrushMapper(IEnumerable<Color> colors) {
            brushes = colors.Select(color => new SolidColorBrush(color)).ToList();
            foreach (var brush in brushes) {
                brush.Freeze();
            }
            loop = true;
        }

        public SequentialBrushMapper(IEnumerable<Color> colors, Color remains) {
            brushes = colors.Select(color => new SolidColorBrush(color)).ToList();
            foreach (var brush in brushes) {
                brush.Freeze();
            }
            this.remains = new SolidColorBrush(remains);
            this.remains.Freeze();
        }

        private readonly IReadOnlyList<Brush> brushes;
        private readonly Brush remains;
        private readonly bool loop;

        public override Brush Map(int key) {
            if (key < 0)
                throw new ArgumentException("key should be greater than or equal 0.");
            if (key < brushes.Count) {
                return brushes[key];
            }
            if (loop) {
                return brushes[key % brushes.Count];
            }
            if (remains != null) {
                return remains;
            }
            throw new NotImplementedException();
        }
    }
}
