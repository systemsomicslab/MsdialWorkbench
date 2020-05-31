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
        #endregion

        #region Property
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
        #endregion

        #region field
        private Dictionary<object, double> converter;
        #endregion

        public override List<LabelTickData> GetLabelTicks()
        {
            var result = new List<LabelTickData>();

            foreach(object item in ItemsSource)
            {
                result.Add(new LabelTickData()
                {
                    Label = item.ToString(),
                    TickType = TickType.LongTick,
                    Center = converter[item],
                    Width = 1d,
                });
            }

            return result;
        }

        public override double ValueToRenderPosition(object value) => ValueToRenderPosition(converter[value]);

        #region Event handler
        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as CategoryAxisManager;
            if (axis == null) return;

            axis.converter = new Dictionary<object, double>();

            var cnt = 0d;
            foreach(object item in axis.ItemsSource)
                axis.converter[item] = 0.5 + cnt++;
            axis.InitialMin = 0d;
            axis.InitialMax = cnt;
        }
        #endregion
    }
}
