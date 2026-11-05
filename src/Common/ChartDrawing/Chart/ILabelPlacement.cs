using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace CompMs.Graphics.Chart
{
    /// <summary>Places labels in priority order using coordinates relative to the plot.</summary>
    public interface ILabelPlacement
    {
        /// <summary>Optional pen connecting horizontally displaced labels to their anchors.</summary>
        Pen? LeaderLinePen { get; }

        // Anchors are sorted by screen X and include visible items without labels.
        bool TryPlaceLabel(LabelLayout candidate, IReadOnlyList<LabelLayout> accepted, IReadOnlyList<Point> anchors, Size plotSize, bool flippedY, out Point origin);
    }

    /// <summary>A centered label's text, original anchor, and drawing origin, in plot coordinates.</summary>
    public readonly struct LabelLayout
    {
        public LabelLayout(FormattedText text, Point anchor, Point origin) {
            Text = text;
            Anchor = anchor;
            Origin = origin;
        }

        public FormattedText Text { get; }
        public Point Anchor { get; }
        public Point Origin { get; }

        // Ink bounds exclude line leading; the full line region is used to control density.
        public Rect Bounds => new Rect(Origin.X - Text.Width / 2d,
            Origin.Y + Text.Height + Text.OverhangAfter - Text.Extent, Text.Width, Text.Extent);
        public Rect Region => new Rect(Origin.X - Text.Width / 2d, Origin.Y, Text.Width, Text.Height);
    }
}
