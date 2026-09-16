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
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedItemChanged)
        );

    public static object GetSelectedItem(DependencyObject d) => d.GetValue(SelectedItemProperty);

    public static void SetSelectedItem(DependencyObject d, object value) => d.SetValue(SelectedItemProperty, value);

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is UIElement ui) {
            ui.RemoveHandler(ButtonBase.ClickEvent, OnClickHandler);
            ui.AddHandler(ButtonBase.ClickEvent, OnClickHandler);
        }
        if (d is ContextMenu cm) {
            cm.RemoveHandler(MenuItem.ClickEvent, OnClickHandler);
            cm.AddHandler(MenuItem.ClickEvent, OnClickHandler);
        }
        if (d is MenuItem mi) {
            mi.RemoveHandler(MenuItem.ClickEvent, OnClickHandler);
            mi.AddHandler(MenuItem.ClickEvent, OnClickHandler);
        }
    }

    private static readonly RoutedEventHandler OnClickHandler = OnClick;

    private static void OnClick(object sender, RoutedEventArgs e) {
        if (e.OriginalSource is DependencyObject src && sender is DependencyObject d) {
            var value = GetItem(src);
            d.SetCurrentValue(SelectedItemProperty, value);
        }
    }

    public static readonly DependencyProperty ItemProperty =
        DependencyProperty.RegisterAttached(
            "Item",
            typeof(object),
            typeof(ItemSelectBehavior),
            new PropertyMetadata(null)
        );

    public static object GetItem(DependencyObject d) => d.GetValue(ItemProperty);

    public static void SetItem(DependencyObject d, object value) => d.SetValue(ItemProperty, value);
}
