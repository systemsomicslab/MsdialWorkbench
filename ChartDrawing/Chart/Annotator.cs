using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;
using CompMs.Common.Extension;
using System.ComponentModel;

namespace CompMs.Graphics.Chart
{
    public class Annotator : ChartBaseControl
    {
        static Annotator() {
            culture = System.Globalization.CultureInfo.GetCultureInfo("en-us");
            typeFace = new Typeface("Calibri");
        }

        public Annotator()
        {
            IsHitTestVisible = false;
        }

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(Annotator),
            new PropertyMetadata(default(System.Collections.IEnumerable), OnItemsSourceChanged)
            );
        public System.Collections.IEnumerable ItemsSource
        {
            get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = (Annotator)d;
            chart.WriteCleanFlag(PropertyClean.ItemsSource, false);
            chart.OnItemsSourceChanged((System.Collections.IEnumerable)e.NewValue, (System.Collections.IEnumerable)e.OldValue);
            chart.Update();
        }

        void OnItemsSourceChanged(System.Collections.IEnumerable newValue, System.Collections.IEnumerable oldValue) {
            if (newValue is INotifyCollectionChanged collectionNew) {
                collectionNew.CollectionChanged += ItemsSourceCollectionChanged;
            }
            if (oldValue is INotifyCollectionChanged collectionOld) {
                collectionOld.CollectionChanged -= ItemsSourceCollectionChanged;
            }
        }

