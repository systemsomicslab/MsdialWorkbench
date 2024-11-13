using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;

namespace CompMs.Graphics.AxisManager.Generic
{
    public sealed class DefectAxisManager : BaseAxisManager<double>
    {
        private static readonly AxisRange DEFECT_RANGE = new AxisRange(-.5, .5);

        public DefectAxisManager(double divisor) : base(DEFECT_RANGE) {
            Divisor = divisor;
        }

        public DefectAxisManager(double divisor, AxisRange bounds) : base(DEFECT_RANGE, bounds) {
            Divisor = divisor;
        }
        
        public DefectAxisManager(double divisor, IChartMargin margin) : base(DEFECT_RANGE, margin) {
            Divisor = divisor;
        }

        public DefectAxisManager(double divisor, IChartMargin margin, AxisRange bounds) : base(DEFECT_RANGE, margin, bounds) {
            Divisor = divisor;
        }

        public DefectAxisManager(double divisor, double factor) : base((DEFECT_RANGE + new AxisValue(.5d)) * factor) {
            Divisor = divisor;
            Factor = factor;
        }

        public DefectAxisManager(double divisor, double factor, AxisRange bounds) : base((DEFECT_RANGE + new AxisValue(.5d)) * factor, bounds) {
            Divisor = divisor;
            Factor = factor;
        }

        public DefectAxisManager(double divisor, double factor, IChartMargin margin) : base((DEFECT_RANGE + new AxisValue(.5d)) * factor, margin) {
            Divisor = divisor;
            Factor = factor;
        }

        public DefectAxisManager(double divisor, double factor, IChartMargin margin, AxisRange bounds) : base((DEFECT_RANGE + new AxisValue(.5d)) * factor, margin, bounds) {
            Divisor = divisor;
            Factor = factor;
        }

        public LabelType LabelType {
            get => _labelType;
            set => SetProperty(ref _labelType, value);
        }
        private LabelType _labelType = LabelType.Standard;

        private ILabelGenerator LabelGenerator {
            get {
                switch (LabelType) {
                    case LabelType.Order:
                        return _labelGenerator is OrderLabelGenerator
                            ? _labelGenerator
                            : _labelGenerator = new OrderLabelGenerator();
                    case LabelType.Relative:
                        return _labelGenerator is RelativeLabelGenerator
                            ? _labelGenerator
                            : _labelGenerator = new RelativeLabelGenerator();
                    case LabelType.Percent:
                        return _labelGenerator is PercentLabelGenerator
                            ? _labelGenerator
                            : _labelGenerator = new PercentLabelGenerator();
                    case LabelType.Standard:
                    default:
                        return _labelGenerator is StandardLabelGenerator
                            ? _labelGenerator
                            : _labelGenerator = new StandardLabelGenerator();
                }
            }
        }
        private ILabelGenerator _labelGenerator;

        public double Divisor {
            get => _divisor;
            set {
                if (SetProperty(ref _divisor, value)) {
                    OnAxisValueMappingChanged();
                }
            }
        }
        private double _divisor;

        public double Factor {
            get => _factor;
            set {
                if (SetProperty(ref _factor, value)) {
                    OnAxisValueMappingChanged();
                    UpdateInitialRange((DEFECT_RANGE + .5d) * Factor);
                }
            }
        }
        private double _factor = 0d;

        protected override void OnRangeChanged() {
            labelTicks = null;
            base.OnRangeChanged();
        }

        public override List<LabelTickData> GetLabelTicks() {
            var generator = LabelGenerator;
            var initialRangeCore = CoerceRange(InitialRangeCore, Bounds);
            List<LabelTickData> ticks;
            (ticks, UnitLabel) = generator.Generate(Range.Minimum.Value, Range.Maximum.Value, initialRangeCore.Minimum.Value, initialRangeCore.Maximum.Value);
            return ticks;
        }

        public override AxisValue TranslateToAxisValue(double value) {
            if (Factor != 0d) {
                return new AxisValue((value / Divisor - Math.Floor(value / Divisor)) * Factor);
            }
            else {
                return new AxisValue(value / Divisor - Math.Round(value / Divisor));
            }
        }
    }
}
