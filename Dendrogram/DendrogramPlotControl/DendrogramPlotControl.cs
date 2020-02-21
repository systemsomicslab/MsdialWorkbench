using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using Common.DataStructure;

namespace Msdial.Dendrogram
{
    public class DendrogramPlotControl : FrameworkElement
    {
        #region Property
        public Graph Dendrogram
        {
            get => (Graph)GetValue(DendrogramProperty);
            set => SetValue(DendrogramProperty, value);
        }
        public static readonly DependencyProperty DendrogramProperty = DependencyProperty.Register(
            "Dendrogram", typeof(Graph), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public int Root
        {
            get => (int)GetValue(RootProperty);
            set => SetValue(RootProperty, value);
        }
        public static readonly DependencyProperty RootProperty = DependencyProperty.Register(
            "Root", typeof(int), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public IReadOnlyList<double> XPositions
        {
            get => (IReadOnlyList<double>)GetValue(XPositionsProperty);
            set => SetValue(XPositionsProperty, value);
        }
        public static readonly DependencyProperty XPositionsProperty = DependencyProperty.Register(
            "XPositions", typeof(IReadOnlyList<double>), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public IReadOnlyList<double> YPositions
        {
            get => (IReadOnlyList<double>)GetValue(YPositionsProperty);
            set => SetValue(YPositionsProperty, value);
        }
        public static readonly DependencyProperty YPositionsProperty = DependencyProperty.Register(
            "YPositions", typeof(IReadOnlyList<double>), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double XDisplayMin
        {
            get => (double)GetValue(XDisplayMinProperty);
            set => SetValue(XDisplayMinProperty, value);
        }
        public static readonly DependencyProperty XDisplayMinProperty = DependencyProperty.Register(
            "XDisplayMin", typeof(double), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double XDisplayMax
        {
            get => (double)GetValue(XDisplayMaxProperty);
            set => SetValue(XDisplayMaxProperty, value);
        }
        public static readonly DependencyProperty XDisplayMaxProperty = DependencyProperty.Register(
            "XDisplayMax", typeof(double), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double YDisplayMin
        {
            get => (double)GetValue(YDisplayMinProperty);
            set => SetValue(YDisplayMinProperty, value);
        }
        public static readonly DependencyProperty YDisplayMinProperty = DependencyProperty.Register(
            "YDisplayMin", typeof(double), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double YDisplayMax
        {
            get => (double)GetValue(YDisplayMaxProperty);
            set => SetValue(YDisplayMaxProperty, value);
        }
        public static readonly DependencyProperty YDisplayMaxProperty = DependencyProperty.Register(
            "YDisplayMax", typeof(double), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public IReadOnlyList<int> LeafIdxs
        {
            get => (IReadOnlyList<int>)GetValue(LeafIdxsProperty);
            set => SetValue(LeafIdxsProperty, value);
        }
        public static readonly DependencyProperty LeafIdxsProperty = DependencyProperty.Register(
            "LeafIdxs", typeof(IReadOnlyList<int>), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(new List<int>(), FrameworkPropertyMetadataOptions.AffectsRender)
        );
        #endregion

        public DendrogramPlotControl() { }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var point = new Point(0, 0);
            var vector = new Vector(ActualWidth, ActualHeight);
            DendrogramPlotPainter.DrawBackground(
                drawingContext, point, vector
            );
            if(Dendrogram != null)
                DendrogramPlotPainter.DrawTree(
                    drawingContext,
                    Dendrogram, Root,
                    XPositions, YPositions,
                    point, vector,
                    XDisplayMin, XDisplayMax,
                    YDisplayMin, YDisplayMax
                );
            // Console.WriteLine("OnRender called");
        }
    }
}
