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

namespace Riken.Metabolomics.Pathwaymap {
    /// <summary>
    /// Interaction logic for PathwayMapUI.xaml
    /// </summary>
    public partial class PathwayMapUI : UserControl
    {
        public PathwayMapUI()
        {
            InitializeComponent();
            this.pathwayFE = new PathwayMapFE(null, this);
            this.Content = this.pathwayFE;
        }

        public PathwayMapUI(PathwayMapObj pathwayObj) {
            InitializeComponent();
            this.pathwayObj = pathwayObj;
            this.pathwayFE = new PathwayMapFE(this.pathwayObj, this);
            this.Content = this.pathwayFE;
        }

        private PathwayMapFE pathwayFE;
        public PathwayMapObj pathwayObj;

        //Graph area format
        private double leftMargin = 10;
        private double topMargin = 10;
        private double rightMargin = 10;
        private double bottomMargin = 10;
        private double plotSize = 6;
        private double plotMargin = 6;

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

        #region // properties
        public double LeftMargin {
            get { return leftMargin; }
            set { leftMargin = value; }
        }

        public double TopMargin {
            get { return topMargin; }
            set { topMargin = value; }
        }

        public double RightMargin {
            get { return rightMargin; }
            set { rightMargin = value; }
        }

        public double BottomMargin {
            get { return bottomMargin; }
            set { bottomMargin = value; }
        }

        public double PlotSize {
            get { return plotSize; }
            set { plotSize = value; }
        }

        public double PlotMargin {
            get { return plotMargin; }
            set { plotMargin = value; }
        }

        public Point CurrentMousePoint {
            get { return currentMousePoint; }
            set { currentMousePoint = value; }
        }

        public Point LeftButtonStartClickPoint {
            get { return leftButtonStartClickPoint; }
            set { leftButtonStartClickPoint = value; }
        }

        public Point LeftButtonEndClickPoint {
            get { return leftButtonEndClickPoint; }
            set { leftButtonEndClickPoint = value; }
        }

        public Point RightButtonStartClickPoint {
            get { return rightButtonStartClickPoint; }
            set { rightButtonStartClickPoint = value; }
        }

        public Point RightButtonEndClickPoint {
            get { return rightButtonEndClickPoint; }
            set { rightButtonEndClickPoint = value; }
        }

        public float GraphScrollInitialRtMin {
            get { return graphScrollInitialRtMin; }
            set { graphScrollInitialRtMin = value; }
        }

        public float GraphScrollInitialRtMax {
            get { return graphScrollInitialRtMax; }
            set { graphScrollInitialRtMax = value; }
        }

        public float GraphScrollInitialIntensityMin {
            get { return graphScrollInitialIntensityMin; }
            set { graphScrollInitialIntensityMin = value; }
        }

        public float GraphScrollInitialIntensityMax {
            get { return graphScrollInitialIntensityMax; }
            set { graphScrollInitialIntensityMax = value; }
        }
        #endregion

        public void RefreshUI() {
            this.pathwayFE.PathwayMapDraw();
        }

        private void userControl_SizeChanged(object sender, SizeChangedEventArgs e) {
            RefreshUI();
        }

        private void userControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            if (this.pathwayObj == null) return;
            this.rightButtonStartClickPoint = Mouse.GetPosition(this);
        }

        private void userControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (this.pathwayFE == null) return;
            if (this.pathwayObj == null) return;
            if (e.StylusDevice != null) return; // Avoid Touch Event

            // Set Mouse Position
            this.leftButtonStartClickPoint = Mouse.GetPosition(this);
            this.currentMousePoint = Mouse.GetPosition(this);

            this.graphScrollInitialRtMin = (float)this.pathwayObj.DisplayRangeMinX;
            this.graphScrollInitialRtMax = (float)this.pathwayObj.DisplayRangeMaxX;
            this.graphScrollInitialIntensityMin = (float)this.pathwayObj.DisplayRangeMinY;
            this.graphScrollInitialIntensityMax = (float)this.pathwayObj.DisplayRangeMaxY;

