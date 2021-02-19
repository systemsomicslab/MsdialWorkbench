using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;

using CompMs.Common.DataStructure;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.AxisManager
{
    public class DependencyContinuousAxisManager : AutoContinuousAxisManager
    {
        #region DependencyProperty
        public static readonly DependencyProperty TargetAxisMapperProperty = DependencyProperty.Register(
            nameof(TargetAxisMapper), typeof(AxisMapper), typeof(DependencyContinuousAxisManager),
            new PropertyMetadata(null, OnTargetAxisMapperChanged)
            );

        public static readonly DependencyProperty TargetPropertyNameProperty = DependencyProperty.Register(
            nameof(TargetPropertyName), typeof(string), typeof(DependencyContinuousAxisManager),
            new PropertyMetadata(null, OnTargetPropertyNameChanged)
            );

        public static readonly DependencyProperty TargetRangeProperty = DependencyProperty.Register(
            nameof(TargetRange), typeof(Range), typeof(DependencyContinuousAxisManager),
            new PropertyMetadata(new Range(0, 0), OnTargetRangeChanged)
            );
        #endregion

        #region Property
        public AxisMapper TargetAxisMapper {
            get => (AxisMapper)GetValue(TargetAxisMapperProperty);
            set => SetValue(TargetAxisMapperProperty, value);
        }

        public string TargetPropertyName {
            get => (string)GetValue(TargetPropertyNameProperty);
            set => SetValue(TargetPropertyNameProperty, value);
        }

        public Range TargetRange {
            get => (Range)GetValue(TargetRangeProperty);
            set => SetValue(TargetRangeProperty, value);
        }
        #endregion

        #region field
        SegmentTree<AxisValue> minTree, maxTree;
        List<AxisValue> targets;
        PropertyInfo tProp;
        #endregion

        private void BuildTree() {
            if (ItemsSource == null || tProp == null || vProp == null || AxisMapper == null || TargetAxisMapper == null) {
                minTree = null; maxTree = null; targets = null;
                return;
            }
            var vMapper = AxisMapper;
            var tMapper = TargetAxisMapper;

            var tmps = new List<(AxisValue target, AxisValue value)>();
            foreach (var item in ItemsSource) {
                var v = vProp.GetValue(item);
                var t = tProp.GetValue(item);
                tmps.Add((tMapper.TranslateToAxisValue(t), vMapper.TranslateToAxisValue(v)));
            }
            tmps.Sort();

            targets = tmps.Select(tmp => tmp.target).ToList();
            minTree = new SegmentTree<AxisValue>(targets.Count, new AxisValue(double.MaxValue), (u, v) => Math.Min(u.Value, v.Value));
            maxTree = new SegmentTree<AxisValue>(targets.Count, new AxisValue(double.MinValue), (u, v) => Math.Max(u.Value, v.Value));
            foreach ((var tmp, var idx) in tmps.WithIndex()) {
                minTree.Set(idx, tmp.value);
                maxTree.Set(idx, tmp.value);
            }
            minTree.Build();
            maxTree.Build();
        }

        protected override void SetAxisStates() {
            base.SetAxisStates();

            if (dataType != null && ValuePropertyName != null)
                vProp = dataType.GetProperty(ValuePropertyName);
            if (dataType != null && TargetPropertyName != null)
                tProp = dataType.GetProperty(TargetPropertyName);
            BuildTree();
        }

        void UpdateRange() {
            if (targets == null || TargetRange == null || minTree == null || maxTree == null) return;
            var range = TargetRange;
            var minidx = Math.Max(SearchCollection.UpperBound(targets, range.Minimum, (u, v) => u.Value.CompareTo(v.Value)) - 1, 0);
            var maxidx = SearchCollection.UpperBound(targets, range.Maximum, (u, v) => u.Value.CompareTo(v.Value));

            Range = new Range(minTree.Query(minidx, maxidx), maxTree.Query(minidx, maxidx));
        }

        #region event handler
        public static void OnTargetAxisMapperChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is DependencyContinuousAxisManager axis) {
                axis.SetAxisStates();
            }
        }

        public static void OnTargetPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is DependencyContinuousAxisManager axis) {
                axis.SetAxisStates();
            }
        }
        public static void OnTargetRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is DependencyContinuousAxisManager axis) {
                axis.SetAxisStates();
                axis.UpdateRange();
            }
        }
        #endregion

        protected override Freezable CreateInstanceCore()
        {
            return new DependencyContinuousAxisManager();
        }
    }
}
