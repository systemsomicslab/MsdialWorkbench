using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Base;

namespace CompMs.Graphics.AxisManager
{
    public class CategoryAxisManager : CompMs.Graphics.Core.Base.AxisManager
    {
        #region DependencyProperty
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(IEnumerable), typeof(CategoryAxisManager),
            new PropertyMetadata(default(IEnumerable), OnItemsSourceChanged)
            );

        public static readonly DependencyProperty DisplayPropertyNameProperty = DependencyProperty.Register(
            nameof(DisplayPropertyName), typeof(string), typeof(CategoryAxisManager),
            new PropertyMetadata(default, OnDisplayPropertyNameChanged)
            );

        public static readonly DependencyProperty IdentityPropertyNameProperty = DependencyProperty.Register(
            nameof(IdentityPropertyName), typeof(string), typeof(CategoryAxisManager),
            new PropertyMetadata(default, OnIdentityPropertyNameChanged)
            );
        #endregion

        #region Property
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public string DisplayPropertyName
        {
            get => (string)GetValue(DisplayPropertyNameProperty);
            set => SetValue(DisplayPropertyNameProperty, value);
        }

        public string IdentityPropertyName
        {
            get => (string)GetValue(IdentityPropertyNameProperty);
            set => SetValue(IdentityPropertyNameProperty, value);
        }
        #endregion

        #region field
        private Dictionary<object, double> converter;
        private Type dataType;
        private PropertyInfo dPropertyReflection;
        private PropertyInfo iPropertyReflection;
        #endregion

        public override List<LabelTickData> GetLabelTicks()
        {
            var result = new List<LabelTickData>();

            if (ItemsSource == null) return result;

            Func<object, string> toLabel;
            if (dPropertyReflection != null)
                toLabel = o => dPropertyReflection.GetValue(o).ToString();
            else
                toLabel = o => o.ToString();

            Func<object, object> toKey;
            if (iPropertyReflection != null)
                toKey = o => iPropertyReflection.GetValue(o);
            else
                toKey = o => o;

            foreach(object item in ItemsSource)
            {
                result.Add(new LabelTickData()
                {
                    Label = toLabel(item),
                    TickType = TickType.LongTick,
                    Center = converter[toKey(item)],
                    Width = 1d,
                    Source = item,
                });
            }

            return result;
        }

        public override double ValueToRenderPosition(object value)
        {
            double min = Min, max = Max;
            bool isFlipped = IsFlipped;

            if (value is double d)
                return ValueToRenderPositionCore(d, min, max, isFlipped);
            else if (converter.ContainsKey(value))
                return base.ValueToRenderPositionCore(converter[value], min, max, isFlipped);
            else
                return double.NaN;
        }

        public override List<double> ValuesToRenderPositions(IEnumerable<object> values) {
            double min = Min, max = Max;
            bool isFlipped = IsFlipped;

            return values.Select(value =>
                converter.ContainsKey(value)
                    ? base.ValueToRenderPositionCore(converter[value], min, max, isFlipped)
                    : double.NaN
                ).ToList();
        }

        private void UpdateConverter()
        {
            if (ItemsSource == null) return;

            converter = new Dictionary<object, double>();

            var cnt = 0d;
            if (iPropertyReflection != null)
                foreach(object item in ItemsSource)
                    converter[iPropertyReflection.GetValue(item)] = 0.5 + cnt++;
            else
                foreach(object item in ItemsSource)
                    converter[item] = 0.5 + cnt++;

            InitialRange = new Range { Minimum = 0d, Maximum = cnt };

            AxisMapper = new AxisMapper(this);
        }

        #region Event handler
        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as CategoryAxisManager;
            if (axis == null) return;

            var enumerator = axis.ItemsSource.GetEnumerator();
            enumerator.MoveNext();
            axis.dataType = enumerator.Current.GetType();

            if (axis.DisplayPropertyName != null)
                axis.dPropertyReflection = axis.dataType.GetProperty(axis.DisplayPropertyName);
            if (axis.IdentityPropertyName != null)
                axis.iPropertyReflection = axis.dataType.GetProperty(axis.IdentityPropertyName);

            axis.UpdateConverter();
        }

        static void OnDisplayPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as CategoryAxisManager;
            if (axis == null) return;

            if (axis.dataType != null)
                axis.dPropertyReflection = axis.dataType.GetProperty(axis.DisplayPropertyName);
        }

        static void OnIdentityPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as CategoryAxisManager;
            if (axis == null) return;

            if (axis.dataType != null)
                axis.iPropertyReflection = axis.dataType.GetProperty(axis.IdentityPropertyName);

            axis.UpdateConverter();
        }
        #endregion
    }
}
