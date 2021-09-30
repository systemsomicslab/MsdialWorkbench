using CompMs.Common.Enum;
using CompMs.Graphics.Chart;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CompMs.Graphics.Specialized
{
    public class LinkedPeakAnnotator : ScatterControl
    {
        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register(
                nameof(Target), typeof(ChromatogramPeakFeature), typeof(LinkedPeakAnnotator),
                new PropertyMetadata(null, ChartUpdate));

        public ChromatogramPeakFeature Target {
            get => (ChromatogramPeakFeature)GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        public static readonly DependencyProperty GroupPeaksProperty =
            DependencyProperty.Register(
                nameof(GroupPeaks), typeof(IReadOnlyList<ChromatogramPeakFeature>), typeof(LinkedPeakAnnotator),
                new PropertyMetadata(new List<ChromatogramPeakFeature>()));

        public IReadOnlyList<ChromatogramPeakFeature> GroupPeaks {
            get => (IReadOnlyList<ChromatogramPeakFeature>)GetValue(GroupPeaksProperty);
            set => SetValue(GroupPeaksProperty, value);
        }

        private DrawingVisual AnnotationLayer => dv ?? (dv = new DrawingVisual());
        private DrawingVisual dv;

        protected override void Update() {
            base.Update();

            if (  HorizontalAxis == null
               || VerticalAxis == null
               || PointBrush == null
               || Target == null
               || GroupPeaks == null
               )
                return;

            var target = Target;
            double radius = Radius, actualWidth = ActualWidth, actualHeight = ActualHeight;

            var dv = AnnotationLayer;
            if (dv.Parent == null)
                visualChildren.Add(dv);
            bool flipX = FlippedX, flipY = FlippedY;
            var small = PointGeometry.Clone();
            var large = PointGeometry.Clone();
            var rect = small.Bounds;
            var small_scale = Radius / Math.Max(rect.Width, rect.Height);
            small.Transform = new ScaleTransform(small_scale, small_scale, rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            var large_scale = (Radius+1) / Math.Max(rect.Width, rect.Height);
            large.Transform = new ScaleTransform(large_scale, large_scale, rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            var geometry = new CombinedGeometry(GeometryCombineMode.Exclude, large, small);
            geometry.Freeze();

            var group = GroupPeaks.Where(peak => target.PeakCharacter.PeakGroupID == peak.PeakCharacter.PeakGroupID).ToArray();

            using (var dc = dv.RenderOpen()) {

                var donelist = new List<string>();
                var lineDoneList = new List<string>();
                var yLabelPositions = new List<double>();
                var peakID = target.PeakID;
                var labelYDistance = 25;

                foreach (var sameGroupPeak in group.Where(peak => peak.PeakID == peakID)) {

                    var xs = HorizontalAxis.TranslateToRenderPoint(sameGroupPeak.ChromXs.Value, flipX, actualWidth);
                    var ys = VerticalAxis.TranslateToRenderPoint(sameGroupPeak.Mass, flipY, actualHeight);

                    // showing isotope adduct information
                    #region
                    var formattedText = new FormattedText(
                        "M + " + sameGroupPeak.PeakCharacter.IsotopeWeightNumber.ToString(),
                         System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                         FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray, 1)
                    {
                        TextAlignment = TextAlignment.Left,
                    };
                    dc.DrawText(formattedText, new Point(xs + 30 * 0.333, ys));

                    if (sameGroupPeak.PeakCharacter.IsotopeWeightNumber == 0) {
                        formattedText = new FormattedText(
                            sameGroupPeak.PeakCharacter.AdductType.AdductIonName,
                            System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray, 1)
                        {
                            TextAlignment = TextAlignment.Center
                        };
                        dc.DrawText(formattedText, new Point(xs, ys + labelYDistance * 0.5));

                        if (sameGroupPeak.PeakCharacter.AdductTypeByAmalgamationProgram != null && sameGroupPeak.PeakCharacter.AdductTypeByAmalgamationProgram.FormatCheck) {
                            formattedText = new FormattedText(
                                sameGroupPeak.PeakCharacter.AdductTypeByAmalgamationProgram.AdductIonName + " (Amal.)",
                                System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Red, 1)
                            {
                                TextAlignment = TextAlignment.Center
                            };
                            dc.DrawText(formattedText, new Point(xs, ys + labelYDistance * 0.5 * 2.0));
                        }
                    }
                    #endregion

                    foreach (var linkedPeakCharacter in sameGroupPeak.PeakCharacter.PeakLinks) {

                        var character = linkedPeakCharacter.Character;
                        var linkedPeak = group.FirstOrDefault(peak => peak.PeakID == linkedPeakCharacter.LinkedPeakID);

                        var xe = HorizontalAxis.TranslateToRenderPoint(linkedPeak.ChromXs.Value, flipX, actualWidth);
                        var ye = VerticalAxis.TranslateToRenderPoint(linkedPeak.Mass, flipY, actualHeight);

                        var blushColor = Brushes.Blue;
                        var xOffset = 30;
                        var yTextOffset = 30;

                        var characterString = string.Empty;
                        switch (character) {
                            case PeakLinkFeatureEnum.SameFeature:
                                blushColor = Brushes.Gray;
                                characterString = "Same metabolite name";
                                break;
                            case PeakLinkFeatureEnum.Isotope:
                                blushColor = Brushes.Red;
                                characterString = "M + " + linkedPeak.PeakCharacter.IsotopeWeightNumber.ToString();
                                break;
                            case PeakLinkFeatureEnum.Adduct:
                                blushColor = Brushes.Blue;
                                characterString = linkedPeak.PeakCharacter.AdductType.AdductIonName;
                                yTextOffset = -30;
                                break;
                            case PeakLinkFeatureEnum.ChromSimilar:
                                blushColor = Brushes.Green;
                                characterString = "Chromatogram similar";
                                xOffset = -30;
                                break;
                            case PeakLinkFeatureEnum.FoundInUpperMsMs:
                                blushColor = Brushes.Pink;
                                characterString = "Found in upper MS/MS";
                                xOffset = -30;
                                yTextOffset = -30;
                                break;
                        }

                        var index = Math.Min(linkedPeak.PeakID, sameGroupPeak.PeakID) + "_" +
                            Math.Max(linkedPeak.PeakID, sameGroupPeak.PeakID) + "_" +
                            characterString;
                        if (donelist.Contains(index)) continue;
                        else donelist.Add(index);

                        var lineIndex = Math.Min(linkedPeak.PeakID, sameGroupPeak.PeakID) + "_" +
                            Math.Max(linkedPeak.PeakID, sameGroupPeak.PeakID) + "_" +
                            blushColor.ToString();

                        if (Math.Abs(ye - ys) >= 20) {

                            var yOffset = ye - ys > 0 ? -10 : 10;

                            if (!lineDoneList.Contains(lineIndex)) {
                                dc.DrawLine(new Pen(blushColor, 1),
                                    new Point(xs - xOffset - (xs - xe), ys - yOffset),
                                    new Point(xe - xOffset, ye + yOffset));
                                dc.DrawLine(new Pen(blushColor, 1),
                                    new Point(xs, ys),
                                    new Point(xs - xOffset - (xs - xe), ys - yOffset));
                                dc.DrawLine(new Pen(blushColor, 1),
                                    new Point(xe, ye),
                                    new Point(xe - xOffset, ye + yOffset));

                                lineDoneList.Add(lineIndex);
                            }

                            dc.DrawRectangle(
                                new DrawingBrush(new GeometryDrawing(blushColor, null, geometry)),
                                null,
                                new Rect(xe - radius, ye - radius, radius * 2, radius * 2));
                            dc.DrawRectangle(
                                new DrawingBrush(new GeometryDrawing(blushColor, null, geometry)),
                                null,
                                new Rect(xs - radius, ys - radius, radius * 2, radius * 2));

                            formattedText = new FormattedText(
                                characterString,
                                System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight, new Typeface("Calibri"), 13,
                                Brushes.Gray, 1)
                            {
                                TextAlignment = TextAlignment.Left,
                            };

                            var xPoint = 0.0;
                            var yPoint = 0.0;
                            if (characterString == "Found in upper MS/MS" || characterString == "Chromatogram similar") {
                                xPoint = xe - xOffset + 5;
                                yPoint = - yTextOffset * 0.333 - 2.5 + (ye + ys) * 0.5;
                            }
                            else if (character == PeakLinkFeatureEnum.Adduct) {
                                formattedText.TextAlignment = TextAlignment.Center;
                                xPoint = xe;
                                yPoint = ye - labelYDistance * 0.5;
                            }
                            else {
                                xPoint = xe + xOffset * 0.333;
                                yPoint = - yTextOffset * 0.2 + ye;
                            }

                            var isLabeledOverlaped = false;
                            foreach (var yLabeledPos in yLabelPositions) {
                                if (Math.Abs(yLabeledPos - yPoint) < 30) {
                                    isLabeledOverlaped = true;
                                    break;
                                }
                            }

                            if (isLabeledOverlaped == false || character == PeakLinkFeatureEnum.Adduct) {
                                dc.DrawText(formattedText, new Point(xPoint, yPoint));
                                yLabelPositions.Add(yPoint);
                            }
                        }
                    }
                }
            }
        }
    }
}
