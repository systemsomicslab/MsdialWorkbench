using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Common.DataStructure;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.Dendrogram
{
    public class DendrogramCoreControl : ChartControl
    {
        public DirectedTree Dendrogram
        {
            get => (DirectedTree)GetValue(DendrogramProperty);
            set => SetValue(DendrogramProperty, value);
        }
        public static readonly DependencyProperty DendrogramProperty = DependencyProperty.Register(
            "Dendrogram", typeof(DirectedTree), typeof(DendrogramCoreControl),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnDendrogramChanged)
            );
        static void OnDendrogramChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as DendrogramCoreControl;
            if (control != null)
            {
                var dendrogram = (DirectedTree)e.NewValue;
                var chartmanager = new DendrogramManager(
                    dendrogram, control.XPositions, control.YPositions );
                control.ChartDrawingArea = chartmanager.ChartArea;
                control.XPositions = chartmanager.XPositions;
                control.YPositions = chartmanager.YPositions;
                control.LeafPositions = dendrogram.Leaves.OrderBy(i => i).Select(i => control.XPositions[i]).ToArray();
                control.LimitDrawingArea = chartmanager.ChartArea;
                control.ChartManager = chartmanager;
            }
        }

        public IReadOnlyList<double> XPositions
        {
            get => (IReadOnlyList<double>)GetValue(XPositionsProperty);
            set => SetValue(XPositionsProperty, value);
        }
        public static readonly DependencyProperty XPositionsProperty = DependencyProperty.Register(
            nameof(XPositions), typeof(IReadOnlyList<double>), typeof(DendrogramCoreControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
            );
        public IReadOnlyList<double> YPositions
        {
            get => (IReadOnlyList<double>)GetValue(YPositionsProperty);
            set => SetValue(YPositionsProperty, value);
        }
        public static readonly DependencyProperty YPositionsProperty = DependencyProperty.Register(
            nameof(YPositions), typeof(IReadOnlyList<double>), typeof(DendrogramCoreControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
            );

        public IReadOnlyList<double> LeafPositions
        {
            get => (IReadOnlyList<double>)GetValue(LeafPositionsProperty);
            private set => SetValue(LeafPositionsPropertyKey, value);
        }
            // => Dendrogram?.Leaves.OrderBy(i => i).Select(i => XPositions[i]).ToArray();
        private static readonly DependencyPropertyKey LeafPositionsPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(LeafPositions), typeof(IReadOnlyList<double>), typeof(DendrogramCoreControl),
            new FrameworkPropertyMetadata(null)
            );
        public static readonly DependencyProperty LeafPositionsProperty = LeafPositionsPropertyKey.DependencyProperty;
    }
}
