using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

using PlottingControls.Base;
using Common.DataStructure;

namespace PlottingControls.Dendrogram
{
    public class DendrogramControl : PlottingBase
    {
        #region Property
        public Graph Dendrogram
        {
            get => (Graph)GetValue(DendrogramProperty);
            set => SetValue(DendrogramProperty, value);
        }
        public static readonly DependencyProperty DendrogramProperty = DependencyProperty.Register(
            "Dendrogram", typeof(Graph), typeof(DendrogramControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public int Root
        {
            get => (int)GetValue(RootProperty);
            set => SetValue(RootProperty, value);
        }
        public static readonly DependencyProperty RootProperty = DependencyProperty.Register(
            "Root", typeof(int), typeof(DendrogramControl),
            new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        #endregion

        public DendrogramControl() { }

        protected override void DrawChart(DrawingContext drawingContext)
        {
            var point = new Point(0, 0);
            var vector = new Vector(ActualWidth, ActualHeight);
            if(Dendrogram != null)
                DendrogramPainter.DrawTree(
                    drawingContext,
                    Dendrogram, Root,
                    XPositions, YPositions,
                    point, vector,
                    XDisplayMin, XDisplayMax,
                    YDisplayMin, YDisplayMax
                );
        }

        protected override void ResetDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                    var xValueMin = XPositions.Min();
                    var xValueMax = XPositions.Max();
                    XDisplayMin = xValueMin - (xValueMax - xValueMin) * 0.05;
                    XDisplayMax = xValueMax + (xValueMax - xValueMin) * 0.05;

                    var yValueMin = YPositions.Min();
                    var yValueMax = YPositions.Max();
                    YDisplayMin = yValueMin;
                    YDisplayMax = yValueMax + (yValueMax - yValueMin) * 0.05;
            }
        }
    }
}
