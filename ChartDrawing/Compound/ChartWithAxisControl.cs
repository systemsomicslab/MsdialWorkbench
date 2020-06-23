using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Compound
{
    /// <summary>
    /// このカスタム コントロールを XAML ファイルで使用するには、手順 1a または 1b の後、手順 2 に従います。
    ///
    /// 手順 1a) 現在のプロジェクトに存在する XAML ファイルでこのカスタム コントロールを使用する場合
    /// この XmlNamespace 属性を使用場所であるマークアップ ファイルのルート要素に
    /// 追加します:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CompMs.Graphics.Compound"
    ///
    ///
    /// 手順 1b) 異なるプロジェクトに存在する XAML ファイルでこのカスタム コントロールを使用する場合
    /// この XmlNamespace 属性を使用場所であるマークアップ ファイルのルート要素に
    /// 追加します:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CompMs.Graphics.Compound;assembly=CompMs.Graphics.Compound"
    ///
    /// また、XAML ファイルのあるプロジェクトからこのプロジェクトへのプロジェクト参照を追加し、
    /// リビルドして、コンパイル エラーを防ぐ必要があります:
    ///
    ///     ソリューション エクスプローラーで対象のプロジェクトを右クリックし、
    ///     [参照の追加] の [プロジェクト] を選択してから、このプロジェクトを参照し、選択します。
    ///
    ///
    /// 手順 2)
    /// コントロールを XAML ファイルで使用します。
    ///
    ///     <MyNamespace:ChartWithAxisControl/>
    ///
    /// </summary>
    public class ChartWithAxisControl : Control
    {
        public Drawing Chart
        {
            get => (Drawing)GetValue(ChartProperty);
            set => SetValue(ChartProperty, value);
        }
        public static readonly DependencyProperty ChartProperty = DependencyProperty.Register(
            nameof(Chart), typeof(Drawing), typeof(ChartWithAxisControl),
            new FrameworkPropertyMetadata(default,
                FrameworkPropertyMetadataOptions.AffectsRender)
            );

        public Drawing HorizontalAxis
        {
            get => (Drawing)GetValue(HorizontalAxisProperty);
            set => SetValue(HorizontalAxisProperty, value);
        }
        public static readonly DependencyProperty HorizontalAxisProperty = DependencyProperty.Register(
            nameof(HorizontalAxis), typeof(Drawing), typeof(ChartWithAxisControl),
            new FrameworkPropertyMetadata(default(Drawing),
                FrameworkPropertyMetadataOptions.AffectsRender)
            );

        public Drawing VerticalAxis
        {
            get => (Drawing)GetValue(VerticalAxisProperty);
            set => SetValue(VerticalAxisProperty, value);
        }
        public static readonly DependencyProperty VerticalAxisProperty = DependencyProperty.Register(
            nameof(VerticalAxis), typeof(Drawing), typeof(ChartWithAxisControl),
            new FrameworkPropertyMetadata(default(Drawing),
                FrameworkPropertyMetadataOptions.AffectsRender)
            );

        public IDrawingChart DrawingChart
        {
            get => (IDrawingChart)GetValue(DrawingChartProperty);
            set => SetValue(DrawingChartProperty, value);
        }
        public static readonly DependencyProperty DrawingChartProperty = DependencyProperty.Register(
            nameof(DrawingChart), typeof(IDrawingChart), typeof(ChartWithAxisControl),
            new PropertyMetadata(default, OnDrawingChartPropertyChanged)
            );

        public IDrawingChart DrawingHorizontalAxis
        {
            get => (IDrawingChart)GetValue(DrawingHorizontalAxisProperty);
            set => SetValue(DrawingHorizontalAxisProperty, value);
        }
        public static readonly DependencyProperty DrawingHorizontalAxisProperty = DependencyProperty.Register(
            nameof(DrawingHorizontalAxis), typeof(IDrawingChart), typeof(ChartWithAxisControl),
            new PropertyMetadata(default(IDrawingChart), OnDrawingHorizontalAxisPropertyChanged)
            );

        public IDrawingChart DrawingVerticalAxis
        {
            get => (IDrawingChart)GetValue(DrawingVerticalAxisProperty);
            set => SetValue(DrawingVerticalAxisProperty, value);
        }
        public static readonly DependencyProperty DrawingVerticalAxisProperty = DependencyProperty.Register(
            nameof(DrawingVerticalAxis), typeof(IDrawingChart), typeof(ChartWithAxisControl),
            new PropertyMetadata(default(IDrawingChart), OnDrawingVerticalAxisPropertyChanged)
            );

        public string XLabel { get; set; } = "X Label";
        public string YLabel { get; set; } = "Y Label";

        FrameworkElement chart;
        FrameworkElement horizontalAxis;
        FrameworkElement verticalAxis;

        static ChartWithAxisControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ChartWithAxisControl), new FrameworkPropertyMetadata(typeof(ChartWithAxisControl)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (chart != null)
                chart.SizeChanged -= OnChartSizeChanged;
            chart = GetTemplateChild("PART_Chart") as FrameworkElement;
            if (chart != null)
            {
                chart.SizeChanged += OnChartSizeChanged;
                if (DrawingChart != null)
                    DrawingChart.RenderSize = chart.RenderSize;
            }

            if (horizontalAxis != null)
                horizontalAxis.SizeChanged -= OnHorizontalAxisSizeChanged;
            horizontalAxis = GetTemplateChild("PART_HorizontalAxis") as FrameworkElement;
            if (horizontalAxis != null)
            {
                horizontalAxis.SizeChanged += OnHorizontalAxisSizeChanged;
                if (DrawingHorizontalAxis != null)
                    DrawingHorizontalAxis.RenderSize = horizontalAxis.RenderSize;
            }


            if (verticalAxis != null)
                verticalAxis.SizeChanged -= OnVerticalAxisSizeChanged;
            verticalAxis = GetTemplateChild("PART_VerticalAxis") as FrameworkElement;
            if (verticalAxis != null)
            {
                verticalAxis.SizeChanged += OnChartSizeChanged;
                if (DrawingVerticalAxis != null)
                    DrawingVerticalAxis.RenderSize = verticalAxis.RenderSize;
            }
        }

        void OnChartSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DrawingChart != null)
                DrawingChart.RenderSize = e.NewSize;
        }

        void OnHorizontalAxisSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DrawingHorizontalAxis != null)
                DrawingHorizontalAxis.RenderSize = e.NewSize;
        }

        void OnVerticalAxisSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DrawingVerticalAxis != null)
                DrawingVerticalAxis.RenderSize = e.NewSize;
        }

        void OnDrawingChartPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var drawingChart = sender as IDrawingChart;
            if (drawingChart == null) return;
            switch (e.PropertyName)
            {
                case "ChartArea":
                    if (DrawingHorizontalAxis != null)
                    {
                        var area = DrawingHorizontalAxis.ChartArea;
                        area.X = drawingChart.ChartArea.X;
                        area.Width = drawingChart.ChartArea.Width;
                        DrawingHorizontalAxis.ChartArea = area;
                    }
                    if (DrawingVerticalAxis != null)
                    {
                        var area = DrawingVerticalAxis.ChartArea;
                        area.Y = drawingChart.ChartArea.Y;
                        area.Height = drawingChart.ChartArea.Height;
                        DrawingVerticalAxis.ChartArea = area;
                    }
                    break;
                case "InitialArea":
                    if (DrawingHorizontalAxis != null)
                    {
                        var area = DrawingHorizontalAxis.InitialArea;
                        area.X = drawingChart.InitialArea.X;
                        area.Width = drawingChart.InitialArea.Width;
                        DrawingHorizontalAxis.InitialArea = area;
                    }
                    if (DrawingVerticalAxis != null)
                    {
                        var area = DrawingVerticalAxis.InitialArea;
                        area.Y = drawingChart.InitialArea.Y;
                        area.Height = drawingChart.InitialArea.Height;
                        DrawingVerticalAxis.InitialArea = area;
                    }
                    break;
            }
            Chart = drawingChart.CreateChart();
        }

        void OnDrawingHorizontalAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var drawingChart = sender as IDrawingChart;
            if (drawingChart == null) return;
            switch (e.PropertyName)
            {
                case "ChartArea":
                    if (DrawingChart != null)
                    {
                        var area = DrawingChart.ChartArea;
                        area.X = drawingChart.ChartArea.X;
                        area.Width = drawingChart.ChartArea.Width;
                        DrawingChart.ChartArea = area;
                    }
                    break;
            }
            HorizontalAxis = drawingChart.CreateChart();
        }

        void OnDrawingVerticalAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var drawingChart = sender as IDrawingChart;
            if (drawingChart == null) return;
            switch (e.PropertyName)
            {
                case "ChartArea":
                    if (DrawingChart != null)
                    {
                        var area = DrawingChart.ChartArea;
                        area.Y = drawingChart.ChartArea.Y;
                        area.Height = drawingChart.ChartArea.Height;
                        DrawingChart.ChartArea = area;
                    }
                    break;
            }
            VerticalAxis = drawingChart.CreateChart();
        }

        static void OnDrawingChartPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ChartWithAxisControl;
            if (control == null) return;
            {
                if (e.OldValue is DrawingChartBase drawingChart)
                    drawingChart.PropertyChanged -= control.OnDrawingChartPropertyChanged;
            }
            {
                if (e.NewValue is DrawingChartBase drawingChart)
                {
                    if (control.chart != null)
                        drawingChart.RenderSize = control.chart.RenderSize;
                    drawingChart.PropertyChanged += control.OnDrawingChartPropertyChanged;

                    if (control.DrawingHorizontalAxis != null)
                    {
                        var area = control.DrawingHorizontalAxis.ChartArea;
                        area.X = drawingChart.ChartArea.X;
                        area.Width = drawingChart.ChartArea.Width;
                        control.DrawingHorizontalAxis.ChartArea = area;
                        area = control.DrawingHorizontalAxis.InitialArea;
                        area.X = drawingChart.InitialArea.X;
                        area.Width = drawingChart.InitialArea.Width;
                        control.DrawingHorizontalAxis.InitialArea = area;
                    }

                    if (control.DrawingVerticalAxis != null)
                    {
                        var area = control.DrawingVerticalAxis.ChartArea;
                        area.Y = drawingChart.ChartArea.Y;
                        area.Height = drawingChart.ChartArea.Height;
                        control.DrawingVerticalAxis.ChartArea = area;
                        area = control.DrawingVerticalAxis.InitialArea;
                        area.Y = drawingChart.InitialArea.Y;
                        area.Height = drawingChart.InitialArea.Height;
                        control.DrawingVerticalAxis.InitialArea = area;
                    }

                    control.Chart = drawingChart.CreateChart();
                }
            }
        }

        static void OnDrawingHorizontalAxisPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ChartWithAxisControl;
            if (control == null) return;
            {
                if (e.OldValue is DrawingChartBase drawingChart)
                    drawingChart.PropertyChanged -= control.OnDrawingHorizontalAxisPropertyChanged;
            }
            {
                if (e.NewValue is DrawingChartBase drawingChart)
                {
                    if (control.horizontalAxis != null)
                        drawingChart.RenderSize = control.horizontalAxis.RenderSize;
                    drawingChart.PropertyChanged += control.OnDrawingHorizontalAxisPropertyChanged;

                    if (control.DrawingChart != null)
                    {
                        var area = drawingChart.ChartArea;
                        area.X = control.DrawingChart.ChartArea.X;
                        area.Width = control.DrawingChart.ChartArea.Width;
                        drawingChart.ChartArea = area;
                    }

                    control.HorizontalAxis = drawingChart.CreateChart();
                }
            }
        }

        static void OnDrawingVerticalAxisPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ChartWithAxisControl;
            if (control == null) return;
            {
                if (e.OldValue is DrawingChartBase drawingChart)
                    drawingChart.PropertyChanged -= control.OnDrawingVerticalAxisPropertyChanged;
            }
            {
                if (e.NewValue is DrawingChartBase drawingChart)
                {
                    if (control.verticalAxis != null)
                        drawingChart.RenderSize = control.verticalAxis.RenderSize;
                    drawingChart.PropertyChanged += control.OnDrawingVerticalAxisPropertyChanged;

                    if (control.DrawingChart != null)
                    {
                        var area = drawingChart.ChartArea;
                        area.Y = control.DrawingChart.ChartArea.Y;
                        area.Height = control.DrawingChart.ChartArea.Height;
                        drawingChart.ChartArea = area;
                    }

                    control.VerticalAxis = drawingChart.CreateChart();
                }
            }
        }
    }
}
