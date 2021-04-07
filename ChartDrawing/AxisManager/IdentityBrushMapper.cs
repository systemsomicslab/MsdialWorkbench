using CompMs.Graphics.Base;
using System;
using System.Windows.Media;

namespace CompMs.Graphics.AxisManager
{
    public class IdentityBrushMapper : BrushMapper<Brush>
    {
        public override Brush Map(Brush key) {
            return key;
        }
    }
}
