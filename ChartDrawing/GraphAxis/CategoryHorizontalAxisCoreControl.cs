using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.GraphAxis
{
    public class CategoryHorizontalAxisCoreControl : ChartControl
    {
        public IReadOnlyList<double> XPositions
        {
            get => (IReadOnlyList<double>)GetValue(XPositionsProperty);
            set => SetValue(XPositionsProperty, value);
        }
        public static readonly DependencyProperty XPositionsProperty = DependencyProperty.Register(
            nameof(XPositions), typeof(IReadOnlyList<double>), typeof(CategoryHorizontalAxisCoreControl),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnXPositionsChanged)
            );
        static void OnXPositionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => Update(d);

        public IReadOnlyList<string> Labels
        {
            get => (IReadOnlyList<string>)GetValue(LabelsProperty);
            set => SetValue(LabelsProperty, value);
        }
        public static readonly DependencyProperty LabelsProperty = DependencyProperty.Register(
            nameof(Labels), typeof(IReadOnlyList<string>), typeof(CategoryHorizontalAxisCoreControl),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnLabelsChanged)
            );
        static void OnLabelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => Update(d);

        public int Limit
        {
            get => (int)GetValue(LimitProperty);
            set => SetValue(LimitProperty, value);
        }
        public static readonly DependencyProperty LimitProperty = DependencyProperty.Register(
            nameof(Limit), typeof(int), typeof(CategoryHorizontalAxisCoreControl),
            new FrameworkPropertyMetadata(-1,
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnLimitChanged)
            );
        static void OnLimitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => Update(d);

        public CategoryHorizontalAxisCoreControl() : base()
        {
            background = new BackgroundManager(Brushes.WhiteSmoke, null);
        }

        static void Update(DependencyObject d)
        {
            var control = d as CategoryHorizontalAxisCoreControl;
            if (control != null && control.XPositions != null)
            {
                control.ChartManager = new CategoryHorizontalAxisManager(
                    control.XPositions, control.Labels, control.Limit);
                control.ChartDrawingArea = control.ChartManager.ChartArea;
                control.LimitDrawingArea = control.ChartManager.ChartArea;
            }
        }
    }
}
