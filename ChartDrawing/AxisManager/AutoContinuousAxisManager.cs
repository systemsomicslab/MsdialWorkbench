using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using CompMs.Graphics.Base;

namespace CompMs.Graphics.AxisManager
{
    public class AutoContinuousAxisManager : ContinuousAxisManager
    {
        #region DependencyProperty
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(IEnumerable), typeof(AutoContinuousAxisManager),
            new PropertyMetadata(null, OnItemsSourceChanged)
            );

        public static readonly DependencyProperty ValuePropertyNameProperty = DependencyProperty.Register(
            nameof(ValuePropertyName), typeof(string), typeof(AutoContinuousAxisManager),
            new PropertyMetadata(null, OnValuePropertyNameChanged)
            );
        #endregion

        #region Property
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public string ValuePropertyName
        {
            get => (string)GetValue(ValuePropertyNameProperty);
            set => SetValue(ValuePropertyNameProperty, value);
        }
        #endregion

        #region field
        private Type dataType;
        #endregion

        void SetMinAndMaxValues() {
            if (ValuePropertyName == null || ItemsSource == null || dataType == null)
                return;

            var propInfo = dataType.GetProperty(ValuePropertyName);
            double min = double.MaxValue, max = double.MinValue;
            foreach (var o in ItemsSource) {
                var v = Convert.ToDouble((IConvertible)propInfo.GetValue(o));
                min = Math.Min(min, v);
                max = Math.Max(max, v);
            }
            MinValue = min;
            MaxValue = max;
        }

        #region Event handler
        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var axis = d as AutoContinuousAxisManager;
            if (axis == null) return;

            var enumerator = axis.ItemsSource?.GetEnumerator();
            if (enumerator == null || !enumerator.MoveNext()) return;
            axis.dataType = enumerator.Current.GetType();

            axis.SetMinAndMaxValues();
        }

        private static void OnValuePropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var axis = d as AutoContinuousAxisManager;
            if (axis == null) return;

            axis.SetMinAndMaxValues();
        }
        #endregion
    }
}
