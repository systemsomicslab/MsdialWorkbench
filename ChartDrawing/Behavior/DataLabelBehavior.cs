using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

using CompMs.Graphics.Core.Adorner;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.Behavior
{
    public class DataLabelBehavior : Behavior<ChartControl>
    {
        public DataLabelBehavior()
        {
            memo = new Dictionary<int, DataLabelAdorner>();
        }

        public IReadOnlyList<string> Labels
        {
            get => (IReadOnlyList<string>)GetValue(LabelsProperty);
            set => SetValue(LabelsProperty, value);
        }
        public static readonly DependencyProperty LabelsProperty = DependencyProperty.Register(
            nameof(Labels), typeof(IReadOnlyList<string>), typeof(DataLabelBehavior),
            new PropertyMetadata(default, OnLabelsUpdate)
            );
        private static void OnLabelsUpdate(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = d as DataLabelBehavior;
            if (behavior == null) return;
            behavior.ResetMemo();
        }
        public IReadOnlyList<double> XPositions
        {
            get => (IReadOnlyList<double>)GetValue(XPositionsProperty);
            set => SetValue(XPositionsProperty, value);
        }
        public static readonly DependencyProperty XPositionsProperty = DependencyProperty.Register(
            nameof(XPositions), typeof(IReadOnlyList<double>), typeof(DataLabelBehavior),
            new PropertyMetadata(default)
            );

        public IReadOnlyList<double> YPositions
        {
            get => (IReadOnlyList<double>) GetValue(YPositionsProperty);
            set => SetValue(YPositionsProperty, value);
        }
        public static readonly DependencyProperty YPositionsProperty = DependencyProperty.Register(
            nameof(YPositions), typeof(IReadOnlyList<double>), typeof(DataLabelBehavior),
            new PropertyMetadata(default(IReadOnlyList<double>))
            );
        
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseMove += ShowDataLabelOnMove;
            AssociatedObject.MouseLeave += ShowDataLabelOnLeave;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseMove -= ShowDataLabelOnMove;
            AssociatedObject.MouseLeave -= ShowDataLabelOnLeave;
        }

        void ShowDataLabelOnMove(object sender, MouseEventArgs e)
        {
            if (XPositions == null || XPositions.Count < Labels.Count)
                XPositions = new double[Labels.Count];
            if (YPositions == null || YPositions.Count < Labels.Count)
                YPositions = new double[Labels.Count];
            var point = AssociatedObject.ChartManager.Translate(
                e.GetPosition(AssociatedObject),
                AssociatedObject.ChartDrawingArea,
                AssociatedObject.RenderSize
                );
            var dist = double.MaxValue;
            var idx_ = -1;
            for(int i = 0; i < Labels.Count; ++i){
                var d = hypotSq(
                    point.X - XPositions[i],
                    point.Y - YPositions[i]
                    );
                if (d < dist){
                    dist = d;
                    idx_ = i;
                }
            }

            if (idx == idx_)
                return;
            idx = idx_;

            if (current != null){
                current.Detach();
                current = null;
            }
            if (!memo.ContainsKey(idx))
            {
                memo[idx] = new DataLabelAdorner(
                    AssociatedObject, Labels[idx]
                    );
            }
            current = memo[idx];
            current.Attach();
            current.Position = AssociatedObject.ChartManager.Inverse(
                new Point(XPositions[idx], YPositions[idx]),
                AssociatedObject.ChartDrawingArea,
                AssociatedObject.RenderSize
                );
        }

        void ShowDataLabelOnLeave(object sender, MouseEventArgs e)
        {
            if (current != null)
            {
                current.Detach();
                current = null;
                idx = -1;
            }
        }

        public void ResetMemo()
        {
            memo = new Dictionary<int, DataLabelAdorner>();
        }

        static double hypotSq(double dx, double dy){
            return dx * dx + dy * dy;
        }

        Dictionary<int, DataLabelAdorner> memo;
        int idx = -1;
        DataLabelAdorner current = null;
    }
}