            this.pathwayFE.IsLeftClicked = true;
            this.pathwayFE.IsLeftReleased = false;
            if (Keyboard.Modifiers == ModifierKeys.Control) {
                this.pathwayFE.IsControlHold = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift) {
                this.pathwayFE.IsShiftHold = true;
            }
            else {
                this.pathwayFE.IsControlHold = false;
                this.pathwayFE.IsShiftHold = false;
            }
            RefreshUI();
        }

        private void userControl_MouseMove(object sender, MouseEventArgs e) {
            if (this.pathwayObj == null) return;
            if (this.pathwayFE == null) return;
            if (this.pathwayObj.Nodes == null) return;

            // Store Current Mouse Point
            this.currentMousePoint = Mouse.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed) {
                this.leftButtonEndClickPoint = Mouse.GetPosition(this);

                var selectedNodeCount = this.pathwayObj.Nodes.Count(n => n.IsSelected);
                if (selectedNodeCount > 0 && !this.pathwayFE.IsShiftHold && !this.pathwayFE.IsControlHold) {
                    RefreshUI();
                }
                else if (this.pathwayFE.IsShiftHold) {
                    this.pathwayFE.ZoomShiftedRubberDraw();
                }
                else {
                    this.pathwayFE.GraphScroll();
                }
            }
            else if (e.RightButton == MouseButtonState.Pressed) {
                if (this.rightButtonStartClickPoint.X != -1 && this.rightButtonStartClickPoint.Y != -1) {
                    this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                    this.pathwayFE.ZoomRubberDraw();
                }
                else {
                    this.rightButtonEndClickPoint = new Point(-1, -1);
                }
            }
            else {
                RefreshUI();
            }
        }


        private void userControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (this.pathwayFE == null) return;
            if (this.pathwayObj == null) return;

            this.pathwayFE.IsLeftReleased = true;
            this.pathwayFE.IsLeftClicked = false;
            this.pathwayFE.IsNodeCaptured = false;

            if (!this.pathwayFE.IsControlHold && !this.pathwayFE.IsShiftHold) {
                RefreshUI();

                this.pathwayFE.ResetNodeCaptures();
            }
            else if (this.pathwayFE.IsShiftHold) {
                this.pathwayFE.MultipleNodeCapture();
                //this.pathwayFE.IsShiftHold = false;
            }

            this.pathwayFE.IsLeftReleased = false;
            this.leftButtonEndClickPoint = Mouse.GetPosition(this);
            RefreshUI();

            // Reset Mouse Position
            this.leftButtonStartClickPoint = new Point(-1, -1);
            this.leftButtonEndClickPoint = new Point(-1, -1);
        }


        private void userControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (this.pathwayFE == null) return;
            if (this.pathwayObj == null) return;

            this.rightButtonEndClickPoint = Mouse.GetPosition(this);

            if ((this.rightButtonStartClickPoint.X < this.leftMargin && this.rightButtonEndClickPoint.X < this.leftMargin)
                || (this.rightButtonStartClickPoint.X > this.ActualWidth - this.rightMargin && this.rightButtonEndClickPoint.X > this.ActualWidth - this.rightMargin)
                || (this.rightButtonStartClickPoint.Y < this.topMargin && this.rightButtonEndClickPoint.Y < this.topMargin)
                || (this.rightButtonStartClickPoint.Y > this.ActualHeight - this.bottomMargin && this.rightButtonEndClickPoint.Y > this.ActualHeight - this.leftMargin)) {
                this.rightButtonStartClickPoint = new Point(-1, -1);
                this.rightButtonEndClickPoint = new Point(-1, -1);
                return;
            }

            this.pathwayFE.GraphZoom();
            RefreshUI();

            // Reset Mouse Position
            this.rightButtonStartClickPoint = new Point(-1, -1);
            this.rightButtonEndClickPoint = new Point(-1, -1);
        }

        private void userControl_MouseLeave(object sender, MouseEventArgs e) {
            if (this.pathwayFE == null) return;
            if (this.pathwayObj == null) return;
            this.rightButtonEndClickPoint.X = -1;
            this.pathwayFE.IsNodeCaptured = false;

            if (!this.pathwayFE.IsControlHold && !this.pathwayFE.IsShiftHold) {
                this.pathwayFE.IsLeftClicked = false;
                this.pathwayFE.ResetNodeCaptures();
            }

            RefreshUI();
        }

        private void userControl_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (this.pathwayObj == null) return;
            this.currentMousePoint = Mouse.GetPosition(this);
            Point xyValue = this.pathwayFE.GetDataPositionOnMousePoint(this.currentMousePoint);

            if (Keyboard.Modifiers == ModifierKeys.Control) {
                float newMinX = float.MaxValue;
                float newMaxX = float.MinValue;
                float newMinY = float.MaxValue;
                float newMaxY = float.MinValue;

                if (e.Delta > 0) {
                    if (this.currentMousePoint.X > this.leftMargin) newMinX = (float)xyValue.X - (float)((xyValue.X - (float)this.pathwayObj.DisplayRangeMinX) * 0.95); else newMinX = (float)this.pathwayObj.DisplayRangeMinX;
                    if (this.currentMousePoint.X > this.leftMargin) newMaxX = (float)xyValue.X + (float)(((float)this.pathwayObj.DisplayRangeMaxX - xyValue.X) * 0.95); else newMaxX = (float)this.pathwayObj.DisplayRangeMaxX;
                    if (this.currentMousePoint.Y < this.ActualHeight - this.bottomMargin) newMinY = (float)xyValue.Y - (float)((xyValue.Y - (float)this.pathwayObj.DisplayRangeMinY) * 0.95); else newMinY = (float)this.pathwayObj.DisplayRangeMinY;
                    if (this.currentMousePoint.Y < this.ActualHeight - this.bottomMargin) newMaxY = (float)xyValue.Y + (float)(((float)this.pathwayObj.DisplayRangeMaxY - xyValue.Y) * 0.95); else newMaxY = (float)this.pathwayObj.DisplayRangeMaxY;
                }
                else {
                    if (this.currentMousePoint.X > this.leftMargin) newMinX = (float)xyValue.X - (float)((xyValue.X - (float)this.pathwayObj.DisplayRangeMinX) * 1.05); else newMinX = (float)this.pathwayObj.DisplayRangeMinX;
                    if (this.currentMousePoint.X > this.leftMargin) newMaxX = (float)xyValue.X + (float)(((float)this.pathwayObj.DisplayRangeMaxX - xyValue.X) * 1.05); else newMaxX = (float)this.pathwayObj.DisplayRangeMaxX;
                    if (this.currentMousePoint.Y < this.ActualHeight - this.bottomMargin) newMinY = (float)xyValue.Y - (float)((xyValue.Y - (float)this.pathwayObj.DisplayRangeMinY) * 1.05); else newMinY = (float)this.pathwayObj.DisplayRangeMinY;
                    if (this.currentMousePoint.Y < this.ActualHeight - this.bottomMargin) newMaxY = (float)xyValue.Y + (float)(((float)this.pathwayObj.DisplayRangeMaxY - xyValue.Y) * 1.05); else newMaxY = (float)this.pathwayObj.DisplayRangeMaxY;
                }

                if (newMinX < this.pathwayObj.MinX) this.pathwayObj.DisplayRangeMinX = this.pathwayObj.MinX; else this.pathwayObj.DisplayRangeMinX = newMinX;
                if (newMinY < this.pathwayObj.MinY) this.pathwayObj.DisplayRangeMinY = this.pathwayObj.MinY; else this.pathwayObj.DisplayRangeMinY = newMinY;
                if (newMaxX > this.pathwayObj.MaxX) this.pathwayObj.DisplayRangeMaxX = this.pathwayObj.MaxX; else this.pathwayObj.DisplayRangeMaxX = newMaxX;
                if (newMaxY > this.pathwayObj.MaxY) this.pathwayObj.DisplayRangeMaxY = this.pathwayObj.MaxY; else this.pathwayObj.DisplayRangeMaxY = newMaxY;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift) {
                var newMinX = 0.0F;
                var newMaxX = 0.0F;
                var diff = 0.0F;
                var foldchange = 0.95;
                if (e.Delta < 0) {
                    foldchange = 1.05;  
                }
                newMinX = (float)xyValue.X - (float)((xyValue.X - (float)this.pathwayObj.DisplayRangeMinX) * foldchange);
                diff = newMinX - this.pathwayObj.DisplayRangeMinX;
                newMaxX = this.pathwayObj.DisplayRangeMaxX + diff;
                this.pathwayObj.DisplayRangeMinX = newMinX;
                this.pathwayObj.DisplayRangeMaxX = newMaxX;
            }
            else {
                var newMinY = 0.0F;
                var newMaxY = 0.0F;
                var diff = 0.0F;
                var foldchange = 1.05;
                if (e.Delta < 0) {
                    foldchange = 0.95;
                }
                newMinY = (float)xyValue.Y - (float)((xyValue.Y - (float)this.pathwayObj.DisplayRangeMinY) * foldchange);
                diff = newMinY - this.pathwayObj.DisplayRangeMinY;
                newMaxY = this.pathwayObj.DisplayRangeMaxY + diff;
                this.pathwayObj.DisplayRangeMinY = newMinY;
                this.pathwayObj.DisplayRangeMaxY = newMaxY;
            }
            RefreshUI();
        }

        private void userControl_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (this.pathwayObj == null) return;
            if (e.StylusDevice != null) return; // Avoid Touch Event

            if (e.RightButton == MouseButtonState.Pressed) {
                this.currentMousePoint = Mouse.GetPosition(this);
                this.pathwayObj.DisplayRangeMinX = this.pathwayObj.MinX;
                this.pathwayObj.DisplayRangeMaxX = this.pathwayObj.MaxX;
                this.pathwayObj.DisplayRangeMinY = this.pathwayObj.MinY;
                this.pathwayObj.DisplayRangeMaxY = this.pathwayObj.MaxY;
                RefreshUI();
            }
        }
    }
}
