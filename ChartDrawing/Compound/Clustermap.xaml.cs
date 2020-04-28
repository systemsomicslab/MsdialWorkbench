using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Common.DataStructure;
using CompMs.Graphics.Core.Command;

namespace CompMs.Graphics.Compound
{
    /// <summary>
    /// Clustermap.xaml の相互作用ロジック
    /// </summary>
    public partial class Clustermap : UserControl
    {
        public Clustermap()
        {
            InitializeComponent();
            SavePngCmd = new SavePngCommand() { Element = this };
            // DataContext = this;
            ContextMenu.DataContext = this;
            sw = new Stopwatch();
            MouseRightButtonDown += HandleContextMenuOnMouseRightButtonDown;
            MouseLeave += HandleContextMenuOnMouseLeave;
            ContextMenuOpening += HandleContextMenuOnContextMenuOpning;
        }

        public ICommand SavePngCmd { get; set; }
        /*
        public ICommand SavePngCmd
        {
            get => (ICommand) GetValue(SavePngCmdProperty);
            set => SetValue(SavePngCmdProperty, value);
        }
        public static readonly DependencyProperty SavePngCmdProperty = DependencyProperty.Register(
            nameof(SavePngCmd), typeof(ICommand), typeof(Clustermap),
            new FrameworkPropertyMetadata(default(ICommand),
                FrameworkPropertyMetadataOptions.AffectsRender)
            );
            */

        public double[,] DataMatrix
        {
            get => (double[,])GetValue(DataMatrixProperty);
            set => SetValue(DataMatrixProperty, value);
        }
        public static readonly DependencyProperty DataMatrixProperty = DependencyProperty.Register(
            nameof(DataMatrix), typeof(double[,]), typeof(Clustermap), new FrameworkPropertyMetadata()
            );

        public DirectedTree XDendrogram
        {
            get => (DirectedTree)GetValue(XDendrogramProperty);
            set => SetValue(XDendrogramProperty, value);
        }
        public static readonly DependencyProperty XDendrogramProperty = DependencyProperty.Register(
            nameof(XDendrogram), typeof(DirectedTree), typeof(Clustermap), new FrameworkPropertyMetadata()
            );

        public DirectedTree YDendrogram
        {
            get => (DirectedTree)GetValue(YDendrogramProperty);
            set => SetValue(YDendrogramProperty, value);
        }
        public static readonly DependencyProperty YDendrogramProperty = DependencyProperty.Register(
            nameof(YDendrogram), typeof(DirectedTree), typeof(Clustermap), new FrameworkPropertyMetadata()
            );

        public IReadOnlyList<double> XPositions
        {
            get => (IReadOnlyList<double>)GetValue(XPositionsProperty);
            set => SetValue(XPositionsProperty, value);
        }
        public static readonly DependencyProperty XPositionsProperty = DependencyProperty.Register(
            nameof(XPositions), typeof(IReadOnlyList<double>), typeof(Clustermap), new FrameworkPropertyMetadata()
            );

        public IReadOnlyList<double> YPositions
        {
            get => (IReadOnlyList<double>)GetValue(YPositionsProperty);
            set => SetValue(YPositionsProperty, value);
        }
        public static readonly DependencyProperty YPositionsProperty = DependencyProperty.Register(
            nameof(YPositions), typeof(IReadOnlyList<double>), typeof(Clustermap), new FrameworkPropertyMetadata()
            );

        public IReadOnlyList<string> XLabels
        {
            get => (IReadOnlyList<string>)GetValue(XLabelsProperty);
            set => SetValue(XLabelsProperty, value);
        }
        public static readonly DependencyProperty XLabelsProperty = DependencyProperty.Register(
            nameof(XLabels), typeof(IReadOnlyList<string>), typeof(Clustermap), new FrameworkPropertyMetadata()
            );

        public IReadOnlyList<string> YLabels
        {
            get => (IReadOnlyList<string>)GetValue(YLabelsProperty);
            set => SetValue(YLabelsProperty, value);
        }
        public static readonly DependencyProperty YLabelsProperty = DependencyProperty.Register(
            nameof(YLabels), typeof(IReadOnlyList<string>), typeof(Clustermap), new FrameworkPropertyMetadata()
            );

