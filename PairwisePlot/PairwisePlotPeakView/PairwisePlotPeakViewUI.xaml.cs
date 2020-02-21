using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv {
	/// <summary>
	/// PairwisePlotPeakViewUI.xaml の相互作用ロジック
	/// </summary>
	public partial class PairwisePlotPeakViewUI : UserControl
    {
        private PairwisePlotPeakViewFE pairwisePlotPeakViewFE;
        private PairwisePlotBean pairwisePlotBean;

      
        //Graph area format
        private double leftMargin = 50;
        private double topMargin = 30;
        private double rightMargin = 10;
        private double bottomMargin = 40;
        private double plotSize = 6;
        private double plotMargin = 6;
        private double triangleSize = 6;


        // Point for Mouse Event
        private Point currentMousePoint = new Point(-1, -1); // Current Location of Mouse Pointer
        private Point leftButtonStartClickPoint = new Point(-1, -1); // for Mouse Left Button        
        private Point leftButtonEndClickPoint = new Point(-1, -1); // for Mouse Left Button (this coordinates are changed by MouseMove)
        private Point rightButtonStartClickPoint = new Point(-1, -1); // Start Click Point of Mouse Right Button
        private Point rightButtonEndClickPoint = new Point(-1, -1); // End Click Point of Mouse Left Button (this coordinates are changed by MouseMove)

        // Graph Move Intial Values
        private float graphScrollInitialRtMin = -1; // Initial minXval for Graph Slide Event
        private float graphScrollInitialRtMax = -1; // Initial maxXval for Graph Slide Event
        private float graphScrollInitialIntensityMin = -1; // Initial minYval for Graph Slide Event
        private float graphScrollInitialIntensityMax = -1;

        public PairwisePlotPeakViewUI(PairwisePlotBean pairwisePlotBean)
        {
            InitializeComponent();
            this.pairwisePlotBean = pairwisePlotBean;
            this.pairwisePlotPeakViewFE = new PairwisePlotPeakViewFE(this.pairwisePlotBean, this);
            this.Content = this.pairwisePlotPeakViewFE;
        }

        public PairwisePlotPeakViewUI() : this(new PairwisePlotBean()) { }

        #region // properties
        public PairwisePlotPeakViewFE PairwisePlotPeakViewFE
        {
            get { return pairwisePlotPeakViewFE; }
            set { pairwisePlotPeakViewFE = value; }
        }

        public PairwisePlotBean PairwisePlotBean
        {
            get { return pairwisePlotBean; }
            set { pairwisePlotBean = value; }
        }

        public double LeftMargin
        {
            get { return leftMargin; }
            set { leftMargin = value; }
        }

        public double TopMargin
        {
            get { return topMargin; }
            set { topMargin = value; }
        }

        public double RightMargin
        {
            get { return rightMargin; }
            set { rightMargin = value; }
        }

        public double BottomMargin
        {
            get { return bottomMargin; }
            set { bottomMargin = value; }
        }

        public double PlotSize
        {
            get { return plotSize; }
            set { plotSize = value; }
        }

        public double TriangleSize
        {
            get { return triangleSize; }
            set { triangleSize = value; }
        }

        public double PlotMargin
        {
            get { return plotMargin; }
            set { plotMargin = value; }
        }

        public Point CurrentMousePoint
        {
            get { return currentMousePoint; }
            set { currentMousePoint = value; }
        }

        public Point LeftButtonStartClickPoint
        {
            get { return leftButtonStartClickPoint; }
            set { leftButtonStartClickPoint = value; }
        }

        public Point LeftButtonEndClickPoint
        {
            get { return leftButtonEndClickPoint; }
            set { leftButtonEndClickPoint = value; }
        }

        public Point RightButtonStartClickPoint
        {
            get { return rightButtonStartClickPoint; }
            set { rightButtonStartClickPoint = value; }
        }

        public Point RightButtonEndClickPoint
        {
            get { return rightButtonEndClickPoint; }
            set { rightButtonEndClickPoint = value; }
        }

        public float GraphScrollInitialRtMin
        {
            get { return graphScrollInitialRtMin; }
            set { graphScrollInitialRtMin = value; }
        }

        public float GraphScrollInitialRtMax
        {
            get { return graphScrollInitialRtMax; }
            set { graphScrollInitialRtMax = value; }
        }

        public float GraphScrollInitialIntensityMin
        {
            get { return graphScrollInitialIntensityMin; }
            set { graphScrollInitialIntensityMin = value; }
        }

        public float GraphScrollInitialIntensityMax
        {
            get { return graphScrollInitialIntensityMax; }
            set { graphScrollInitialIntensityMax = value; }
        }
        #endregion

        public void RefreshUI()
        {
            this.pairwisePlotPeakViewFE.PairwisePlotDraw();
		}

		private void userControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshUI();
        }

        private void userControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.pairwisePlotBean == null) return;

            // Store Current Mouse Point
            this.currentMousePoint = Mouse.GetPosition(this);

			if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.leftButtonEndClickPoint = Mouse.GetPosition(this);
                this.pairwisePlotPeakViewFE.GraphScroll();
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                if (this.rightButtonStartClickPoint.X != -1 && this.rightButtonStartClickPoint.Y != -1)
                {
                    this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                    this.pairwisePlotPeakViewFE.ZoomRubberDraw();
                }
                else
                {
                    this.rightButtonEndClickPoint = new Point(-1, -1);
                }
            }
            else
            {
                if (this.pairwisePlotBean.DisplayedSpotCount > 3000) return;
                RefreshUI();
            }
		}

		private void userControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
			if (this.pairwisePlotBean == null) return;
			this.rightButtonStartClickPoint = Mouse.GetPosition(this);
		}

		private void userControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
			this.rightButtonEndClickPoint = Mouse.GetPosition(this);

            if ((this.rightButtonStartClickPoint.X < this.leftMargin && this.rightButtonEndClickPoint.X < this.leftMargin)
                || (this.rightButtonStartClickPoint.X > this.ActualWidth - this.rightMargin && this.rightButtonEndClickPoint.X > this.ActualWidth - this.rightMargin)
                || (this.rightButtonStartClickPoint.Y < this.topMargin && this.rightButtonEndClickPoint.Y < this.topMargin)
                || (this.rightButtonStartClickPoint.Y > this.ActualHeight - this.bottomMargin && this.rightButtonEndClickPoint.Y > this.ActualHeight - this.leftMargin))
            {
                this.rightButtonStartClickPoint = new Point(-1, -1);
                this.rightButtonEndClickPoint = new Point(-1, -1);
                return;
            }

			this.pairwisePlotPeakViewFE.GraphZoom();
            RefreshUI();

            // Reset Mouse Position
            this.rightButtonStartClickPoint = new Point(-1, -1);
            this.rightButtonEndClickPoint = new Point(-1, -1);
		}

		private void userControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (this.pairwisePlotBean == null) return;
            this.rightButtonEndClickPoint.X = -1;
            RefreshUI();
		}

		private void userControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.pairwisePlotBean == null) return;
            if (e.StylusDevice != null) return; // Avoid Touch Event

            this.graphScrollInitialRtMin = (float)this.pairwisePlotBean.DisplayRangeMinX;
            this.graphScrollInitialRtMax = (float)this.pairwisePlotBean.DisplayRangeMaxX;
            this.graphScrollInitialIntensityMin = (float)this.pairwisePlotBean.DisplayRangeMinY;
            this.graphScrollInitialIntensityMax = (float)this.pairwisePlotBean.DisplayRangeMaxY;

            // Set Mouse Position
            this.leftButtonStartClickPoint = Mouse.GetPosition(this);
            this.currentMousePoint = Mouse.GetPosition(this);
		}

		private void userControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
			if (this.pairwisePlotBean == null) return;
           
            this.leftButtonEndClickPoint = Mouse.GetPosition(this);
            
            this.pairwisePlotPeakViewFE.PlotFocus();

            RefreshUI();

            // Reset Mouse Position
            this.leftButtonStartClickPoint = new Point(-1, -1);
            this.leftButtonEndClickPoint = new Point(-1, -1);
		}

		private void userControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.currentMousePoint = Mouse.GetPosition(this);
            Point xyValue = this.pairwisePlotPeakViewFE.GetDataPositionOnMousePoint(this.currentMousePoint);

            float newMinX = float.MaxValue;
            float newMaxX = float.MinValue;
            float newMinY = float.MaxValue;
            float newMaxY = float.MinValue;

            if (e.Delta > 0)
            {
                if (this.currentMousePoint.X > this.leftMargin) newMinX = (float)xyValue.X - (float)((xyValue.X - (float)this.pairwisePlotBean.DisplayRangeMinX) * 0.9); else newMinX = (float)this.pairwisePlotBean.DisplayRangeMinX;
                if (this.currentMousePoint.X > this.leftMargin) newMaxX = (float)xyValue.X + (float)(((float)this.pairwisePlotBean.DisplayRangeMaxX - xyValue.X) * 0.9); else newMaxX = (float)this.pairwisePlotBean.DisplayRangeMaxX;
                if (this.currentMousePoint.Y < this.ActualHeight - this.bottomMargin) newMinY = (float)xyValue.Y - (float)((xyValue.Y - (float)this.pairwisePlotBean.DisplayRangeMinY) * 0.9); else newMinY = (float)this.pairwisePlotBean.DisplayRangeMinY;
                if (this.currentMousePoint.Y < this.ActualHeight - this.bottomMargin) newMaxY = (float)xyValue.Y + (float)(((float)this.pairwisePlotBean.DisplayRangeMaxY - xyValue.Y) * 0.9); else newMaxY = (float)this.pairwisePlotBean.DisplayRangeMaxY;
            }
            else
            {
                if (this.currentMousePoint.X > this.leftMargin) newMinX = (float)xyValue.X - (float)((xyValue.X - (float)this.pairwisePlotBean.DisplayRangeMinX) * 1.1); else newMinX = (float)this.pairwisePlotBean.DisplayRangeMinX;
                if (this.currentMousePoint.X > this.leftMargin) newMaxX = (float)xyValue.X + (float)(((float)this.pairwisePlotBean.DisplayRangeMaxX - xyValue.X) * 1.1); else newMaxX = (float)this.pairwisePlotBean.DisplayRangeMaxX;
                if (this.currentMousePoint.Y < this.ActualHeight - this.bottomMargin) newMinY = (float)xyValue.Y - (float)((xyValue.Y - (float)this.pairwisePlotBean.DisplayRangeMinY) * 1.1); else newMinY = (float)this.pairwisePlotBean.DisplayRangeMinY;
                if (this.currentMousePoint.Y < this.ActualHeight - this.bottomMargin) newMaxY = (float)xyValue.Y + (float)(((float)this.pairwisePlotBean.DisplayRangeMaxY - xyValue.Y) * 1.1); else newMaxY = (float)this.pairwisePlotBean.DisplayRangeMaxY;
            }

            if (newMinX < this.pairwisePlotBean.MinX) this.pairwisePlotBean.DisplayRangeMinX = this.pairwisePlotBean.MinX; else this.pairwisePlotBean.DisplayRangeMinX = newMinX;
            if (newMinY < this.pairwisePlotBean.MinY) this.pairwisePlotBean.DisplayRangeMinY = this.pairwisePlotBean.MinY; else this.pairwisePlotBean.DisplayRangeMinY = newMinY;
            if (newMaxX > this.pairwisePlotBean.MaxX) this.pairwisePlotBean.DisplayRangeMaxX = this.pairwisePlotBean.MaxX; else this.pairwisePlotBean.DisplayRangeMaxX = newMaxX;
            if (newMaxY > this.pairwisePlotBean.MaxY) this.pairwisePlotBean.DisplayRangeMaxY = this.pairwisePlotBean.MaxY; else this.pairwisePlotBean.DisplayRangeMaxY = newMaxY;

            RefreshUI();
        }

        private void userControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.pairwisePlotBean == null) return;
            if (e.StylusDevice != null) return; // Avoid Touch Event

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.currentMousePoint = Mouse.GetPosition(this);

                if (this.currentMousePoint.X > this.leftMargin && this.currentMousePoint.Y > this.ActualHeight - this.bottomMargin)
                {
                    this.pairwisePlotBean.DisplayRangeMinX = this.pairwisePlotBean.MinX;
                    this.pairwisePlotBean.DisplayRangeMaxX = this.pairwisePlotBean.MaxX;
                    RefreshUI();
                }
                else if (this.currentMousePoint.X < this.leftMargin && this.currentMousePoint.Y < this.ActualHeight - this.bottomMargin)
                {
                    this.pairwisePlotBean.DisplayRangeMinY = this.pairwisePlotBean.MinY;
                    this.pairwisePlotBean.DisplayRangeMaxY = this.pairwisePlotBean.MaxY;
                    RefreshUI();
                }
                else if (this.currentMousePoint.X > this.leftMargin && this.currentMousePoint.Y > this.topMargin)
                {
                    this.pairwisePlotPeakViewFE.ResetGraphDisplayRange();
                }
            }
		}
	}
}
