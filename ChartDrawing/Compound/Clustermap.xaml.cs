using System;
using System.Collections.Generic;
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
        private static readonly DependencyProperty DataMatrixProperty = DependencyProperty.Register(
            nameof(DataMatrix), typeof(double[,]), typeof(Clustermap), new FrameworkPropertyMetadata()
            );

        public DirectedTree XDendrogram
        {
            get => (DirectedTree)GetValue(XDendrogramProperty);
            set => SetValue(XDendrogramProperty, value);
        }
        private static readonly DependencyProperty XDendrogramProperty = DependencyProperty.Register(
            nameof(XDendrogram), typeof(DirectedTree), typeof(Clustermap), new FrameworkPropertyMetadata()
            );

        public DirectedTree YDendrogram
        {
            get => (DirectedTree)GetValue(YDendrogramProperty);
            set => SetValue(YDendrogramProperty, value);
        }
        private static readonly DependencyProperty YDendrogramProperty = DependencyProperty.Register(
            nameof(YDendrogram), typeof(DirectedTree), typeof(Clustermap), new FrameworkPropertyMetadata()
            );

        public IReadOnlyList<string> XLabels
        {
            get => (IReadOnlyList<string>)GetValue(XLabelsProperty);
            set => SetValue(XLabelsProperty, value);
        }
        private static readonly DependencyProperty XLabelsProperty = DependencyProperty.Register(
            nameof(XLabels), typeof(IReadOnlyList<string>), typeof(Clustermap), new FrameworkPropertyMetadata()
            );

        public IReadOnlyList<string> YLabels
        {
            get => (IReadOnlyList<string>)GetValue(YLabelsProperty);
            set => SetValue(YLabelsProperty, value);
        }
        private static readonly DependencyProperty YLabelsProperty = DependencyProperty.Register(
            nameof(YLabels), typeof(IReadOnlyList<string>), typeof(Clustermap), new FrameworkPropertyMetadata()
            );
    }
}
