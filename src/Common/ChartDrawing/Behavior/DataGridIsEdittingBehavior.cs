using CompMs.Graphics.Base;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CompMs.Graphics.Behavior
{
    public sealed class DataGridIsEdittingBehavior
    {
        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached(
                "Enabled",
                typeof(bool),
                typeof(DataGridIsEdittingBehavior),
                new PropertyMetadata(
                    BooleanBoxes.FalseBox,
                    OnEnabledChanged));

        public static bool GetEnabled(DependencyObject d) {
            return (bool)d.GetValue(EnabledProperty);
        }

        public static void SetEnabled(DependencyObject d, bool value) {
            d.SetValue(EnabledProperty, BooleanBoxes.Box(value));
        }

        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            // DO NOT USE CellEditEnding
            // If cell editting is cancelled, CellEditEnding event is fired. However, RowEditting is not cancelled. ICollectionView is still not editable.
            if (d is DataGrid grid) {
                if ((bool)e.OldValue) {
                    grid.BeginningEdit -= OnBeginningEdit;
                    grid.RowEditEnding -= OnRowEditEnding;
                }
                if ((bool)e.NewValue) {
                    grid.BeginningEdit += OnBeginningEdit;
                    grid.RowEditEnding += OnRowEditEnding;
                }
            }
        }

        private static void OnBeginningEdit(object sender, DataGridBeginningEditEventArgs e) {
            if (sender is DataGrid grid) {
                System.Diagnostics.Debug.WriteLine("Edit beginning");
                SetIsEditting(grid, true);
            }
        }

        private static void OnRowEditEnding(object sender, DataGridRowEditEndingEventArgs e) {
            if (sender is DataGrid grid) {
                System.Diagnostics.Debug.WriteLine("Row edit ending");
                SetIsEditting(grid, false);
            }
        }

        public static readonly DependencyProperty IsEdittingProperty =
            DependencyProperty.RegisterAttached(
                "IsEditting",
                typeof(bool),
                typeof(DataGridIsEdittingBehavior),
                new PropertyMetadata(BooleanBoxes.FalseBox));

        public static bool GetIsEditting(DependencyObject d) {
            return (bool)d.GetValue(IsEdittingProperty);
        }

        public static void SetIsEditting(DependencyObject d, bool value) {
            d.SetValue(IsEdittingProperty, BooleanBoxes.Box(value));
        }
    }
}
