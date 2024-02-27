using CompMs.Graphics.Base;
using System.Windows;

namespace CompMs.Graphics.Chart
{
    public sealed class RenderAreaControlState : DependencyObject
    {
        public static DependencyProperty ZoomByDragIsEnabledProperty =
            DependencyProperty.Register(
                nameof(ZoomByDragIsEnabled),
                typeof(bool),
                typeof(RenderAreaControlState),
                new PropertyMetadata(BooleanBoxes.TrueBox));

        public bool ZoomByDragIsEnabled {
            get => (bool)GetValue(ZoomByDragIsEnabledProperty);
            set => SetValue(ZoomByDragIsEnabledProperty, BooleanBoxes.Box(value));
        }

        public static DependencyProperty ZoomByWheelIsEnabledProperty =
            DependencyProperty.Register(
                nameof(ZoomByWheelIsEnabled),
                typeof(bool),
                typeof(RenderAreaControlState),
                new PropertyMetadata(BooleanBoxes.TrueBox));

        public bool ZoomByWheelIsEnabled {
            get => (bool)GetValue(ZoomByWheelIsEnabledProperty);
            set => SetValue(ZoomByWheelIsEnabledProperty, BooleanBoxes.Box(value));
        }

        public static DependencyProperty MoveByDragIsEnabledProperty =
            DependencyProperty.Register(
                nameof(MoveByDragIsEnabled),
                typeof(bool),
                typeof(RenderAreaControlState),
                new PropertyMetadata(BooleanBoxes.TrueBox));

        public bool MoveByDragIsEnabled {
            get => (bool)GetValue(MoveByDragIsEnabledProperty);
            set => SetValue(MoveByDragIsEnabledProperty, BooleanBoxes.Box(value));
        }

        public static DependencyProperty ResetRangeByDoubleClickIsEnabledProperty =
            DependencyProperty.Register(
                nameof(ResetRangeByDoubleClickIsEnabled),
                typeof(bool),
                typeof(RenderAreaControlState),
                new PropertyMetadata(BooleanBoxes.TrueBox));

        public bool ResetRangeByDoubleClickIsEnabled {
            get => (bool)GetValue(ResetRangeByDoubleClickIsEnabledProperty);
            set => SetValue(ResetRangeByDoubleClickIsEnabledProperty, BooleanBoxes.Box(value));
        }

        public static DependencyProperty AxisRangeRecalculationIsEnabledProperty =
            DependencyProperty.Register(
                nameof(AxisRangeRecalculationIsEnabled),
                typeof(bool),
                typeof(RenderAreaControlState),
                new PropertyMetadata(BooleanBoxes.TrueBox));

        public bool AxisRangeRecalculationIsEnabled {
            get => (bool)GetValue(AxisRangeRecalculationIsEnabledProperty);
            set => SetValue(AxisRangeRecalculationIsEnabledProperty, BooleanBoxes.Box(value));
        }
    }
}
