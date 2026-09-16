using CompMs.Common.DataStructure;
using CompMs.Common.Extension;
using CompMs.Graphics.Adorner;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.Graphics.Behavior
{
    public static class AreaSelectBehavior
    {
        static void OnAttached(FrameworkElement fe) {
            fe.MouseLeftButtonDown += DrawOnMouseLeftButtonDown;
            fe.MouseMove += DrawOnMouseMove;
            fe.MouseLeftButtonUp += DrawOnMouseLeftButtonUp;

            var source = GetItemsSource(fe)?.OfType<object>().ToList() ?? new List<object>();
            var datatype = source.Count > 0 ? source[0].GetType() : null;

            Func<object, double> horizontalGetter = _ => 0d;
            var horizontalProperty = GetHorizontalProperty(fe);
            var horizontalAxis = ChartBaseControl.GetHorizontalAxis(fe);
            if (!string.IsNullOrEmpty(horizontalProperty) && horizontalAxis != null && datatype != null) {
                horizontalGetter = AdornerSelectObserver.BuildGetter(datatype, horizontalProperty, horizontalAxis);
            }

            Func<object, double> verticalGetter = _ => 0d;
            var verticalProperty = GetVerticalProperty(fe);
            var verticalAxis = ChartBaseControl.GetVerticalAxis(fe);
            if (!string.IsNullOrEmpty(verticalProperty) && verticalAxis != null && datatype != null) {
                verticalGetter = AdornerSelectObserver.BuildGetter(datatype, verticalProperty, verticalAxis);
            }

            var observer = new AdornerSelectObserver(source, horizontalGetter, verticalGetter, fe);
            observer.SetColors(GetAreaColors(fe));
            SetSelectionObserver(fe, observer);
            // fe.CoerceValue(SelectedItemsProperty);
            SetSelectedItems(fe, observer.SelectedItems);
        }

        static void OnDetaching(FrameworkElement fe) {
            fe.MouseLeftButtonDown -= DrawOnMouseLeftButtonDown;
            fe.MouseMove -= DrawOnMouseMove;
            fe.MouseLeftButtonUp -= DrawOnMouseLeftButtonUp;
            GetSelectionObserver(fe)?.Dispose();
            SetSelectionObserver(fe, null);
            //fe.CoerceValue(SelectedItemsProperty);
            SetSelectedItems(fe, null);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.RegisterAttached(
                "ItemsSource",
                typeof(IList),
                typeof(AreaSelectBehavior),
                new PropertyMetadata(OnItemsSourceChanged));
        public static IList GetItemsSource(DependencyObject obj) => (IList)obj.GetValue(ItemsSourceProperty);
        public static void SetItemsSource(DependencyObject obj, IList value) => obj.SetValue(ItemsSourceProperty, value);

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement fe) {
                if (e.OldValue != null) {
                    OnDetaching(fe);
                }
                if (e.NewValue != null) {
                    OnAttached(fe);
                }
            }
        }

        public static readonly DependencyProperty HorizontalPropertyProperty =
            DependencyProperty.RegisterAttached(
                "HorizontalProperty",
                typeof(string),
                typeof(AreaSelectBehavior),
                new PropertyMetadata(
                    string.Empty,
                    OnHorizontalPropertyChanged));
        public static string GetHorizontalProperty(DependencyObject obj) => (string)obj.GetValue(HorizontalPropertyProperty);
        public static void SetHorizontalProperty(DependencyObject obj, string value) => obj.SetValue(HorizontalPropertyProperty, value);

        private static void OnHorizontalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement fe) {
                if (!string.IsNullOrEmpty((string)e.OldValue)) {
                    OnDetaching(fe);
                }
                if (!string.IsNullOrEmpty((string)e.NewValue)) {
                    OnAttached(fe);
                }
            }
        }

        public static readonly DependencyProperty VerticalPropertyProperty =
            DependencyProperty.RegisterAttached(
                "VerticalProperty",
                typeof(string),
                typeof(AreaSelectBehavior),
                new PropertyMetadata(
                    string.Empty,
                    OnVerticalPropertyChanged));
        public static string GetVerticalProperty(DependencyObject obj) => (string)obj.GetValue(VerticalPropertyProperty);
        public static void SetVerticalProperty(DependencyObject obj, string value) => obj.SetValue(VerticalPropertyProperty, value);

        private static void OnVerticalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement fe) {
                if (!string.IsNullOrEmpty((string)e.OldValue)) {
                    OnDetaching(fe);
                }
                if (!string.IsNullOrEmpty((string)e.NewValue)) {
                    OnAttached(fe);
                }
            }
        }

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItems",
                typeof(ObservableCollection<ObservableCollection<object>>),
                typeof(AreaSelectBehavior));//,
                // new FrameworkPropertyMetadata(
                //     null,
                //     OnSelectedItemsChanged
                //     //, CoerceSelectedItems
                //     ));
        public static ObservableCollection<ObservableCollection<object>> GetSelectedItems(DependencyObject obj) => (ObservableCollection<ObservableCollection<object>>)obj.GetValue(SelectedItemsProperty);
        public static void SetSelectedItems(DependencyObject obj, ObservableCollection<ObservableCollection<object>> value) => obj.SetValue(SelectedItemsProperty, value);

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            
        }

        private static object CoerceSelectedItems(DependencyObject d, object value) {
            var observer = GetSelectionObserver(d);
            if (observer != null) {
                return observer.SelectedItems;
            }
            return null;
        }

        public static readonly DependencyProperty AreaColorsProperty =
            DependencyProperty.RegisterAttached(
                "AreaColors",
                typeof(IReadOnlyList<Color>),
                typeof(AreaSelectBehavior),
                new PropertyMetadata(
                    new[] { Colors.Red, Colors.Blue, },
                    OnAreaColorChanged));

        public static IReadOnlyList<Color> GetAreaColors(DependencyObject obj) => (IReadOnlyList<Color>)obj.GetValue(AreaColorsProperty);
        public static void SetAreaColors(DependencyObject obj, IReadOnlyList<Color> value) => obj.SetValue(AreaColorsProperty, value);

        private static void OnAreaColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var observer = GetSelectionObserver(d);
            if (e.NewValue != null && observer != null) {
                observer.SetColors((IEnumerable<Color>)e.NewValue);
            }
        }

        static readonly DependencyProperty InitialPointProperty =
            DependencyProperty.RegisterAttached(
                "InitialPoint",
                typeof(Point),
                typeof(AreaSelectBehavior),
                new PropertyMetadata());

        static Point GetInitialPoint(DependencyObject obj) => (Point)obj.GetValue(InitialPointProperty);
        static void SetInitialPoint(DependencyObject obj, Point value) => obj.SetValue(InitialPointProperty, value);

        static readonly DependencyProperty CurrentAdornerProperty =
            DependencyProperty.RegisterAttached(
                "CurrentFocusAdorner",
                typeof(ChartSelectionRubberAdorner),
                typeof(AreaSelectBehavior),
                new PropertyMetadata(null));

        static ChartSelectionRubberAdorner GetCurrentAdorner(DependencyObject obj) => (ChartSelectionRubberAdorner)obj.GetValue(CurrentAdornerProperty);
        static void SetCurrentAdorner(DependencyObject obj, ChartSelectionRubberAdorner value) => obj.SetValue(CurrentAdornerProperty, value);

        static readonly DependencyProperty SelectionObserverProperty =
            DependencyProperty.RegisterAttached(
                "SelectionObserver",
                typeof(AdornerSelectObserver),
                typeof(AreaSelectBehavior),
                new PropertyMetadata(null));

        static AdornerSelectObserver GetSelectionObserver(DependencyObject obj) => (AdornerSelectObserver)obj.GetValue(SelectionObserverProperty);
        static void SetSelectionObserver(DependencyObject obj, AdornerSelectObserver value) => obj.SetValue(SelectionObserverProperty, value);

        static void DrawOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (sender is FrameworkElement fe) {
                var adorners = GetSelectionObserver(fe);
                if (GetAreaColors(fe).Count <= adorners.Adorners.Count(adorner_ => adorner_.IsSelected)) {
                    return;
                }
                var initial = e.GetPosition(fe);
                SetInitialPoint(fe, initial);
                var adorner = new ChartSelectionRubberAdorner(fe, initial, Colors.Gray) { IsClipEnabled = true, };
                adorner.Attach();
                SetCurrentAdorner(fe, adorner);
                fe.CaptureMouse();

                foreach (var adorner_ in adorners.Adorners) {
                    if (!adorner_.IsSelected) {
                        adorner_.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        static void DrawOnMouseMove(object sender, MouseEventArgs e) {
            if (sender is FrameworkElement fe) {
                var adorner = GetCurrentAdorner(fe);
                if (adorner != null) {
                    adorner.CurrentPoint = e.GetPosition(fe);
                }
            }
        }

        static void DrawOnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (sender is FrameworkElement fe) {
                var adorner = GetCurrentAdorner(fe);
                if (adorner != null) {
                    fe.ReleaseMouseCapture();
                    SetCurrentAdorner(fe, null);

                    var offset = e.GetPosition(fe) - GetInitialPoint(fe);

                    var adorners = GetSelectionObserver(fe);

                    if (Math.Abs(offset.X) < SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(offset.Y) < SystemParameters.MinimumVerticalDragDistance) {
                    
                        foreach (var adorner_ in adorners.Adorners) {
                            if (!adorner_.IsSelected) {
                                adorner_.Visibility = Visibility.Visible;
                            }
                        }
                        adorner.Detach();
                        return;
                    }

                    foreach (var adorner_ in adorners.Adorners) {
                        if (!adorner_.IsSelected) {
                            adorner_.Detach();
                        }
                    }
                    adorners.RemoveAdornerAll(adorner_ => !adorner_.IsSelected);
                    adorners.AddAdorner(adorner);
                    foreach ((var adorner_, var color) in adorners.Adorners.ZipInternal(GetAreaColors(fe))) {
                        adorner_.ChangeColor(color);
                    }
                }
            }
        }

        class AdornerSelectObserver : IObserver<ChartSelectionRubberAdorner>, IDisposable
        {
            public ObservableCollection<ObservableCollection<object>> SelectedItems { get; }

            public AdornerSelectObserver(IReadOnlyList<object> source, Func<object, double> horizontal, Func<object, double> vertical, FrameworkElement fe) {
                adorners = new List<ChartSelectionRubberAdorner>();
                Adorners = adorners.AsReadOnly();
                tree = KdTree.Build(source, horizontal, vertical);
                SelectedItems = new ObservableCollection<ObservableCollection<object>>();
                this.fe = fe;
            }

            public ReadOnlyCollection<ChartSelectionRubberAdorner> Adorners { get; }

            public List<Color> AreaColors { get; set; } = new List<Color> { Colors.Red, Colors.Blue };

            private readonly List<ChartSelectionRubberAdorner> adorners;
            private readonly List<Color> colors = new List<Color>();
            private FrameworkElement fe;
            private KdTree<object> tree;
            private bool disposed = false;
            private Dictionary<ChartSelectionRubberAdorner, ObservableCollection<object>> map = new Dictionary<ChartSelectionRubberAdorner, ObservableCollection<object>>();

            public void AddAdorner(ChartSelectionRubberAdorner adorner) {
                adorners.Add(adorner);
                adorner.Subscribe(this);
            }

            public void RemoveAdorner(ChartSelectionRubberAdorner adorner) {
                adorners.Remove(adorner);
            }

            public void RemoveAdornerAll(Predicate<ChartSelectionRubberAdorner> predicate) {
                adorners.RemoveAll(predicate);
            }

            public void SetColors(IEnumerable<Color> colors) {
                this.colors.Clear();
                this.colors.AddRange(colors);
            }

            public void OnCompleted() {
                Dispose();
            }

            public void OnError(Exception error) {
                Dispose();
            }

            public void OnNext(ChartSelectionRubberAdorner value) {
                if (disposed) {
                    return;
                }
                if (value.IsSelected) {
                    var minx = Math.Min(value.InitialX.Value, value.CurrentX.Value);
                    var maxx = Math.Max(value.InitialX.Value, value.CurrentX.Value);
                    var miny = Math.Min(value.InitialY.Value, value.CurrentY.Value);
                    var maxy = Math.Max(value.InitialY.Value, value.CurrentY.Value);
                    var list = new ObservableCollection<object>(tree.RangeSearch(new[] { minx, miny, }, new[] { maxx, maxy, }));
                    if (map.TryGetValue(value, out var prev) && SelectedItems.Contains(prev)) {
                        var idx = SelectedItems.IndexOf(prev);
                        SelectedItems[idx] = list;
                    }
                    else {
                        SelectedItems.Add(list);
                    }
                    map[value] = list;
                }
                else {
                    if (map.ContainsKey(value)) {
                        SelectedItems.Remove(map[value]);
                    }
                }
                var beb = BindingOperations.GetBindingExpression(fe, SelectedItemsProperty);
                if (beb != null) {
                    beb.UpdateSource();
                }
                RefreshColors();
            }

            private void RefreshColors() {
                foreach ((var adorner, var color) in Adorners.ZipInternal(colors)) {
                    adorner.ChangeColor(color);
                }
            }

            public void Dispose() {
                tree = null;
                map = null;
                SelectedItems.Clear();
                disposed = true;
            }

            public static Func<object, double> BuildGetter(Type type, string property, IAxisManager am) {
                var arg = System.Linq.Expressions.Expression.Parameter(typeof(object));
                var casted = System.Linq.Expressions.Expression.Convert(arg, type);
                var prop = System.Linq.Expressions.Expression.Property(casted, property);
                var result = System.Linq.Expressions.Expression.Convert(prop, typeof(object));
                var lambda = System.Linq.Expressions.Expression.Lambda<Func<object, object>>(result, arg);
                var getter = lambda.Compile();
                return (object v) => am.TranslateToAxisValue(getter(v)).Value;
            }
        }
    }
}