        private void ItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            WriteCleanFlag(PropertyClean.Item, false);
            Update();
        }

        private void UpdateItemsSource() {
            cv = CollectionViewSource.GetDefaultView(ItemsSource);
            if (cv != null && !cv.IsEmpty) {

                if (cv.IsCurrentBeforeFirst || cv.IsCurrentAfterLast) {
                    cv.MoveCurrentToFirst();
                }

                dataType = cv.CurrentItem.GetType();
                datas = cv.Cast<object>().Select(_ => new LabelData()).ToArray();

                WriteCleanFlag(PropertyClean.Horizontal | PropertyClean.Vertical | PropertyClean.Label | PropertyClean.Order, false);
            }
        }

        private void UpdateItem() {
            UpdateItemsSource();
        }

        public static readonly DependencyProperty HorizontalPropertyNameProperty = DependencyProperty.Register(
            nameof(HorizontalPropertyName), typeof(string), typeof(Annotator),
            new PropertyMetadata(default(string), OnHorizontalChanged)
            );
        public string HorizontalPropertyName
        {
            get => (string)GetValue(HorizontalPropertyNameProperty);
            set => SetValue(HorizontalPropertyNameProperty, value);
        }

        static void OnHorizontalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = (Annotator)d;
            chart.WriteCleanFlag(PropertyClean.Horizontal, false);
            chart.Update();
        }

        void UpdateHorizontalItems() {
            hPropertyInfo = dataType?.GetProperty(HorizontalPropertyName);
            if (hPropertyInfo == null) return;

            var hAxis = HorizontalAxis;
            foreach ((var obj, var idx) in cv.Cast<object>().WithIndex()) {
                datas[idx].x = hPropertyInfo == null ? 0 : hAxis.TranslateToAxisValue(hPropertyInfo.GetValue(obj));
            }
        }

        public static readonly DependencyProperty VerticalPropertyNameProperty = DependencyProperty.Register(
            nameof(VerticalPropertyName), typeof(string), typeof(Annotator),
            new PropertyMetadata(default(string), OnVerticalChanged)
            );
        public string VerticalPropertyName
        {
            get => (string)GetValue(VerticalPropertyNameProperty);
            set => SetValue(VerticalPropertyNameProperty, value);
        }

        static void OnVerticalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = (Annotator)d;
            chart.WriteCleanFlag(PropertyClean.Vertical, false);
            chart.Update();
        }

        void UpdateVerticalItems() {
            vPropertyInfo = dataType?.GetProperty(VerticalPropertyName);
            if (vPropertyInfo == null) return;

            var vAxis = VerticalAxis;
            foreach ((var obj, var idx) in cv.Cast<object>().WithIndex()) {
                datas[idx].y = vPropertyInfo == null ? 0 : vAxis.TranslateToAxisValue(vPropertyInfo.GetValue(obj));
            }
        }

        public static readonly DependencyProperty OrderingPropertyNameProperty =
            DependencyProperty.Register(
                nameof(OrderingPropertyName), typeof(string), typeof(Annotator),
                new PropertyMetadata(string.Empty, OnOrderChanged));
        public string OrderingPropertyName {
            get => (string)GetValue(OrderingPropertyNameProperty);
            set => SetValue(OrderingPropertyNameProperty, value);
        }

        private static void OnOrderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (Annotator)d;
            chart.WriteCleanFlag(PropertyClean.Order, false);
            chart.Update();
        }

        void UpdateOrderItems() {
            var prop = dataType?.GetProperty(OrderingPropertyName);
            if (prop == null) return;

            foreach ((var obj, var idx) in cv.Cast<object>().WithIndex()) {
                var value = prop.GetValue(obj);
                if (value is double dvalue) {
                    datas[idx].order = dvalue;
                }
                else if (value is IConvertible conv) {
                    datas[idx].order = Convert.ToDouble(conv);
                }
                else {
                    datas[idx].order = 0;
                }
            }
        }

        public static readonly DependencyProperty LabelPropertyNameProperty = DependencyProperty.Register(
            nameof(LabelPropertyName), typeof(string), typeof(Annotator),
            new PropertyMetadata(default(string), OnLabelChanged));

        public string LabelPropertyName
        {
            get => (string)GetValue(LabelPropertyNameProperty);
            set => SetValue(LabelPropertyNameProperty, value);
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (Annotator)d;
            chart.WriteCleanFlag(PropertyClean.Label, false);
            chart.Update();
        }

        void UpdateLabel() {
            if (dataType == null) return;

            lPropertyInfo = LabelPropertyName != null ? dataType.GetProperty(LabelPropertyName) : null;

            var format = Format;
            foreach ((var obj, var idx) in cv.Cast<object>().Select(o => lPropertyInfo?.GetValue(o)).WithIndex()) {
                if (obj == null)
                    datas[idx].label = string.Empty;
                else if (obj is double val)
                    datas[idx].label = val.ToString(format);
                else
                    datas[idx].label = obj.ToString();
            }

        }

        public static readonly DependencyProperty TopNProperty =
            DependencyProperty.Register(
                nameof(TopN), typeof(int?), typeof(Annotator),
                new PropertyMetadata(null));
        public int? TopN {
            get => (int?)GetValue(TopNProperty);
            set => SetValue(TopNProperty, value);
        }

        public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(
            nameof(Format), typeof(string), typeof(Annotator),
            new PropertyMetadata("N5")
            );
        public string Format {
            get => (string)GetValue(FormatProperty);
            set => SetValue(FormatProperty, value);
        }

        public static readonly DependencyProperty ContentAlignmentProperty =
            DependencyProperty.Register(
                nameof(ContentAlignment), typeof(System.Drawing.ContentAlignment), typeof(Annotator),
                new PropertyMetadata(System.Drawing.ContentAlignment.TopCenter));
        public System.Drawing.ContentAlignment ContentAlignment {
            get => (System.Drawing.ContentAlignment)GetValue(ContentAlignmentProperty);
            set => SetValue(ContentAlignmentProperty, value);
        }

        public static readonly DependencyProperty BrushProperty =
            DependencyProperty.Register(
                nameof(Brush), typeof(Brush), typeof(Annotator),
                new PropertyMetadata(Brushes.Gray));
        public Brush Brush {
            get => (Brush)GetValue(BrushProperty);
            set => SetValue(BrushProperty, value);
        }

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(
                nameof(FontSize), typeof(double), typeof(Annotator),
                new PropertyMetadata(13d));

        public double FontSize {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        private DrawingVisual DV {
            get {
                if (dv == null) {
                    dv = new DrawingVisual();
                    visualChildren.Add(dv);
                }
                return dv;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (!ReadCleanFlag(PropertyClean.ItemsSource)) {
                UpdateItemsSource();
                WriteCleanFlag(PropertyClean.ItemsSource, true);
            }
            if (!ReadCleanFlag(PropertyClean.Item)) {
                UpdateItem();
                WriteCleanFlag(PropertyClean.Item, true);
            }
            if (!ReadCleanFlag(PropertyClean.Horizontal)) {
                UpdateHorizontalItems();
                WriteCleanFlag(PropertyClean.Horizontal, true);
            }
            if (!ReadCleanFlag(PropertyClean.Vertical)) {
                UpdateVerticalItems();
                WriteCleanFlag(PropertyClean.Vertical, true);
            }
            if (!ReadCleanFlag(PropertyClean.Order)) {
                UpdateOrderItems();
                WriteCleanFlag(PropertyClean.Order, true);
            }
            if (!ReadCleanFlag(PropertyClean.Label)) {
                UpdateLabel();
                WriteCleanFlag(PropertyClean.Label, true);
            }
            if (!ReadCleanFlag(PropertyClean.Format)) {
            }


            var dv = DV;
            double actualWidth = ActualWidth, actualHeight = ActualHeight;
            var hAxis = HorizontalAxis;
            var vAxis = VerticalAxis;
            if (hAxis == null || vAxis == null || datas == null)
                return;

            double dx = 0, dy = 0;
            switch (ContentAlignment) {
                case System.Drawing.ContentAlignment.BottomLeft:
                case System.Drawing.ContentAlignment.MiddleLeft:
                case System.Drawing.ContentAlignment.TopLeft:
                    dx = 20;
                    break;
                case System.Drawing.ContentAlignment.TopRight:
                case System.Drawing.ContentAlignment.MiddleRight:
                case System.Drawing.ContentAlignment.BottomRight:
                    dx = -20;
                    break;
            }
            switch (ContentAlignment) {
                case System.Drawing.ContentAlignment.TopCenter:
                case System.Drawing.ContentAlignment.TopLeft:
                case System.Drawing.ContentAlignment.TopRight:
                    dy = -20;
                    break;
                case System.Drawing.ContentAlignment.BottomCenter:
                case System.Drawing.ContentAlignment.BottomLeft:
                case System.Drawing.ContentAlignment.BottomRight:
                    dy = 10;
                    break;
            }

            var counter = 0;
            using (var dc = dv.RenderOpen()) {
                foreach(var data in datas.OrderByDescending(d => d.order)) {
                    if (TopN.HasValue && counter >= TopN)
                        break;
                    if (!string.IsNullOrEmpty(data.label) && hAxis.Contains(data.x) && vAxis.Contains(data.y)) {

                        double xx = hAxis.TranslateToRenderPoint(data.x, FlippedX) * actualWidth;
                        double yy = vAxis.TranslateToRenderPoint(data.y, FlippedY) * actualHeight;

                        var text = new FormattedText(data.label, culture, FlowDirection.LeftToRight, typeFace, FontSize, Brush, 1)
                        {
                            TextAlignment = TextAlignment.Center
                        };
                        dc.DrawText(text, new Point(xx + dx, yy + dy));

                        ++counter;
                    }
                }
            }
        }

        #region field
        private ICollectionView cv;
        private Type dataType;
        private PropertyInfo hPropertyInfo, vPropertyInfo, lPropertyInfo;
        private DrawingVisual dv;
        private LabelData[] datas;
        private static readonly System.Globalization.CultureInfo culture;
        private static readonly Typeface typeFace;
        private PropertyClean cleanFlags = PropertyClean.None;
        #endregion

        private bool ReadCleanFlag(PropertyClean flag) {
            return (cleanFlags & flag) != 0;
        }

        private void WriteCleanFlag(PropertyClean flag, bool set) {
            if (set)
                cleanFlags |= flag;
            else
                cleanFlags &= (~flag);
        }
        
        [Flags]
        enum PropertyClean : uint
        {
            None = 0x0,
            ItemsSource = 0x1,
            Item = 0x2,
            Horizontal = 0x4,
            Vertical = 0x8,
            Label = 0x10,
            Order = 0x20,
            Format = 0x40,
        }

        class LabelData {
            internal AxisValue x, y;
            internal string label;
            internal double order;
            internal Brush brush;
        }
    }
}
