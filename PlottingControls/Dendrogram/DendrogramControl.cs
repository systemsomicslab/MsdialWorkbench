using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using PlottingControls.Base;
using Common.DataStructure;
using System.Windows.Media;

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

        protected override void PlotChart(DrawingContext drawingContext)
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
    }
}
