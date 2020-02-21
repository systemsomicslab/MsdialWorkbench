using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Msdial.Dendrogram
{
    /// <summary>
    /// Interaction logic for DendrogramPlotUI.xaml
    /// </summary>
    public partial class DendrogramPlotUI : UserControl
    {
        #region // Properties
        public DendrogramPlotBean DendrogramPlotBean { get; set; }
        public DendrogramPlotFE DendrogramPlotFE { get; set; }

        public Point LeftButtonStartClickPoint { get; private set; } = new Point(-1, -1);
        public Point LeftButtonEndClickPoint { get; private set; } = new Point(-1, -1);
        public Point RightButtonStartClickPoint { get; private set; } = new Point(-1, -1);
        public Point RightButtonEndClickPoint { get; private set; } = new Point(-1, -1);
        public Point CurrentMousePoint { get; private set; } = new Point(-1, -1);

        // Graph area format
        public double LeftMargin { get; set; } = 50;
        public double RightMargin { get; set; } = 10;
        public double TopMargin { get; set; } = 30;
        public double BottomMargin { get; set; } = 40;
        #endregion

        public DendrogramPlotUI(DendrogramPlotBean dendrogramPlotBean)
        {
            InitializeComponent();
            this.DendrogramPlotBean = dendrogramPlotBean;
            this.DendrogramPlotFE = new DendrogramPlotFE(this.DendrogramPlotBean, this);
            this.Content = this.DendrogramPlotFE;
        }
        public DendrogramPlotUI()
            :this(new DendrogramPlotBean()) { }

        private bool insideGraphArea(Point p)
        {
            var width = this.ActualWidth;
            var height = this.ActualHeight;

            if (p.X < this.LeftMargin || width - this.RightMargin < p.X ||
                p.Y < this.TopMargin || height - this.BottomMargin < p.Y )
                return false;
            return true;

        }

        public void RefreshUI()
        {
            this.DendrogramPlotFE.DendrogramPlotDraw();
        }

        private void userControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshUI();
        }

        private void userControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.DendrogramPlotBean == null) return;
            if (e.StylusDevice != null) return; // Avoid Touch Event
            this.LeftButtonStartClickPoint = Mouse.GetPosition(this);
        }

        private void userControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.DendrogramPlotBean == null) return;
            this.LeftButtonEndClickPoint = Mouse.GetPosition(this);
            RefreshUI();

            // Reset Mouse Position
            this.LeftButtonStartClickPoint = new Point(-1, -1);
            this.LeftButtonEndClickPoint = new Point(-1, -1);
        }

        private void userControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.DendrogramPlotBean == null) return;
            this.RightButtonStartClickPoint = Mouse.GetPosition(this);
        }

        private void userControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.DendrogramPlotBean == null) return;
            this.RightButtonEndClickPoint = Mouse.GetPosition(this);
            this.DendrogramPlotFE.UpdateGraphRange(this.RightButtonStartClickPoint, this.RightButtonEndClickPoint);
            RefreshUI();

            // Reset Mouse Position
            this.RightButtonStartClickPoint = new Point(-1, -1);
            this.RightButtonEndClickPoint = new Point(-1, -1);
        }

        private void userControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (this.DendrogramPlotBean == null) return;
            this.CurrentMousePoint = Mouse.GetPosition(this);

            if (this.insideGraphArea(this.CurrentMousePoint))
                this.DendrogramPlotFE.ZoomGraph(this.CurrentMousePoint, e.Delta);

            RefreshUI();

            this.CurrentMousePoint = new Point(-1, -1);
        }

        private void userControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.DendrogramPlotBean == null) return;

            var current = Mouse.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if(insideGraphArea(current) && insideGraphArea(this.CurrentMousePoint))
                {
                    this.DendrogramPlotFE.MoveGraph(this.CurrentMousePoint - current);
                    RefreshUI();
                }
            }
            else if(e.RightButton == MouseButtonState.Pressed)
            {
                if(insideGraphArea(current) && insideGraphArea(this.RightButtonStartClickPoint))
                {
                    this.DendrogramPlotFE.DrawZoomRubber(this.RightButtonStartClickPoint, current);
                }
            }
            else
            {
                RefreshUI();
            }
            this.CurrentMousePoint = current;
        }

        private void userControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.DendrogramPlotBean == null) return;
            this.DendrogramPlotBean.SelectedIdx = -1;
            RefreshUI();
        }
    }
}
