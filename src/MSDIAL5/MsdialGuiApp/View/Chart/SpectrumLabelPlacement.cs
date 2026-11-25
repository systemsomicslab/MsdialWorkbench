using CompMs.Common.Utility;
using CompMs.Graphics.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.View.Chart
{
    public sealed class SpectrumLabelPlacement : ILabelPlacement
    {
        private static readonly Pen leaderLinePen = new Pen(Brushes.Gray, .5d) { DashStyle = DashStyles.Dash };

        static SpectrumLabelPlacement() => leaderLinePen.Freeze();

        public Pen LeaderLinePen => leaderLinePen;

        public bool TryPlaceLabel(LabelLayout candidate, IReadOnlyList<LabelLayout> accepted, IReadOnlyList<Point> anchors, Size plotSize, bool flippedY, out Point origin) {
            origin = candidate.Origin;
            // Similar heights need half a label width; large height differences can use less.
            // Original peak positions keep shifting from bypassing this zoom-dependent spacing.
            foreach (var other in accepted) {
                var heightSeparation = Math.Abs(candidate.Anchor.Y - other.Anchor.Y) / plotSize.Height;
                var minimumSpacing = (candidate.Text.Width + other.Text.Width) / 2d * Math.Max(.1d, .5d - heightSeparation);
                if (Math.Abs(candidate.Anchor.X - other.Anchor.X) < minimumSpacing) {
                    return false;
                }
            }

            var relativeHeight = Math.Max(0d, Math.Min(1d, flippedY ? 1d - candidate.Anchor.Y / plotSize.Height : candidate.Anchor.Y / plotSize.Height));
            var canShift = relativeHeight * plotSize.Height >= candidate.Text.Height;
            var bounds = candidate.Bounds;
            foreach (var other in accepted.OrderBy(label => label.Bounds.Left)) {
                if (!bounds.IntersectsWith(other.Bounds)) {
                    continue;
                }
                var shift = other.Bounds.Right - bounds.Left + 2d;
                // Keep a shifted label near its peak and inside the right plot edge.
                if (!canShift || origin.X + shift - candidate.Origin.X > candidate.Text.Width / 2d || bounds.Right + shift > plotSize.Width) {
                    return false;
                }
                origin.X += shift;
                bounds.Offset(shift, 0d);
            }

            // Weak peaks need horizontal space; at half height vertical clearance has full weight.
            var verticalWeight = relativeHeight >= .5d ? 1d : relativeHeight / (1d - relativeHeight);
            bool IsCrowded(LabelLayout layout) => accepted.Any(other => {
                var dx = Math.Abs(layout.Region.Left + layout.Region.Width / 2d - other.Region.Left - other.Region.Width / 2d);
                var dy = Math.Abs(layout.Region.Top - other.Region.Top);
                return dx / ((layout.Region.Width + other.Region.Width) / 2d)
                    + dy / ((layout.Region.Height + other.Region.Height) / 2d) * verticalWeight < 1d;
            });
            var leastObstructions = 2;
            // Peaks shorter than one text line stay centered; visible peaks may move either way.
            foreach (var shift in new[] { origin.X - candidate.Origin.X, candidate.Text.Width / 2d, -candidate.Text.Width / 2d }) {
                if (shift != 0d && !canShift) {
                    continue;
                }
                var proposed = new Point(candidate.Origin.X + shift, candidate.Origin.Y);
                var layout = new LabelLayout(candidate.Text, candidate.Anchor, proposed);
                if (shift != 0d && (layout.Bounds.Left < 0d || layout.Bounds.Right > plotSize.Width)) {
                    continue;
                }
                if (accepted.Any(other => layout.Bounds.IntersectsWith(other.Bounds)) || IsCrowded(layout)) {
                    continue;
                }
                var obstructions = CountObstructions(anchors, layout.Bounds, flippedY);
                if (!canShift && obstructions > 0) {
                    continue;
                }
                if (obstructions < leastObstructions) {
                    leastObstructions = obstructions;
                    origin = proposed;
                    if (obstructions == 0) {
                        return true;
                    }
                }
            }
            return leastObstructions < 2;
        }

        private static int CountObstructions(IReadOnlyList<Point> anchors, Rect bounds, bool flippedY) {
            var count = 0;
            var lastColumn = double.NaN;
            var start = anchors.LowerBound(bounds.Left, (point, x) => point.X.CompareTo(x));
            for (int index = start; index < anchors.Count && anchors[index].X <= bounds.Right; index++) {
                var point = anchors[index];
                if (flippedY ? point.Y > bounds.Bottom : point.Y < bounds.Top) {
                    continue;
                }
                // Samples in one screen column form one stroke; two obstructions already reject a label.
                var column = Math.Round(point.X);
                if (column != lastColumn) {
                    lastColumn = column;
                    if (++count == 2) {
                        return count;
                    }
                }
            }
            return count;
        }
    }
}
