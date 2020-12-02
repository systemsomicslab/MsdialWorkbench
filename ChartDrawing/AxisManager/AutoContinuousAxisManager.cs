using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using CompMs.Graphics.Base;

namespace CompMs.Graphics.AxisManager
{
    public class AutoContinuousAxisManager : ContinuousAxisManager {
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
        public IEnumerable ItemsSource {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public string ValuePropertyName {
            get => (string)GetValue(ValuePropertyNameProperty);
            set => SetValue(ValuePropertyNameProperty, value);
        }
        #endregion

        #region field
        protected Type dataType;
        protected PropertyInfo vProp;
        #endregion

        void SetMinAndMaxValues() {
            if (ValuePropertyName == null || ItemsSource == null || dataType == null)
                return;

            vProp = dataType.GetProperty(ValuePropertyName);
            double min = double.MaxValue, max = double.MinValue;
            foreach (var o in ItemsSource) {
                if (o == null) continue;
                var v = Convert.ToDouble((IConvertible)vProp.GetValue(o));
                min = Math.Min(min, v);
                max = Math.Max(max, v);
            }
            MinValue = min;
            MaxValue = max;
        }

        protected virtual void SetAxisStates() {
            if (ItemsSource != null) {
                var enumerator = ItemsSource?.GetEnumerator();
                if (enumerator == null || !enumerator.MoveNext()) return;
                dataType = enumerator.Current.GetType();
            }
        }

        #region Event handler
        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var axis = d as AutoContinuousAxisManager;
            if (axis == null) return;

            if (e.NewValue is INotifyCollectionChanged collectionNew) {
                collectionNew.CollectionChanged += axis.OnItemsSourceCollectionChanged;
            }
            if (e.OldValue is INotifyCollectionChanged collectionOld) {
                collectionOld.CollectionChanged -= axis.OnItemsSourceCollectionChanged;
            }

            axis.SetAxisStates();
            axis.SetMinAndMaxValues();
        }

        private static void OnValuePropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var axis = d as AutoContinuousAxisManager;
            if (axis == null) return;

            axis.SetAxisStates();
            axis.SetMinAndMaxValues();
        }

        private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            SetAxisStates();
            SetMinAndMaxValues();
        }
        #endregion
    }
}
