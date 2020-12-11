using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;
using CompMs.Common.Extension;

namespace CompMs.Graphics.Chart
{
    public class Annotator : ChartBaseControl
    {
        #region DependencyProperty
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(Annotator),
            new PropertyMetadata(default(System.Collections.IEnumerable), OnItemsSourceChanged)
            );

        public static readonly DependencyProperty HorizontalPropertyNameProperty = DependencyProperty.Register(
            nameof(HorizontalPropertyName), typeof(string), typeof(Annotator),
            new PropertyMetadata(default(string), OnHorizontalPropertyNameChanged)
            );

        public static readonly DependencyProperty VerticalPropertyNameProperty = DependencyProperty.Register(
            nameof(VerticalPropertyName), typeof(string), typeof(Annotator),
            new PropertyMetadata(default(string), OnVerticalPropertyNameChanged)
            );

        public static readonly DependencyProperty LabelPropertyNameProperty = DependencyProperty.Register(
            nameof(LabelPropertyName), typeof(string), typeof(Annotator),
            new PropertyMetadata(default(string), OnLabelPropertyNameChanged)
            );

        public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(
            nameof(Format), typeof(string), typeof(Annotator),
            new PropertyMetadata("N5", ChartUpdate)
            );
        #endregion

        #region Property
        public System.Collections.IEnumerable ItemsSource
        {
            get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public string HorizontalPropertyName
        {
            get => (string)GetValue(HorizontalPropertyNameProperty);
            set => SetValue(HorizontalPropertyNameProperty, value);
        }

        public string VerticalPropertyName
        {
            get => (string)GetValue(VerticalPropertyNameProperty);
            set => SetValue(VerticalPropertyNameProperty, value);
        }

        public string LabelPropertyName
        {
            get => (string)GetValue(LabelPropertyNameProperty);
            set => SetValue(LabelPropertyNameProperty, value);
        }

        public string Format {
            get => (string)GetValue(FormatProperty);
            set => SetValue(FormatProperty, value);
        }
        private DrawingVisual DV => dv ?? (dv = new DrawingVisual());
        #endregion

        #region field
        private CollectionView cv;
        private Type dataType;
        private PropertyInfo hPropertyInfo, vPropertyInfo, lPropertyInfo;
        private DrawingVisual dv;
        private LabelData[] datas;
        private static readonly System.Globalization.CultureInfo culture;
        private static readonly Typeface typeFace;
        #endregion

        static Annotator() {
            culture = System.Globalization.CultureInfo.GetCultureInfo("en-us");
            typeFace = new Typeface("Calibri");
        }

        public Annotator()
        {
            ClipToBounds = true;
            IsHitTestVisible = false;
        }

        protected override void Update()
        {
            base.Update();
            if (  datas == null
               || HorizontalAxis == null
               || VerticalAxis == null
               )
                return;

            var dv = DV;
            if (dv.Parent == null) {
                visualChildren.Add(dv);
            }
            double actualWidth = ActualWidth, actualHeight = ActualHeight;
            var hAxis = HorizontalAxis;
            var vAxis = VerticalAxis;

            using (var dc = dv.RenderOpen()) {
                foreach(var data in datas) {
                    if (!string.IsNullOrEmpty(data.label) && hAxis.Contains(data.x) && vAxis.Contains(data.y)) {

                        double xx = hAxis.TranslateToRenderPoint(data.x, FlippedX) * actualWidth;
                        double yy = vAxis.TranslateToRenderPoint(data.y, FlippedY) * actualHeight;

                        var text = new FormattedText(data.label, culture, FlowDirection.LeftToRight, typeFace, 13, Brushes.Gray, 1)
                        {
                            TextAlignment = TextAlignment.Center
                        };
                        dc.DrawText(text, new Point(xx, yy));
                    }
                }
            }
        }

        private void ItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            SetHorizontalDatas();
            SetVerticalDatas();
            SetLabelDatas();
            Update();
        }

        #region Event handler
        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as Annotator;
            if (chart == null) return;

            chart.dataType = null;
            chart.cv = null;

            if (chart.ItemsSource == null) return;

            if (e.NewValue is INotifyCollectionChanged collectionNew) {
                collectionNew.CollectionChanged += chart.ItemsSourceCollectionChanged;
            }
            if (e.OldValue is INotifyCollectionChanged collectionOld) {
                collectionOld.CollectionChanged -= chart.ItemsSourceCollectionChanged;
            }

            chart.cv = CollectionViewSource.GetDefaultView(chart.ItemsSource) as CollectionView;
            if (chart.cv.Count == 0) return;
            chart.dataType = chart.cv.GetItemAt(0).GetType();
            chart.datas = Enumerable.Range(0, chart.cv.Count).Select(_ => new LabelData()).ToArray();

            chart.SetHorizontalDatas();
            chart.SetVerticalDatas();
            chart.SetLabelDatas();
            chart.Update();
        }

        static void OnHorizontalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as Annotator;
            if (chart == null) return;

            chart.SetHorizontalDatas();
            chart.Update();
        }

        void SetHorizontalDatas() {
            if (dataType == null || HorizontalPropertyName == null || datas == null) return;

            hPropertyInfo = dataType.GetProperty(HorizontalPropertyName);
            var hAxis = HorizontalAxis;
            foreach ((var obj, var idx) in cv.Cast<object>().Select(o => hPropertyInfo.GetValue(o)).WithIndex())
                datas[idx].x = hAxis.TranslateToAxisValue(obj);
        }

        static void OnVerticalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as Annotator;
            if (chart == null) return;

            chart.SetVerticalDatas();
            chart.Update();
        }

        void SetVerticalDatas() {
            if (dataType == null || VerticalPropertyName == null || datas == null) return;

            vPropertyInfo = dataType.GetProperty(VerticalPropertyName);
            var vAxis = VerticalAxis;
            foreach ((var obj, var idx) in cv.Cast<object>().Select(o => vPropertyInfo.GetValue(o)).WithIndex())
                datas[idx].y = vAxis.TranslateToAxisValue(obj);
        }

        static void OnLabelPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as Annotator;
            if (chart == null) return;

            chart.SetLabelDatas();
            chart.Update();
        }

        void SetLabelDatas() {
            if (dataType == null || datas == null) return;

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
        #endregion

        class LabelData {
            internal AxisValue x, y;
            internal string label;
        }
    }
}
