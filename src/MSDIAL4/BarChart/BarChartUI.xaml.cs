using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Common.BarChart
{
    /// <summary>
    /// Interaction logic for BarChartUI.xaml
    /// </summary>
    public partial class BarChartUI : UserControl
    {
        private BarChartBean barChartBean;
        private BarChartFE barChartFE;

        //Graph area format
        private double leftMargin = 40;
        private double topMargin = 20; 
        private double rightMargin = 10;
        private double bottomMargin = 40;
        private double topMarginForLabel = 20;
        private double barMargin = 10;
       
        // Point for Mouse Event
        private Point currentMousePoint = new Point(-1, -1); // Current Location of Mouse Pointer
        private Point leftButtonStartClickPoint = new Point(-1, -1); // for Mouse Left Button        
        private Point leftButtonEndClickPoint = new Point(-1, -1); // for Mouse Left Button (this coordinates are changed by MouseMove)
        private Point rightButtonStartClickPoint = new Point(-1, -1); // Start Click Point of Mouse Right Button
        private Point rightButtonEndClickPoint = new Point(-1, -1); // End Click Point of Mouse Left Button (this coordinates are changed by MouseMove)

        private bool leftMouseButtonLeftEdgeCapture = false;
        private bool leftMouseButtonRightEdgeCapture = false;
        private bool rightMouseButtonChromatogramUpDownZoom = false;
        private bool rightMouseButtonChromatogramLeftRightZoom = false;
        private bool rightMouseButtonChromatogramRectangleZoom = false;
        private bool keyDownCheck = false;

        // Graph Move Intial Values
        private float graphScrollInitialMassMin = -1; // Initial minXval for Graph Slide Event
        private float graphScrollInitialMassMax = -1; // Initial maxXval for Graph Slide Event
        private float graphScrollInitialIntensityMin = -1; // Initial minYval for Graph Slide Event
        private float graphScrollInitialIntensityMax = -1; // Initial maxYval for Graph Slide Event


        public BarChartUI(BarChartBean barChartBean)
        {
            InitializeComponent();
            this.barChartBean = barChartBean;
            InitializeMargins();

            this.barChartFE = new BarChartFE(this.barChartBean, this);
            this.Content = this.barChartFE;
        }

        private void InitializeMargins() {
            if (this.barChartBean.XAxisTitle == string.Empty) {
                bottomMargin = 10;
            }

            if (this.barChartBean.YAxisTitle == string.Empty) {
                leftMargin = 30;
            }
        }

        public BarChartUI()
        {
            InitializeComponent();
            this.barChartBean = null;
            this.barChartFE = new BarChartFE(this.barChartBean, this);
            this.Content = this.barChartFE;
        }

        public void RefreshUI()
        {
            this.barChartFE.BarChartDraw();
        }

        public DrawingVisual GetBarChartUiDrawingVisual()
        {
            return this.barChartFE.GetBarChartDrawingVisual(this.Width, this.Height);
        }

        private void userControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshUI();
        }

        private void userControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.barChartBean == null) return;

            // Set Mouse Position
            this.rightButtonStartClickPoint = Mouse.GetPosition(this);

            if (this.rightButtonStartClickPoint.X <= this.leftMargin && this.rightButtonStartClickPoint.Y > this.topMargin && this.rightButtonStartClickPoint.Y < this.ActualHeight - this.bottomMargin) this.rightMouseButtonChromatogramUpDownZoom = true;
            else if (this.rightButtonStartClickPoint.X > this.leftMargin && this.rightButtonStartClickPoint.Y >= this.ActualHeight - this.bottomMargin) this.rightMouseButtonChromatogramLeftRightZoom = true;
            else this.rightMouseButtonChromatogramRectangleZoom = true;
        }

        private void userControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.barChartBean == null) return;

            // Store Current Mouse Point
            this.currentMousePoint = Mouse.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed) {
                this.leftButtonEndClickPoint = Mouse.GetPosition(this);
            }
            else if (e.RightButton == MouseButtonState.Pressed) {
                if (this.rightButtonStartClickPoint.X != -1 && this.rightButtonStartClickPoint.Y != -1) {

                    if (this.rightMouseButtonChromatogramRectangleZoom) {
                        this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                        this.barChartFE.ZoomRubberDraw();
                    }
                }
                else {
                    this.rightButtonEndClickPoint = new Point(-1, -1);
                }
            }
            else {
                RefreshUI();
            }
        }

        private void userControl_MouseLeave(object sender, MouseEventArgs e)
        {

        }

        private void userControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.barChartBean == null) return;

            if (this.rightMouseButtonChromatogramRectangleZoom) {
                this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                this.barChartFE.GraphZoom();
            }

            // Reset Mouse Position
            this.rightButtonStartClickPoint = new Point(-1, -1);
            this.rightButtonEndClickPoint = new Point(-1, -1);
            this.rightMouseButtonChromatogramLeftRightZoom = false;
            this.rightMouseButtonChromatogramRectangleZoom = false;
            this.rightMouseButtonChromatogramUpDownZoom = false;
        }
       
        private void userControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.barChartBean == null) return;
            if (e.StylusDevice != null) return; // Avoid Touch Event

            if (e.LeftButton == MouseButtonState.Pressed) {
                this.barChartFE.ResetGraphDisplayRange();
            }
        }

        private void userControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void userControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void userControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (this.barChartBean == null) return;

            this.currentMousePoint = Mouse.GetPosition(this);

            var clickPointX = this.currentMousePoint.X;

            var currentBeginID = this.barChartBean.DisplayedBeginID;
            var currentEndID = this.barChartBean.DisplayedEndID;
            var newBeginID = currentBeginID;
            var newEndID = currentEndID;

            if (currentEndID - currentBeginID <= 7) return;
    
            var currentPercent = (clickPointX - this.LeftMargin) / (this.ActualWidth - this.RightMargin - this.LeftMargin);
            var currentPointID = currentBeginID + (int)(this.barChartBean.DisplayedBarElements.Count * currentPercent);

            if (e.Delta > 0) {
                //newBeginID = currentBeginID + 1;
                newBeginID = currentPointID - (int)Math.Max((currentPointID - currentBeginID) * 0.9, 3);

                if (newBeginID > currentPointID - 3) newBeginID = currentPointID - 3;
                if (newBeginID < 0) newBeginID = 0;

                //newEndID = currentEndID - 1;

                newEndID = currentPointID + (int)Math.Max((currentEndID - currentPointID) * 0.9, 3);
                if (newEndID < currentPointID + 3) newEndID = currentPointID + 3;
                if (newEndID > this.barChartBean.BarElements.Count - 1) newEndID = this.barChartBean.BarElements.Count - 1;
            }
            else {

                newBeginID = currentBeginID - 1;
                if (newBeginID < 0) newBeginID = 0;

                newEndID = currentEndID + 1;
                if (newEndID > this.barChartBean.BarElements.Count - 1) newEndID = this.barChartBean.BarElements.Count - 1;

            }


            this.barChartBean.DisplayElementRearrangement(newBeginID, newEndID);
            this.barChartFE.BarChartDraw();
        }

            

        #region // property

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

        public double TopMarginForLabel
        {
            get { return topMarginForLabel; }
            set { topMarginForLabel = value; }
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

        public double BarMargin
        {
            get { return barMargin; }
            set { barMargin = value; }
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

        public bool LeftMouseButtonLeftEdgeCapture
        {
            get { return leftMouseButtonLeftEdgeCapture; }
            set { leftMouseButtonLeftEdgeCapture = value; }
        }

        public bool LeftMouseButtonRightEdgeCapture
        {
            get { return leftMouseButtonRightEdgeCapture; }
            set { leftMouseButtonRightEdgeCapture = value; }
        }

        public bool RightMouseButtonChromatogramUpDownZoom
        {
            get { return rightMouseButtonChromatogramUpDownZoom; }
            set { rightMouseButtonChromatogramUpDownZoom = value; }
        }

        public bool RightMouseButtonChromatogramLeftRightZoom
        {
            get { return rightMouseButtonChromatogramLeftRightZoom; }
            set { rightMouseButtonChromatogramLeftRightZoom = value; }
        }

        public bool RightMouseButtonChromatogramRectangleZoom
        {
            get { return rightMouseButtonChromatogramRectangleZoom; }
            set { rightMouseButtonChromatogramRectangleZoom = value; }
        }

        public bool KeyDownCheck
        {
            get { return keyDownCheck; }
            set { keyDownCheck = value; }
        }

        public float GraphScrollInitialMassMin
        {
            get { return graphScrollInitialMassMin; }
            set { graphScrollInitialMassMin = value; }
        }

        public float GraphScrollInitialMassMax
        {
            get { return graphScrollInitialMassMax; }
            set { graphScrollInitialMassMax = value; }
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




    }
}
