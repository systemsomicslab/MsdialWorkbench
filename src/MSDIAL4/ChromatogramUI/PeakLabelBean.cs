using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public class PeakLabelBean
    {
        private string label;
        private double labelCoordinateX;
        private double labelCoordinateY;
        private SolidColorBrush labelBrush;

        public PeakLabelBean() { }
        public PeakLabelBean(string label, double labelCoordinateX, double labelCoordinateY, SolidColorBrush labelBrush)
        {
            this.label = label;
            this.labelCoordinateX = labelCoordinateX;
            this.labelCoordinateY = labelCoordinateY;
            this.labelBrush = labelBrush;
        }

        #region // Properties
        public string Label
        {
            get { return label; }
            set { label = value; }
        }

        public double LabelCoordinateX
        {
            get { return labelCoordinateX; }
            set { labelCoordinateX = value; }
        }

        public double LabelCoordinateY
        {
            get { return labelCoordinateY; }
            set { labelCoordinateY = value; }
        }

        public SolidColorBrush LabelBrush
        {
            get { return labelBrush; }
            set { labelBrush = value; }
        }
        #endregion
    }
}
