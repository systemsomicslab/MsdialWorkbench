using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.Graphics.Base
{
    public class AxisMapper
    {
        public double InitialMin => manager.InitialRange.Minimum;
        public double InitialMax => manager.InitialRange.Maximum;

        private CompMs.Graphics.Core.Base.AxisManager manager;

        public AxisMapper(CompMs.Graphics.Core.Base.AxisManager manager_) {
            manager = manager_;
        }

        public AxisValue TranslateToAxisValue(object value) {
            return manager.TranslateToAxisValue(value);
        }

        public double TranslateToRenderPoint(AxisValue value, bool isFlipped) {
            return manager.TranslateToRenderPoint(value, isFlipped);
        }

        public double TranslateToRenderPoint(object value, bool isFlipped) {
            return manager.TranslateToRenderPoint(value, isFlipped);
        }

        public AxisValue TranslateFromRenderPoint(double value, bool isFlipped) {
            return manager.TranslateFromRenderPoint(value, isFlipped);
        }

        public List<double> TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped) {
            return manager.TranslateToRenderPoints(values, isFlipped);
        }
    }
}
