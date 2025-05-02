using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CompMs.Graphics.Behavior;

public static class ItemSelectBehavior
{
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.RegisterAttached(
            "SelectedItem",
            typeof(object),
            typeof(ItemSelectBehavior),
            new FrameworkPropertyMetadata(
                _defaultSelectedItem,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedItemChanged)
        );

    private static readonly object _defaultSelectedItem = new();

    public static object GetSelectedItem(DependencyObject d) => d.GetValue(SelectedItemProperty);

    public static void SetSelectedItem(DependencyObject d, object value) => d.SetValue(SelectedItemProperty, value);

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        SetParentDependencyObject(d, d);
    }

    private static readonly DependencyProperty ParentDependencyObjectProperty =
        DependencyProperty.RegisterAttached(
            "ParentDependencyObject",
            typeof(DependencyObject),
            typeof(ItemSelectBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits)
        );

    private static DependencyObject GetParentDependencyObject(DependencyObject d) => (DependencyObject)d.GetValue(ParentDependencyObjectProperty);
    private static void SetParentDependencyObject(DependencyObject d, DependencyObject value) => d.SetValue(ParentDependencyObjectProperty, value);

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
            btn.Click -= OnClick;
            btn.Click += OnClick;
        }
        if (d is MenuItem menu) {
            menu.Click -= OnClick;
            menu.Click += OnClick;
        }
    }

    private static void OnClick(object sender, RoutedEventArgs e) {
        if (sender is DependencyObject d) {
            var parent = GetParentDependencyObject(d);
            parent.SetCurrentValue(SelectedItemProperty, GetItem(d));
        }
    }
}
