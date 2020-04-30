using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using CompMs.Common.DataStructure;

namespace CompMs.Graphics.Dendrogram
{
    public class DigraphView : TreeView
    {
        public Digraph GraphSource
        {
            get => (Digraph)GetValue(GraphSourceProperty);
            set => SetValue(GraphSourceProperty, value);
        }
        private static readonly DependencyProperty GraphSourceProperty = DependencyProperty.Register(
            nameof(GraphSource), typeof(Digraph), typeof(DigraphView),
            new FrameworkPropertyMetadata(null, OnGraphSourceChanged)); 
        private static void OnGraphSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as DigraphView;
            if (control != null)
            {
                var tree = e.NewValue as DirectedTree;
                if (tree != null && control.Root == -1)
                    control.Root = tree.Root;
                if (control.Root != -1)
                    control.ItemsSource = new List<DigraphItem> { DigraphItem.Get(control, control.Root) };
            }
        }

        public int Root
        {
            get => (int)GetValue(RootProperty);
            set => SetValue(RootProperty, value);
        }
        private static readonly DependencyProperty RootProperty = DependencyProperty.Register(
            nameof(Root), typeof(int), typeof(DigraphView), new FrameworkPropertyMetadata(-1)); 

        public IReadOnlyList<string> Labels
        {
            get => (IReadOnlyList<string>)GetValue(LabelsProperty);
            set => SetValue(LabelsProperty, value);
        }
        private static readonly DependencyProperty LabelsProperty = DependencyProperty.Register(
            nameof(Labels), typeof(IReadOnlyList<string>), typeof(DigraphView), new FrameworkPropertyMetadata(null));

        /*
        public int SelectedNode
        {
            get => (int)GetValue(SelectedNodeProperty);
            set => SetValue(SelectedNodeProperty, value);
        }
        private static readonly DependencyProperty SelectedNodeProperty = DependencyProperty.Register(
            nameof(SelectedNode), typeof(int), typeof(DigraphView), new FrameworkPropertyMetadata(-1));
        static void OnSelectedNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as DigraphView;
            if (control != null)
            {
                var node = (int)e.NewValue;
                control.SelectedItem = DigraphItem.Get(control, node);
            }
        }

        protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            base.OnSelectedItemChanged(e);
            var item = e.NewValue as DigraphItem;
            if (item != null)
                SelectedNode = item.Node;
        }
        */
    }

    class DigraphItem
    {
        public string Name
        {
            get
            {
                if (control.Labels == null || control.Labels.Count < node || control.Labels[node] == null)
                    return "";
                return control.Labels[node];
            }
        }

        public IReadOnlyCollection<DigraphItem> Children
        {
            get
            {
                if (children == null)
                    children = control.GraphSource[node].Select(e => Get(control, e.To)).ToList();
                return children;
            }
        }

        public int Node => node;

        private DigraphItem(DigraphView control_, int node_)
        {
            node = node_;
            control = control_;
        }

        public static DigraphItem Get(DigraphView control, int node)
        {
            if (!memo.ContainsKey((control, node)))
                memo[(control, node)] = new DigraphItem(control, node);
            return memo[(control, node)];
        }

        private static Dictionary<(DigraphView, int), DigraphItem> memo = new Dictionary<(DigraphView, int), DigraphItem>();
        private int node;
        private List<DigraphItem> children;
        private DigraphView control;
    }
}
