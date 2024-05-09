using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CompMs.Graphics.Behavior;

// see http://yujiro15.net/blog/index.php?id=151
public static class ReorderableItemsControlBehavior
{
    public static readonly DependencyProperty CallbackProperty =
        DependencyProperty.RegisterAttached(
            "Callback",
            typeof(Action<int>),
            typeof(ReorderableItemsControlBehavior),
            new PropertyMetadata(null, OnCallbackPropertyChanged));

    public static Action<int> GetCallback(DependencyObject target)
    {
        return (Action<int>)target.GetValue(CallbackProperty);
    }

    public static void SetCallback(DependencyObject target, Action<int> value)
    {
        target.SetValue(CallbackProperty, value);
    }

    private static void OnCallbackPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var itemsControl = d as ItemsControl;
        if (itemsControl == null) return;

        if (GetCallback(itemsControl) != null)
        {
            itemsControl.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            itemsControl.PreviewMouseMove += OnPreviewMouseMove;
            itemsControl.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            itemsControl.PreviewDragEnter += OnPreviewDragEnter;
            itemsControl.PreviewDragLeave += OnPreviewDragLeave;
            itemsControl.PreviewDrop += OnPreviewDrop;
        }
        else
        {
            itemsControl.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            itemsControl.PreviewMouseMove -= OnPreviewMouseMove;
            itemsControl.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
            itemsControl.PreviewDragEnter -= OnPreviewDragEnter;
            itemsControl.PreviewDragLeave -= OnPreviewDragLeave;
            itemsControl.PreviewDrop -= OnPreviewDrop;
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var control = sender as FrameworkElement;
        _draggingData = new DragDropObject
        {
            Start = e.GetPosition(System.Windows.Window.GetWindow(control)),
            DraggedItem = GetTemplatedRootElement(e.OriginalSource as FrameworkElement)
        };
    }

    private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingData is not null)
        {
            var control = sender as FrameworkElement;
            var current = e.GetPosition(System.Windows.Window.GetWindow(control));
            if (_draggingData.CheckStartDragging(current))
            {
                DragDrop.DoDragDrop(control, _draggingData.DraggedItem, DragDropEffects.Move);
                _draggingData = null;
            }
        }
    }

    private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _draggingData = null;
    }

    private static void OnPreviewDragEnter(object sender, DragEventArgs e)
    {
        _draggingData.IsDroppable = true;
    }

    private static void OnPreviewDragLeave(object sender, DragEventArgs e)
    {
        _draggingData.IsDroppable = false;
    }

    private static void OnPreviewDrop(object sender, DragEventArgs e)
    {
        if (_draggingData.IsDroppable)
        {
            var itemsControl = sender as ItemsControl;
            if (itemsControl.ItemContainerGenerator.IndexFromContainer(_draggingData.DraggedItem) >= 0)
            {
                var targetContainer = GetTemplatedRootElement(e.OriginalSource as FrameworkElement);
                var index = itemsControl.ItemContainerGenerator.IndexFromContainer(targetContainer);
                if (index >= 0)
                {
                    GetCallback(itemsControl)?.Invoke(index);
                }
            }
        }
    }

    private static FrameworkElement GetTemplatedRootElement(FrameworkElement element)
    {
        var parent = element.TemplatedParent as FrameworkElement;
        while (parent.TemplatedParent != null)
        {
            parent = parent.TemplatedParent as FrameworkElement;
        }
        return parent;
    }

    private static DragDropObject? _draggingData;
     
    internal class DragDropObject
    {
        public Point Start { get; set; }
     
        public FrameworkElement? DraggedItem { get; set; }
     
        public bool IsDroppable { get; set; }
     
        public bool CheckStartDragging(Point current)
        {
            return (current - Start).Length - MinimumDragPoint.Length > 0;
        }
     
        private static readonly Vector MinimumDragPoint = new(SystemParameters.MinimumHorizontalDragDistance, SystemParameters.MinimumVerticalDragDistance);
    }
}
