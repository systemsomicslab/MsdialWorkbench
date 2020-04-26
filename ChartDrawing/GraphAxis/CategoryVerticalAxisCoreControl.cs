using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.GraphAxis
{
    public class CategoryVerticalAxisCoreControl : ChartControl
    {
        public IReadOnlyList<double> YPositions
        {
            get => (IReadOnlyList<double>)GetValue(YPositionsProperty);
            set => SetValue(YPositionsProperty, value);
        }
        public static readonly DependencyProperty YPositionsProperty = DependencyProperty.Register(
            nameof(YPositions), typeof(IReadOnlyList<double>), typeof(CategoryVerticalAxisCoreControl),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnYPositionsChanged)
            );
        static void OnYPositionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => Update(d);

        public IReadOnlyList<string> Labels
        {
            get => (IReadOnlyList<string>)GetValue(LabelsProperty);
            set => SetValue(LabelsProperty, value);
        }
        public static readonly DependencyProperty LabelsProperty = DependencyProperty.Register(
            nameof(Labels), typeof(IReadOnlyList<string>), typeof(CategoryVerticalAxisCoreControl),
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
            nameof(Limit), typeof(int), typeof(CategoryVerticalAxisCoreControl),
            new FrameworkPropertyMetadata(-1,
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnLimitChanged)
            );
        static void OnLimitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => Update(d);

        public CategoryVerticalAxisCoreControl() : base()
        {
            background = new BackgroundManager(Brushes.WhiteSmoke, null);
        }

        static void Update(DependencyObject d)
        {
            var control = d as CategoryVerticalAxisCoreControl;
            if (control != null && control.YPositions != null)
            {
                control.ChartManager = new CategoryVerticalAxisManager(
                    control.YPositions, control.Labels, control.Limit);
                control.ChartDrawingArea = control.ChartManager.ChartArea;
                control.LimitDrawingArea = control.ChartManager.ChartArea;
            }
        }
    }
}
