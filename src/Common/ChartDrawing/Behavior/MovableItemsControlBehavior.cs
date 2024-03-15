using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CompMs.Graphics.Behavior;

/// <summary>
/// Provides behavior for a movable items control.
/// </summary>
public static class MovableItemsControlBehavior
{
    /// <summary>
    /// Gets or sets the callback action for the movable items control.
    /// </summary>
    /// <remarks>
    /// The expected arguments for the callback action are:
    /// - sourceCollection: The source collection of the item being dragged.
    /// - sourceIndex: The index of the item being dragged in the source collection.
    /// - destinationCollection: The destination collection where the item is being dropped.
    /// - destinationIndex: The index in the destination collection where the item is being dropped. 
    /// Importantly, a negative destinationIndex indicates that the item was dropped without a specific position being designated.
    /// This might be used to signal a default insertion position or a special handling scenario.
    /// </remarks>
    public static readonly DependencyProperty CallbackProperty =
        DependencyProperty.RegisterAttached(
            "Callback",
            typeof(Action<object, int, object, int>),
            typeof(MovableItemsControlBehavior),
            new PropertyMetadata(null, OnCallbackPropertyChanged));

    /// <summary>
    /// Gets the callback action for the movable items control.
    /// </summary>
    /// <param name="target">The target dependency object.</param>
    /// <returns>The callback action.</returns>
    public static Action<object, int, object, int> GetCallback(DependencyObject target) {
        return (Action<object, int, object, int>)target.GetValue(CallbackProperty);
    }

    /// <summary>
    /// Sets the callback action for the movable items control.
    /// </summary>
    /// <param name="target">The target dependency object.</param>
    /// <param name="value">The callback action.</param>
    public static void SetCallback(DependencyObject target, Action<object, int, object, int> value) {
        target.SetValue(CallbackProperty, value);
    }

    /// <summary>
    /// Gets or sets the collection for the movable items control.
    /// </summary>
    public static readonly DependencyProperty CollectionProperty =
        DependencyProperty.RegisterAttached(
            "Collection",
            typeof(object),
            typeof(MovableItemsControlBehavior),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection for the movable items control.
    /// </summary>
    /// <param name="target">The target dependency object.</param>
    /// <returns>The collection.</returns>
    public static object GetCollection(DependencyObject target) {
        return target.GetValue(CollectionProperty);
    }

    /// <summary>
    /// Sets the collection for the movable items control.
    /// </summary>
    /// <param name="target">The target dependency object.</param>
    /// <param name="value">The collection.</param>
    public static void SetCollection(DependencyObject target, object value) {
        target.SetValue(CollectionProperty, value);
    }

    private static void OnCallbackPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not ItemsControl itemsControl) {
            return;
        }

        if (GetCallback(itemsControl) is not null) {
            itemsControl.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            itemsControl.PreviewMouseMove += OnPreviewMouseMove;
            itemsControl.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            itemsControl.PreviewDragEnter += OnPreviewDragEnter;
            itemsControl.PreviewDragLeave += OnPreviewDragLeave;
            itemsControl.Drop += OnDrop;
        }
        else {
            itemsControl.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            itemsControl.PreviewMouseMove -= OnPreviewMouseMove;
            itemsControl.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
            itemsControl.PreviewDragEnter -= OnPreviewDragEnter;
            itemsControl.PreviewDragLeave -= OnPreviewDragLeave;
            itemsControl.Drop -= OnDrop;
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
        var itemsControl = sender as ItemsControl;
        var sourceContainer = GetTemplatedRootElement(e.OriginalSource as FrameworkElement);
        var sourceCollection = GetCollection(itemsControl);
        var index = itemsControl.ItemContainerGenerator.IndexFromContainer(sourceContainer);
        if (index < 0 || sourceCollection is null) {
            return;
        }
        _draggingData = new DragDropObject
        {
            Start = e.GetPosition(System.Windows.Window.GetWindow(itemsControl)),
            DraggedItem = sourceContainer,
            SourceCollection = sourceCollection,
            SourceIndex = index,
        };
    }

    private static void OnPreviewMouseMove(object sender, MouseEventArgs e) {
        if (_draggingData is not null) {
            var control = sender as FrameworkElement;
            var current = e.GetPosition(System.Windows.Window.GetWindow(control));
            if (_draggingData.CheckStartDragging(current)) {
                DragDrop.DoDragDrop(control, _draggingData.DraggedItem, DragDropEffects.Move);
                _draggingData = null;
            }
        }
    }

    private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
        _draggingData = null;
    }

    private static void OnPreviewDragEnter(object sender, DragEventArgs e) {
        if (_draggingData is not null) {
            _draggingData.IsDroppable = true;
        }
    }

    private static void OnPreviewDragLeave(object sender, DragEventArgs e) {
        if (_draggingData is not null) {
            _draggingData.IsDroppable = false;
        }
    }

    private static void OnDrop(object sender, DragEventArgs e) {
        if (_draggingData.IsDroppable) {
            var data = _draggingData;
            var itemsControl = sender as ItemsControl;
            var destinationContainer = GetTemplatedRootElement(e.OriginalSource as FrameworkElement);
            var destinationCollection = GetCollection(itemsControl);
            var index = itemsControl.ItemContainerGenerator.IndexFromContainer(destinationContainer);
            if (destinationCollection is not null) {
                GetCallback(itemsControl)?.Invoke(data.SourceCollection, data.SourceIndex, destinationCollection, index);
                e.Handled = true;
            }
        }
    }

    private static FrameworkElement GetTemplatedRootElement(FrameworkElement element) {
        var parent = element;
        while (parent.TemplatedParent is not null) {
            parent = parent.TemplatedParent as FrameworkElement;
        }
        return parent;
    }

    private static DragDropObject? _draggingData;

    internal sealed class DragDropObject
    {
        public Point Start { get; set; }

        public FrameworkElement? DraggedItem { get; set; }

        public int SourceIndex { get; set; }

        public object SourceCollection { get; set; }

        public bool IsDroppable { get; set; }

        public bool CheckStartDragging(Point current) {
            return (current - Start).Length - MinimumDragPoint.Length > 0;
        }

        private static readonly Vector MinimumDragPoint = new(SystemParameters.MinimumHorizontalDragDistance, SystemParameters.MinimumVerticalDragDistance);
    }
}
