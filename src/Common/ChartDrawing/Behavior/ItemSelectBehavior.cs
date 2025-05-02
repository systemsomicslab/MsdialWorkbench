using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CompMs.Graphics.Behavior;

public static class ItemSelectBehavior
{
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.RegisterAttached(
            "SelectedItem",
            typeof(object),
            typeof(ItemSelectBehavior),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

    public static object GetSelectedItem(DependencyObject d) => d.GetValue(SelectedItemProperty);

    public static void SetSelectedItem(DependencyObject d, object value) => d.SetValue(SelectedItemProperty, value);

    public static readonly DependencyProperty ItemProperty =
        DependencyProperty.RegisterAttached(
            "Item",
            typeof(object),
            typeof(ItemSelectBehavior),
            new PropertyMetadata(null, OnItemChanged)
        );

    public static object GetItem(DependencyObject d) => d.GetValue(ItemProperty);

    public static void SetItem(DependencyObject d, object value) => d.SetValue(ItemProperty, value);

    private static void OnItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is ButtonBase btn) {
            WeakEventManager<ButtonBase, RoutedEventArgs>.RemoveHandler(btn, nameof(ButtonBase.Click), OnClick);
            WeakEventManager<ButtonBase, RoutedEventArgs>.AddHandler(btn, nameof(ButtonBase.Click), OnClick);
        }
        if (d is MenuItem menu) {
            WeakEventManager<MenuItem, RoutedEventArgs>.RemoveHandler(menu, nameof(MenuItem.Click), OnClick);
            WeakEventManager<MenuItem, RoutedEventArgs>.AddHandler(menu, nameof(MenuItem.Click), OnClick);
        }
    }

    private static void OnClick(object sender, RoutedEventArgs e) {
        if (sender is DependencyObject d) {
            var parent = FindAncestorWithProperty(d, SelectedItemProperty);
            if (parent is not null) {
                var value = GetItem(d);
                parent.SetCurrentValue(SelectedItemProperty, value);
            }
        }
    }

    private static DependencyObject? FindAncestorWithProperty(DependencyObject child, DependencyProperty property) {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent is not null) {
            if (parent.ReadLocalValue(property) != DependencyProperty.UnsetValue) {
                return parent;
            }
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }
}
