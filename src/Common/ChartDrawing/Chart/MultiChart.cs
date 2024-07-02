using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace CompMs.Graphics.Chart
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CompMs.Graphics.Chart"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CompMs.Graphics.Chart;assembly=CompMs.Graphics.Chart"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:MultiChart/>
    ///
    /// </summary>
    [TemplatePart(Name = "PART_HorizontalAxis", Type = typeof(ChartBaseControl))]
    [TemplatePart(Name = "PART_VerticalAxis", Type = typeof(ChartBaseControl))]
    public class MultiChart : ItemsControl
    {
        static MultiChart() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiChart), new FrameworkPropertyMetadata(typeof(MultiChart)));
        }

        public MultiChart()
        {
            SetValue(RenderAreaControlStateProperty, new RenderAreaControlState());
        }

        public static readonly DependencyProperty HorizontalAxisProperty =
            ChartBaseControl.HorizontalAxisProperty.AddOwner(
                typeof(MultiChart),
                new FrameworkPropertyMetadata(
                    ChartBaseControl.HorizontalAxisProperty.DefaultMetadata.DefaultValue,
                    FrameworkPropertyMetadataOptions.Inherits));

        public IAxisManager HorizontalAxis {
            get => (IAxisManager)GetValue(HorizontalAxisProperty);
            set => SetValue(HorizontalAxisProperty, value);
        }

        public static readonly DependencyProperty VerticalAxisProperty =
            ChartBaseControl.VerticalAxisProperty.AddOwner(
                typeof(MultiChart),
                new FrameworkPropertyMetadata(
                    ChartBaseControl.VerticalAxisProperty.DefaultMetadata.DefaultValue,
                    FrameworkPropertyMetadataOptions.Inherits));

        public IAxisManager VerticalAxis {
            get => (IAxisManager)GetValue(VerticalAxisProperty);
            set => SetValue(VerticalAxisProperty, value);
        }

        public static readonly DependencyProperty FlippedXProperty =
            ChartBaseControl.FlippedXProperty.AddOwner(
                typeof(MultiChart),
                new FrameworkPropertyMetadata(
                    ChartBaseControl.FlippedXProperty.DefaultMetadata.DefaultValue,
                    FrameworkPropertyMetadataOptions.Inherits));

        public bool FlippedX {
            get => (bool)GetValue(FlippedXProperty);
            set => SetValue(FlippedXProperty, BooleanBoxes.Box(value));
        }

        public static readonly DependencyProperty FlippedYProperty =
            ChartBaseControl.FlippedYProperty.AddOwner(
                typeof(MultiChart),
                new FrameworkPropertyMetadata(
                    ChartBaseControl.FlippedYProperty.DefaultMetadata.DefaultValue,
                    FrameworkPropertyMetadataOptions.Inherits));

        public bool FlippedY {
            get => (bool)GetValue(FlippedYProperty);
            set => SetValue(FlippedYProperty, BooleanBoxes.Box(value));
        }

        public static readonly DependencyProperty GraphTitleProperty =
            DependencyProperty.Register(
                nameof(GraphTitle), typeof(string), typeof(MultiChart),
                new PropertyMetadata(string.Empty));

        public string GraphTitle {
            get => (string)GetValue(GraphTitleProperty);
            set => SetValue(GraphTitleProperty, value);
        }

        public static readonly DependencyProperty HorizontalTitleProperty =
            DependencyProperty.Register(
                nameof(HorizontalTitle), typeof(string), typeof(MultiChart),
                new PropertyMetadata(string.Empty));

        public string HorizontalTitle {
            get => (string)GetValue(HorizontalTitleProperty);
            set => SetValue(HorizontalTitleProperty, value);
        }

        public static readonly DependencyProperty VerticalTitleProperty =
            DependencyProperty.Register(
                nameof(VerticalTitle), typeof(string), typeof(MultiChart),
                new PropertyMetadata(string.Empty));

        public string VerticalTitle {
            get => (string)GetValue(VerticalTitleProperty);
            set => SetValue(VerticalTitleProperty, value);
        }

        public static readonly DependencyProperty RenderAreaControlStateProperty =
            DependencyProperty.Register(
                nameof(RenderAreaControlState), typeof(RenderAreaControlState), typeof(MultiChart),
                new PropertyMetadata(null));

        public RenderAreaControlState RenderAreaControlState {
            get => (RenderAreaControlState)GetValue(RenderAreaControlStateProperty);
            set => SetValue(RenderAreaControlStateProperty, value);
        }

        private ChartBaseControl horizontalAxisElement, verticalAxisElement;

        public ChartBaseControl HorizontalAxisElement {
            get => horizontalAxisElement;
            set {
                if (horizontalAxisElement == value) {
                    return;
                }
                if (horizontalAxisElement != null) {
                    BindingOperations.ClearBinding(horizontalAxisElement, ChartBaseControl.HorizontalAxisProperty);
                }
                horizontalAxisElement = value;
                if (horizontalAxisElement != null) {
                    horizontalAxisElement.SetBinding(ChartBaseControl.HorizontalAxisProperty, new Binding(nameof(HorizontalAxis)) { Source = this });
                }
            }
        }

        public ChartBaseControl VerticalAxisElement {
            get => verticalAxisElement;
            set {
                if (verticalAxisElement == value) {
                    return;
                }
                if (verticalAxisElement != null) {
                    BindingOperations.ClearBinding(verticalAxisElement, ChartBaseControl.VerticalAxisProperty);
                }
                verticalAxisElement = value;
                if (verticalAxisElement != null) {
                    verticalAxisElement.SetBinding(ChartBaseControl.VerticalAxisProperty, new Binding(nameof(VerticalAxis)) { Source = this });
                }
            }
        }

        public override void OnApplyTemplate() {
            HorizontalAxisElement = GetTemplateChild("PART_HorizontalAixs") as ChartBaseControl;
            VerticalAxisElement = GetTemplateChild("PART_VerticalAixs") as ChartBaseControl;
        }

        protected override bool IsItemItsOwnContainerOverride(object item) {
            return false;
        }

        protected override DependencyObject GetContainerForItemOverride() {
            return new ContentControl();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonUp(e);
            if (!IsKeyboardFocused) {
                Keyboard.Focus(this);
            }
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e) {
            base.OnMouseRightButtonUp(e);
            if (!IsKeyboardFocused) {
                Keyboard.Focus(this);
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e) {
            base.OnMouseWheel(e);
            if (!IsKeyboardFocused) {
                Keyboard.Focus(this);
            }
        }
    }
}
