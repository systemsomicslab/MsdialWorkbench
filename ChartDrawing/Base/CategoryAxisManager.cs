using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Base
{
    public class CategoryAxisManager : AxisManager
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

        public static readonly DependencyProperty AxisFunctionProperty = DependencyProperty.Register(
            nameof(AxisFunction), typeof(AxisManagerFunction), typeof(CategoryAxisManager),
            new PropertyMetadata(default)
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

        public AxisManagerFunction AxisFunction
        {
            get => (AxisManagerFunction)GetValue(AxisFunctionProperty);
            set => SetValue(AxisFunctionProperty, value);
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

        private double ValueToRenderPositionCore(object value) => base.ValueToRenderPosition(converter[value]);

        public override double ValueToRenderPosition(object value) => ValueToRenderPositionCore(value);
        public override double ValueToRenderPosition(IConvertible value) => ValueToRenderPositionCore(value);
        public override double ValueToRenderPosition(double value)
        {
            Console.WriteLine("called double -> double");
            return base.ValueToRenderPosition(value);
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

            InitialMin = 0d;
            InitialMax = cnt;
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
            axis.AxisFunction = new AxisManagerFunction(axis);
        }

        static void OnDisplayPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as CategoryAxisManager;
            if (axis == null) return;

            if (axis.dataType != null)
                axis.dPropertyReflection = axis.dataType.GetProperty(axis.DisplayPropertyName);

            axis.AxisFunction = new AxisManagerFunction(axis);
        }

        static void OnIdentityPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as CategoryAxisManager;
            if (axis == null) return;

            if (axis.dataType != null)
                axis.iPropertyReflection = axis.dataType.GetProperty(axis.IdentityPropertyName);

            axis.UpdateConverter();
            axis.AxisFunction = new AxisManagerFunction(axis);
        }
        #endregion

    }
    public class AxisManagerFunction : AxisManager
    {
        private AxisManager parent;

        public AxisManagerFunction(AxisManager axis)
        {
            parent = axis;
        }
        public override List<LabelTickData> GetLabelTicks() => parent.GetLabelTicks();
        public override double ValueToRenderPosition(object value) => parent.ValueToRenderPosition(value);
        public override double ValueToRenderPosition(IConvertible value) => parent.ValueToRenderPosition(value); 
        public override double ValueToRenderPosition(double value) => parent.ValueToRenderPosition(value);
    }
}
