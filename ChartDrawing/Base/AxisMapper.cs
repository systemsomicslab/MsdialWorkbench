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

        public double ValueToRenderPosition(object value) {
            return manager.TranslateToRenderPoint(value);
        }
        public AxisValue RenderPositionToValue(double value) {
            return manager.TranslateFromRenderPoint(value);
        }
        public List<double> ValuesToRenderPositions(IEnumerable<object> values) {
            return manager.TranslateToRenderPoints(values);
        }
    }
}
