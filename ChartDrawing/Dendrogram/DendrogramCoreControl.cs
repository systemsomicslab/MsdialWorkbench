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
                control.ChartManager = new DendrogramManager(
                    (DirectedTree)e.NewValue, control.XPositions, control.YPositions );
                control.ChartDrawingArea = control.ChartManager.ChartArea;
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

        
        public DendrogramCoreControl(): base()
        {
        }
    }
}
