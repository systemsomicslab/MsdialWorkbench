using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;

namespace CompMs.Graphics.AxisManager
{
    class LabelGeneratorBase
    {
        protected static double GetExponent(double x) {
            return Math.Floor(Math.Log10(x));
        }

        protected static decimal GetLongInterval(double delta) {
            var order = (decimal)GetOrder(delta);
            if ((decimal)delta / order > 2) {
                return order;
            }
            else {
                return order / 2;
            }
        }

        protected static double GetOrder(double x) {
            return Math.Pow(10, GetExponent(x));
        }

        protected static decimal GetShortInterval(double delta, decimal longInterval) {
            if ((decimal)delta / longInterval >= 5) {
                return longInterval * (decimal)0.5;
            }
            else if ((decimal)delta / longInterval >= 2) {
                return longInterval * (decimal)0.25;
            }
            else {
                return longInterval * (decimal)0.1;
            }
        }

        protected List<LabelTickData> GetRealTicks(decimal lo, decimal hi, decimal interval, Func<decimal, decimal, LabelTickData> create) {
            var result = new List<LabelTickData>();
            for (var i = lo / interval; i * interval <= hi; ++i) {
                result.Add(create(i, interval));
            }
            return result;
        }

        protected List<LabelTickData> GetIntTicks(decimal lo, decimal hi, decimal interval, Func<decimal, decimal, LabelTickData> create) {
            var result = new List<LabelTickData>();
            for (var i = Math.Ceiling(lo / interval); i * interval <= hi; ++i) {
                result.Add(create(i, interval));
            }
            return result;
        }

        protected List<LabelTickData> GetLongTicks(decimal lo, decimal hi, decimal interval, decimal factor, string format) {
            return GetIntTicks(lo, hi, interval,
                (i, interval_) => new LabelTickData()
                {
                    Label = (i * interval_).ToString(format),
                    TickType = TickType.LongTick,
                    Center = (double)(i * interval_ * factor),
                    Width = (double)(interval_ * factor),
                    Source = (double)(i * interval_ * factor),
                });
        }

        protected List<LabelTickData> GetShortTicks(decimal lo, decimal hi, decimal interval, decimal factor, string format) {
            return GetIntTicks(lo, hi, interval,
                (i, interval_) => new LabelTickData()
                {
                    Label = (i * interval_).ToString(format),
                    TickType = TickType.ShortTick,
                    Center = (double)(i * interval_ * factor),
                    Width = 0,
                    Source = (double)(i * interval_ * factor),
                });
        }

    }
}