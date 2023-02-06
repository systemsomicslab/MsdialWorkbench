using CompMs.Graphics.Adorner;
using CompMs.Graphics.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CompMs.Graphics.Behavior
{
    public class ScatterFocusBehavior
    {
        public static readonly DependencyProperty EnableOuterProperty =
            DependencyProperty.RegisterAttached("EnableOuter", typeof(bool), typeof(ScatterFocusBehavior), new PropertyMetadata(false, EnableOuterPropertyChanged));

        public static bool GetEnableOuter(DependencyObject obj) => (bool)obj.GetValue(EnableOuterProperty);
        public static void SetEnableOuter(DependencyObject obj, bool value) => obj.SetValue(EnableOuterProperty, value);

        public static void EnableOuterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ScatterControl scatter) {
                if ((bool)e.NewValue) {
                    OnOuterAttached(scatter);
                }
                else {
                    OnOuterDetaching(scatter);
                }
            }
        }

        public static readonly DependencyProperty OuterScaleProperty =
            DependencyProperty.RegisterAttached("OuterScale", typeof(double), typeof(ScatterFocusBehavior), new PropertyMetadata(2d));
        public static double GetOuterScale(DependencyObject obj) => (double)obj.GetValue(OuterScaleProperty);
        public static void SetOuterScale(DependencyObject obj, double value) => obj.SetValue(OuterScaleProperty, value);

        public static readonly DependencyProperty InnerScaleProperty =
            DependencyProperty.RegisterAttached("InnerScale", typeof(double), typeof(ScatterFocusBehavior), new PropertyMetadata(1.5));

        public static double GetInnerScale(DependencyObject obj) => (double)obj.GetValue(InnerScaleProperty);
        public static void SetInnerScale(DependencyObject obj, double value) => obj.SetValue(InnerScaleProperty, value);

        private static void OnOuterAttached(ScatterControl scatter) {
            scatter.MouseEnter += DrawFocusOnMouseEnter;
            scatter.MouseLeave += DeleteFocusOnMouseLeave;
        }

        private static void OnOuterDetaching(ScatterControl scatter) {
            scatter.MouseEnter -= DrawFocusOnMouseEnter;
            scatter.MouseLeave -= DeleteFocusOnMouseLeave;
        }

        static void DrawFocusOnMouseEnter(object sender, MouseEventArgs e) {
            if (sender is ScatterControl scatter) {
                if (!(scatter.GetValue(ScatterFocusAdorner.AdornerProperty) is ScatterFocusAdorner adorner)) {
                    var outerScale = GetOuterScale(scatter);
                    var innerScale = GetInnerScale(scatter);
                    var targetPoint = GetLabelTargetPoint(scatter);
                    adorner = new ScatterFocusAdorner(scatter, outerScale, innerScale) { TargetPoint = targetPoint };
                    scatter.SetValue(ScatterFocusAdorner.AdornerProperty, adorner);
                }
                adorner.Attach();
            }
        }

        static void DeleteFocusOnMouseLeave(object sender, MouseEventArgs e) {
            if (sender is ScatterControl scatter) {
                var adorner = (ScatterFocusAdorner)scatter.GetValue(ScatterFocusAdorner.AdornerProperty);
                if (adorner != null) {
                    adorner.Detach();
                }
            }
        }

        public static readonly DependencyProperty LabelTemplateProperty =
            DependencyProperty.RegisterAttached("LabelTemplate", typeof(DataTemplate), typeof(ScatterFocusBehavior), new PropertyMetadata(null, LabelTemplatePropertyChanged));
        public static DataTemplate GetLabelTemplate(DependencyObject obj) => (DataTemplate)obj.GetValue(LabelTemplateProperty);
        public static void SetLabelTemplate(DependencyObject obj, DataTemplate value) => obj.SetValue(LabelTemplateProperty, value);

        public static void LabelTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement element) {
                if (e.OldValue != null) {
                    OnLabelDetaching(element);
                }
                if (e.NewValue != null) {
                    OnLabelAttached(element);
                }
            }
        }

        public static readonly DependencyProperty LabelDataContextProperty =
            DependencyProperty.RegisterAttached("LabelDataContext", typeof(object), typeof(ScatterFocusBehavior), new PropertyMetadata(null, LabelDataContextPropertyChanged));
        public static object GetLabelDataContext(DependencyObject obj) => (object)obj.GetValue(LabelDataContextProperty);
        public static void SetLabelDataContext(DependencyObject obj, object value) => obj.SetValue(LabelDataContextProperty, value);

        public static void LabelDataContextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement element) {
                if (element.GetValue(ContentAdorner.AdornerProperty) is ContentAdorner adorner) {
                    var dc = GetLabelDataContext(element);
                    adorner.DataContext = dc;
                }
            }
        }

        public static readonly DependencyProperty LabelAlignmentProperty =
            DependencyProperty.RegisterAttached("LabelAlignment", typeof(System.Drawing.ContentAlignment), typeof(ScatterFocusBehavior), new PropertyMetadata(System.Drawing.ContentAlignment.TopCenter, LabelAlignmentPropertyChanged));
        public static System.Drawing.ContentAlignment GetLabelAlignment(DependencyObject obj) => (System.Drawing.ContentAlignment)obj.GetValue(LabelAlignmentProperty);
        public static void SetLabelAlignment(DependencyObject obj, System.Drawing.ContentAlignment value) => obj.SetValue(LabelAlignmentProperty, value);

        public static void LabelAlignmentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement element) {
                if (element.GetValue(ContentAdorner.AdornerProperty) is ContentAdorner adorner) {
                    var alignment = GetLabelAlignment(element);
                    adorner.Alignment = alignment;
                }
            }
        }

        public static readonly DependencyProperty LabelTargetPointProperty =
            DependencyProperty.RegisterAttached("LabelTargetPoint", typeof(Point?), typeof(ScatterFocusBehavior), new PropertyMetadata(null, LabelTargetPointPropertyChanged));
        public static Point? GetLabelTargetPoint(DependencyObject obj) => (Point?)obj.GetValue(LabelTargetPointProperty);
        public static void SetLabelTargetPoint(DependencyObject obj, Point? value) => obj.SetValue(LabelTargetPointProperty, value);

        public static void LabelTargetPointPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement element) {
                if (element.GetValue(ContentAdorner.AdornerProperty) is ContentAdorner cadorner) {
                    var point = GetLabelTargetPoint(element);
                    cadorner.TargetPoint = point;
                    cadorner.InvalidateVisual();
                }
            }
            if (d is ScatterControl scatter) {
                if (scatter.GetValue(ScatterFocusAdorner.AdornerProperty) is ScatterFocusAdorner sadorner) {
                    var point = GetLabelTargetPoint(scatter);
                    sadorner.TargetPoint = point;
                    sadorner.InvalidateVisual();
                }
            }
        }

        public static readonly DependencyProperty LabelTargetRadiusProperty =
            DependencyProperty.RegisterAttached("LabelTargetRadius", typeof(double), typeof(ScatterFocusBehavior), new PropertyMetadata(0d, LabelTargetRadiusPropertyChanged));
        public static double GetLabelTargetRadius(DependencyObject obj) => (double)obj.GetValue(LabelTargetRadiusProperty);
        public static void SetLabelTargetRadius(DependencyObject obj, double value) => obj.SetValue(LabelTargetRadiusProperty, value);

        public static void LabelTargetRadiusPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement element) {
                if (element.GetValue(ContentAdorner.AdornerProperty) is ContentAdorner adorner) {
                    var radius = GetLabelTargetRadius(element);
                    if (GetEnableOuter(element)) {
                        radius *= GetOuterScale(element);
                    }
                    adorner.TargetRadius = radius;
                }
            }
        }

        private static void UpdateProperty(FrameworkElement element) {
            var adorner = element.GetValue(ContentAdorner.AdornerProperty) as ContentAdorner;
            if (adorner?.IsAttached ?? false) {
                adorner.Detach();
            }
            var template = GetLabelTemplate(element);
            var dc = GetLabelDataContext(element);
            var alignment = GetLabelAlignment(element);
            var point = GetLabelTargetPoint(element);
            var radius = GetLabelTargetRadius(element);
            if (GetEnableOuter(element)) {
                radius *= GetOuterScale(element);
            }
            adorner = null;
            if (template?.LoadContent() is FrameworkElement label) {
                adorner = new ContentAdorner(element, label)
                {
                    DataContext = dc,
                    Alignment = alignment,
                    TargetPoint = point,
                    TargetRadius = radius
                };
                adorner.Attach();
            }
            element.SetValue(ContentAdorner.AdornerProperty, adorner);
        }

        private static void OnLabelAttached(FrameworkElement element) {
            element.MouseEnter += DrawLabelOnMouseEnter;
            element.MouseLeave += DeleteLabelOnMouseLeave;
        }

        private static void OnLabelDetaching(FrameworkElement element) {
            element.MouseEnter -= DrawLabelOnMouseEnter;
            element.MouseLeave -= DeleteLabelOnMouseLeave;
        }

        private static void DrawLabelOnMouseEnter(object sender, MouseEventArgs e) {
            if (sender is FrameworkElement element) {
                var adorner = (ContentAdorner)element.GetValue(ContentAdorner.AdornerProperty);
                if (adorner == null)
                    UpdateProperty(element);
                adorner?.Attach();
            }
        }

        private static void DeleteLabelOnMouseLeave(object sender, MouseEventArgs e) {
            if (sender is FrameworkElement element) {
                var adorner = (ContentAdorner)element.GetValue(ContentAdorner.AdornerProperty);
                adorner?.Detach();
            }
        }
    }
}