        [Bindable(true), Browsable(true)]
        public double MinX
        {
            get => (double)GetValue(MinXProperty);
            set => SetValue(MinXProperty, value);
        }
        public static readonly DependencyProperty MinXProperty = DependencyProperty.Register(
            nameof(MinX), typeof(double), typeof(Clustermap), new FrameworkPropertyMetadata(0d)
            );

        public double MaxX
        {
            get => (double)GetValue(MaxXProperty);
            set => SetValue(MaxXProperty, value);
        }
        public static readonly DependencyProperty MaxXProperty = DependencyProperty.Register(
            nameof(MaxX), typeof(double), typeof(Clustermap), new FrameworkPropertyMetadata(100d)
            );

        public double MinY
        {
            get => (double)GetValue(MinYProperty);
            set => SetValue(MinYProperty, value);
        }
        public static readonly DependencyProperty MinYProperty = DependencyProperty.Register(
            nameof(MinY), typeof(double), typeof(Clustermap), new FrameworkPropertyMetadata(0d)
            );

        public double MaxY
        {
            get => (double)GetValue(MaxYProperty);
            set => SetValue(MaxYProperty, value);
        }
        public static readonly DependencyProperty MaxYProperty = DependencyProperty.Register(
            nameof(MaxY), typeof(double), typeof(Clustermap), new FrameworkPropertyMetadata(100d)
            );

        public double LimitMinX
        {
            get => (double)GetValue(LimitMinXProperty);
            set => SetValue(LimitMinXProperty, value);
        }
        public static readonly DependencyProperty LimitMinXProperty = DependencyProperty.Register(
            nameof(LimitMinX), typeof(double), typeof(Clustermap), new FrameworkPropertyMetadata(0d)
            );

        public double LimitMaxX
        {
            get => (double)GetValue(LimitMaxXProperty);
            set => SetValue(LimitMaxXProperty, value);
        }
        public static readonly DependencyProperty LimitMaxXProperty = DependencyProperty.Register(
            nameof(LimitMaxX), typeof(double), typeof(Clustermap), new FrameworkPropertyMetadata(100d)
            );

        public double LimitMinY
        {
            get => (double)GetValue(LimitMinYProperty);
            set => SetValue(LimitMinYProperty, value);
        }
        public static readonly DependencyProperty LimitMinYProperty = DependencyProperty.Register(
            nameof(LimitMinY), typeof(double), typeof(Clustermap), new FrameworkPropertyMetadata(0d)
            );

        public double LimitMaxY
        {
            get => (double)GetValue(LimitMaxYProperty);
            set => SetValue(LimitMaxYProperty, value);
        }
        public static readonly DependencyProperty LimitMaxYProperty = DependencyProperty.Register(
            nameof(LimitMaxY), typeof(double), typeof(Clustermap), new FrameworkPropertyMetadata(100d)
            );

        public int XLabelLimit
        {
            get => (int)GetValue(XLabelLimitProperty);
            set => SetValue(XLabelLimitProperty, value);
        }
        public static readonly DependencyProperty XLabelLimitProperty = DependencyProperty.Register(
            nameof(XLabelLimit), typeof(int), typeof(Clustermap), new FrameworkPropertyMetadata(20)
            );

        public int YLabelLimit
        {
            get => (int)GetValue(YLabelLimitProperty);
            set => SetValue(YLabelLimitProperty, value);
        }
        public static readonly DependencyProperty YLabelLimitProperty = DependencyProperty.Register(
            nameof(YLabelLimit), typeof(int), typeof(Clustermap), new FrameworkPropertyMetadata(20)
            );

        Stopwatch sw;
        void HandleContextMenuOnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            sw.Start();
        }
        void HandleContextMenuOnContextMenuOpning(object sender, ContextMenuEventArgs e)
        {
            sw.Stop();
            if (sw.ElapsedMilliseconds > 200)
                e.Handled = true;
            sw.Reset();
        }
        void HandleContextMenuOnMouseLeave(object sender, MouseEventArgs e)
        {
            sw.Stop();
            sw.Reset();
        }
    }
}
