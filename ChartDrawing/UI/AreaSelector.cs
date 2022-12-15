using CompMs.Graphics.Base;
using CompMs.Graphics.Helper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CompMs.Graphics.UI
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CompMs.Graphics.UI"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CompMs.Graphics.UI;assembly=CompMs.Graphics.UI"
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
    ///     <MyNamespace:AreaSelector/>
    ///
    /// </summary>
    public sealed class AreaSelector : Canvas
    {
        static AreaSelector() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AreaSelector), new FrameworkPropertyMetadata(typeof(AreaSelector)));
        }

        public static readonly DependencyProperty SelectedPointsProperty =
            DependencyProperty.Register(
                nameof(SelectedPoints),
                typeof(List<Point>),
                typeof(AreaSelector),
                new PropertyMetadata(
                    null,
                    OnSelectedPointsChanged,
                    CoerceSelectedPoints));

        public List<Point> SelectedPoints {
            get => (List<Point>)GetValue(SelectedPointsProperty);
            set => SetValue(SelectedPointsProperty, value);
        }

        private static void OnSelectedPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((AreaSelector)d).OnSelectedPointsChanged((List<Point>)e.OldValue, (List<Point>)e.NewValue);
        }

        private void OnSelectedPointsChanged(List<Point> oldValue, List<Point> newValue) {
            if (_mode != AreaSelectorMode.Selecting) {
                foreach (var v in _visuals.Concat(_previousVisuals)) {
                    Children.Remove(v);
                }
                _visuals.Clear();
                _previousVisuals.Clear();
                if (newValue.Count >= 3) {
                    var thickness = StrokeThickness;
                    var stroke = Stroke;
                    for (int i = 1; i <= newValue.Count; i++) {
                        var ellipse = new Ellipse
                        {
                            Width = thickness * 2,
                            Height = thickness * 2,
                            Fill = stroke,
                            IsHitTestVisible = false,
                        };
                        SetLeft(ellipse, newValue[i % newValue.Count].X - thickness);
                        SetTop(ellipse, newValue[i % newValue.Count].Y - thickness);
                        _visuals.Add(ellipse);
                        Children.Add(ellipse);
                        var line = new Line
                        {
                            X1 = newValue[i - 1].X,
                            Y1 = newValue[i - 1].Y,
                            X2 = newValue[i % newValue.Count].X,
                            Y2 = newValue[i % newValue.Count].Y,
                            Stroke = stroke,
                            StrokeThickness = thickness,
                            IsHitTestVisible = false,
                        };
                        _visuals.Add(line);
                        Children.Add(line);
                    }
                    _mode = AreaSelectorMode.Selected;
                }
                _mode = AreaSelectorMode.UnSelected;
            }
        }

        private static object CoerceSelectedPoints(DependencyObject d, object value) {
            var selector = (AreaSelector)d;
            if (selector._mode != AreaSelectorMode.Selecting) {
                if (value is List<Point> points && points.Count >= 3) {
                    return value;
                }
                return null;
            }
            return selector.SelectedPoints;
        }

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register(
                nameof(Stroke), typeof(Brush), typeof(AreaSelector),
                new PropertyMetadata(
                    Brushes.Red,
                    OnStrokeChanged));
        
        public Brush Stroke {
            get => (Brush)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        private static void OnStrokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((AreaSelector)d).OnStrokeChanged((Brush)e.OldValue, (Brush)e.NewValue);
        }

        private void OnStrokeChanged(Brush oldValue, Brush newValue) {
            foreach (var v in _visuals.Concat(_previousVisuals).OfType<Line>()) {
                v.Stroke = newValue;
            }
            foreach (var v in _visuals.Concat(_previousVisuals).OfType<Ellipse>()) {
                v.Fill = newValue;
            }
        }

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness), typeof(double), typeof(AreaSelector),
                new PropertyMetadata(
                    2d,
                    OnStrokeThicknessChanged));

        public double StrokeThickness {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        private static void OnStrokeThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((AreaSelector)d).OnStrokeThicknessChanged((double)e.OldValue, (double)e.NewValue);
        }

        private void OnStrokeThicknessChanged(double oldValue, double newValue) {
            foreach (var v in _visuals.Concat(_previousVisuals).OfType<Line>()) {
                v.StrokeThickness = newValue;
            }
            foreach (var v in _visuals.Concat(_previousVisuals).OfType<Ellipse>()) {
                v.Width = newValue * 2;
                v.Height = newValue * 2;
                SetTop(v, GetTop(v) + oldValue - newValue);
                SetLeft(v, GetLeft(v) + oldValue - newValue);
            }
        }

        public static readonly DependencyProperty IsSelectableProperty =
            DependencyProperty.Register(
                nameof(IsSelectable), typeof(bool), typeof(AreaSelector),
                new PropertyMetadata(BooleanBoxes.TrueBox));
        
        public bool IsSelectable {
            get => (bool)GetValue(IsSelectableProperty);
            set => SetValue(IsSelectableProperty, BooleanBoxes.Box(value));
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonUp(e);
            var p = e.GetPosition(this);
            if (_mode != AreaSelectorMode.Selecting) {
                if (IsSelectable) {
                    StartSelecting(p);
                }
            }
            else {
                var (i, r) = InteractPoint(p);
                if (i >= 0) {
                    ContinueSelecting(r);
                    FinishSelecting(i);
                    return;
                }
                ContinueSelecting(p);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (_mode == AreaSelectorMode.Selecting && _currentLine != null) {
                var p = e.GetPosition(this);
                _currentLine.X2 = p.X;
                _currentLine.Y2 = p.Y;
            }
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e) {
            base.OnMouseRightButtonUp(e);
            if (_mode == AreaSelectorMode.Selecting) {
                CancelSelecting();
            }
        }

        private AreaSelectorMode _mode = AreaSelectorMode.UnSelected;
        private AreaSelectorMode _previousMode = AreaSelectorMode.UnSelected;
        private ObservableCollection<Point> _selectingPoints;
        private Line _currentLine;
        private readonly List<Shape> _visuals = new List<Shape>();
        private readonly List<Shape> _previousVisuals = new List<Shape>();

        private void StartSelecting(Point p) {
            _selectingPoints = new ObservableCollection<Point> { p, };
            _currentLine = new Line()
            {
                X1 = p.X,
                Y1 = p.Y,
                X2 = p.X,
                Y2 = p.Y,
                Stroke = Stroke,
                StrokeThickness = StrokeThickness,
                IsHitTestVisible = false,
            };
            Children.Add(_currentLine);
            _previousMode = _mode;
            _previousVisuals.AddRange(_visuals);
            foreach (var v in _previousVisuals) {
                Children.Remove(v);
            }
            _visuals.Clear();
            var ellipse = new Ellipse()
            {
                Width = StrokeThickness * 2,
                Height = StrokeThickness * 2,
                Fill = Stroke,
                IsHitTestVisible = false,
            };
            SetLeft(ellipse, p.X - StrokeThickness);
            SetTop(ellipse, p.Y - StrokeThickness);
            Children.Add(ellipse);
            _visuals.Add(ellipse);
            _mode = AreaSelectorMode.Selecting;
            CaptureMouse();
        }

        private void ContinueSelecting(Point p) {
            _selectingPoints.Add(p);
            _currentLine.X2 = p.X;
            _currentLine.Y2 = p.Y;
            _visuals.Add(_currentLine);
            _currentLine = new Line()
            {
                X1 = p.X,
                Y1 = p.Y,
                X2 = p.X,
                Y2 = p.Y,
                Stroke = Stroke,
                StrokeThickness = StrokeThickness,
                IsHitTestVisible = false,
            };
            Children.Add(_currentLine);
            var ellipse = new Ellipse()
            {
                Width = StrokeThickness * 2,
                Height = StrokeThickness * 2,
                Fill = Stroke,
                IsHitTestVisible = false,
            };
            SetLeft(ellipse, p.X - StrokeThickness);
            SetTop(ellipse, p.Y - StrokeThickness);
            Children.Add(ellipse);
            _visuals.Add(ellipse);
        }
        
        private (int, Point) InteractPoint(Point p) {
            var q = _selectingPoints[_selectingPoints.Count - 1];
            for (int i = 1; i < _selectingPoints.Count - 1; i++) {
                var r = GeometricCalculationHelper.Interaction(_selectingPoints[i - 1], _selectingPoints[i], q, p);
                if (GeometricCalculationHelper.OnSegment(_selectingPoints[i - 1], _selectingPoints[i], r)
                    && GeometricCalculationHelper.OnSegment(p, q, r)) {
                    return (i, r);
                }
            }
            return (-1, default);
        }

        private void FinishSelecting(int numSkip) {
            var selected = _selectingPoints.Skip(numSkip).ToList();
            SelectedPoints = selected;
            _selectingPoints = null;
            ReleaseMouseCapture();
            for (int i = 0; i < numSkip * 2; i++) {
                Children.Remove(_visuals[i]);
            }
            _visuals.RemoveRange(0, numSkip * 2);

            _currentLine.X2 = selected[0].X;
            _currentLine.Y2 = selected[0].Y;
            _visuals.Add(_currentLine);
            _currentLine = null;
            _mode = AreaSelectorMode.Selected;

            foreach (var v in _previousVisuals) {
                Children.Remove(v);
            }
            _previousVisuals.Clear();
        }

        private void CancelSelecting() {
            ReleaseMouseCapture();
            _selectingPoints = null;
            foreach (var v in _visuals) {
                Children.Remove(v);
            }
            _visuals.Clear();
            Children.Remove(_currentLine);
            _currentLine = null;
            _visuals.AddRange(_previousVisuals);
            foreach (var v in _previousVisuals) {
                Children.Add(v);
            }
            _previousVisuals.Clear();
            _mode = _previousMode;
        }

        enum AreaSelectorMode {
            UnSelected,
            Selecting,
            Selected,
        }
    }
}
