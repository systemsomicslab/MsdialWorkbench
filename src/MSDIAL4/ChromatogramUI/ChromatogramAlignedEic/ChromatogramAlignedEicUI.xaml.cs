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

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// ChromatogramTicEicUI.xaml の相互作用ロジック
    /// </summary>
    public partial class ChromatogramAlignedEicUI : UserControl
    {
        private ChromatogramAlignedEicFE chromatogramAlignedEicFE;
        private ChromatogramTicEicViewModel chromatogramTicEicViewModel;

        //Graph area format
        private double leftMargin = 55;
        private double topMargin = 20;
        private double topMarginForLabel = 20;
        private double rightMargin = 10;
        private double bottomMargin = 40;
        private double triangleSize = 6;

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
        private float graphScrollInitialRtMin = -1; // Initial minXval for Graph Slide Event
        private float graphScrollInitialRtMax = -1; // Initial maxXval for Graph Slide Event
        private float graphScrollInitialIntensityMin = -1; // Initial minYval for Graph Slide Event
        private float graphScrollInitialIntensityMax = -1; // Initial maxYval for Graph Slide Event

        public ChromatogramAlignedEicUI() {
            InitializeComponent();

            //Property settting
            this.chromatogramTicEicViewModel = null;
            this.chromatogramAlignedEicFE = new ChromatogramAlignedEicFE(this.chromatogramTicEicViewModel, this);
            this.Content = this.chromatogramAlignedEicFE;
        }

        public ChromatogramAlignedEicUI(ChromatogramTicEicViewModel ChromatogramTicEicViewModel) {
            InitializeComponent();

            //Property settting
            this.chromatogramTicEicViewModel = ChromatogramTicEicViewModel;
            this.chromatogramAlignedEicFE = new ChromatogramAlignedEicFE(this.chromatogramTicEicViewModel, this);
            this.Content = this.chromatogramAlignedEicFE;
        }

        public void RefreshUI() {
            this.chromatogramAlignedEicFE.ChromatogramDraw();
        }

        private void userControl_SizeChanged(object sender, SizeChangedEventArgs e) {
            RefreshUI();
        }

        private void useerControl_MouseLeftDoubleClick(object sender, MouseButtonEventArgs e) {
            if (this.chromatogramTicEicViewModel == null) return;
            if (e.StylusDevice != null) return;// Avoid Touch Event

            // Set Mouse Position
            if (e.LeftButton == MouseButtonState.Pressed) {
                this.currentMousePoint = Mouse.GetPosition(this);

                if (this.currentMousePoint.X > this.leftMargin && this.currentMousePoint.Y > this.ActualHeight - this.bottomMargin) {
                    this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicViewModel.MaxRt;
                    this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicViewModel.MinRt;
                    RefreshUI();
                }
                else if (this.currentMousePoint.X < this.leftMargin && this.currentMousePoint.Y < this.ActualHeight - this.bottomMargin) {
                    this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicViewModel.MaxIntensity;
                    this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramTicEicViewModel.MinIntensity;
                    RefreshUI();
                }
                else if (this.currentMousePoint.X > this.leftMargin && this.currentMousePoint.Y > this.topMargin + this.topMarginForLabel) {
                    this.chromatogramAlignedEicFE.ResetGraphDisplayRange();
                }
            }
        }

        private void userControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            if (this.chromatogramTicEicViewModel == null) return;

            // Set Mouse Position
            this.rightButtonStartClickPoint = Mouse.GetPosition(this);

            if (this.rightButtonStartClickPoint.X <= this.leftMargin && this.rightButtonStartClickPoint.Y > this.topMargin && this.rightButtonStartClickPoint.Y < this.ActualHeight - this.bottomMargin) this.rightMouseButtonChromatogramUpDownZoom = true;
            else if (this.rightButtonStartClickPoint.X > this.leftMargin && this.rightButtonStartClickPoint.Y >= this.ActualHeight - this.bottomMargin) this.rightMouseButtonChromatogramLeftRightZoom = true;
            else this.rightMouseButtonChromatogramRectangleZoom = true;
        }

        private void userControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (this.chromatogramTicEicViewModel == null) return;
            if (e.StylusDevice != null) return; // Avoid Touch Event

            // Set Mouse Position
            this.leftButtonStartClickPoint = Mouse.GetPosition(this);

            if (this.chromatogramTicEicViewModel.EditMode == ChromatogramEditMode.Display) {
                // Set Initial Values for Graph Scroll
                this.graphScrollInitialRtMin = (float)this.chromatogramTicEicViewModel.DisplayRangeRtMin;
                this.graphScrollInitialRtMax = (float)this.chromatogramTicEicViewModel.DisplayRangeRtMax;
                this.graphScrollInitialIntensityMin = (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMin;
                this.graphScrollInitialIntensityMax = (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMax;
            }
        }

        private void userControl_MouseMove(object sender, MouseEventArgs e) {
            if (this.chromatogramTicEicViewModel == null) return;

            // Store Current Mouse Point
            this.currentMousePoint = Mouse.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed) {
                this.leftButtonEndClickPoint = Mouse.GetPosition(this);

                if (this.chromatogramTicEicViewModel.EditMode == ChromatogramEditMode.Display) {
                    this.chromatogramAlignedEicFE.GraphScroll();
                }
            }

            if (e.RightButton == MouseButtonState.Pressed) {
                if (this.rightButtonStartClickPoint.X != -1 && this.rightButtonStartClickPoint.Y != -1) {

                    if (this.rightMouseButtonChromatogramRectangleZoom) {
                        this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                        if (this.chromatogramTicEicViewModel.EditMode == ChromatogramEditMode.Display) {
                            this.chromatogramAlignedEicFE.ZoomRubberDraw();
                        }
                    }
                    else if (this.rightMouseButtonChromatogramLeftRightZoom) {
                        float[] peakInformation = this.chromatogramAlignedEicFE.getDataPositionOnMousePoint(this.rightButtonStartClickPoint);
                        if (peakInformation == null) return;

                        float mousePointRt = 0;
                        float newMinRt = float.MaxValue;
                        float newMaxRt = float.MinValue;

                        mousePointRt = peakInformation[1];

                        if (Mouse.GetPosition(this).X - this.rightButtonEndClickPoint.X > 0) {
                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);

                            newMinRt = mousePointRt - (float)((mousePointRt - (float)this.chromatogramTicEicViewModel.DisplayRangeRtMin) * 0.98);
                            newMaxRt = mousePointRt + (float)(((float)this.chromatogramTicEicViewModel.DisplayRangeRtMax - mousePointRt) * 0.98);

                            if (newMinRt + 0.0001 < newMaxRt) {
                                this.chromatogramTicEicViewModel.DisplayRangeRtMin = newMinRt;
                                this.chromatogramTicEicViewModel.DisplayRangeRtMax = newMaxRt;
                            }
                        }
                        else if (Mouse.GetPosition(this).X - this.rightButtonEndClickPoint.X < 0) {

                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);

                            newMinRt = mousePointRt - (float)((mousePointRt - (float)this.chromatogramTicEicViewModel.DisplayRangeRtMin) * 1.02);
                            newMaxRt = mousePointRt + (float)(((float)this.chromatogramTicEicViewModel.DisplayRangeRtMax - mousePointRt) * 1.02);

                            if (newMinRt < this.chromatogramTicEicViewModel.MinRt)
                                this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicViewModel.MinRt;
                            else
                                this.chromatogramTicEicViewModel.DisplayRangeRtMin = newMinRt;

                            if (newMaxRt > this.chromatogramTicEicViewModel.MaxRt)
                                this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicViewModel.MaxRt;
                            else
                                this.chromatogramTicEicViewModel.DisplayRangeRtMax = newMaxRt;
                        }
                        this.chromatogramAlignedEicFE.ChromatogramDraw();
                    }
                    else if (this.rightMouseButtonChromatogramUpDownZoom) {

                        float[] peakInformation = this.chromatogramAlignedEicFE.getDataPositionOnMousePoint(this.rightButtonStartClickPoint);
                        if (peakInformation == null) return;

                        float mousePointIntensity = 0;
                        float newMinIntensity = float.MaxValue;
                        float newMaxIntensity = float.MinValue;

                        mousePointIntensity = peakInformation[3];

                        // Mouse On Y-Axis                
                        if (Mouse.GetPosition(this).Y - this.rightButtonEndClickPoint.Y < 0) {
                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);

                            if ((float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMax < 0.0001)
                                return;

                            newMaxIntensity = (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMax * (float)0.98;
                            if (newMaxIntensity > 0 && newMaxIntensity > this.chromatogramTicEicViewModel.DisplayRangeIntensityMin)
                                this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = newMaxIntensity;
                        }
                        else if (Mouse.GetPosition(this).Y - this.rightButtonEndClickPoint.Y > 0) {
                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                            this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicViewModel.DisplayRangeIntensityMax * 1.02F;

                            if (this.chromatogramTicEicViewModel.DisplayRangeIntensityMax > this.chromatogramTicEicViewModel.MaxIntensity)
                                this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicViewModel.MaxIntensity;
                        }
                        this.chromatogramAlignedEicFE.ChromatogramDraw();
                    }
                }
                else {
                    this.rightButtonEndClickPoint = new Point(-1, -1);
                }
            }
            else
                RefreshUI();
        }

        private void userControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (this.chromatogramTicEicViewModel == null) return;

            if (this.rightMouseButtonChromatogramRectangleZoom) {
                this.rightButtonEndClickPoint = Mouse.GetPosition(this);

                if (this.chromatogramTicEicViewModel.EditMode == ChromatogramEditMode.Display) {
                    this.chromatogramAlignedEicFE.GraphZoom();
                }

                this.chromatogramAlignedEicFE.ChromatogramDraw();
            }

            // Reset Mouse Position
            this.rightButtonStartClickPoint = new Point(-1, -1);
            this.rightButtonEndClickPoint = new Point(-1, -1);
            this.rightMouseButtonChromatogramLeftRightZoom = false;
            this.rightMouseButtonChromatogramRectangleZoom = false;
            this.rightMouseButtonChromatogramUpDownZoom = false;

        }

        private void userControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (this.chromatogramTicEicViewModel == null) return;
            this.chromatogramAlignedEicFE.ChromatogramDraw();

            // Reset Mouse Position
            this.leftButtonStartClickPoint = new Point(-1, -1);
            this.leftButtonEndClickPoint = new Point(-1, -1);
        }

        private void userControl_MouseLeave(object sender, MouseEventArgs e) {
            if (this.chromatogramTicEicViewModel == null) return;
            this.rightButtonEndClickPoint.X = -1;
            this.chromatogramAlignedEicFE.ChromatogramDraw();
        }

        private void userControl_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (this.chromatogramTicEicViewModel == null) return;

            this.currentMousePoint = Mouse.GetPosition(this);

            var peakInformation = this.chromatogramAlignedEicFE.getDataPositionOnMousePoint(this.currentMousePoint);
            if (peakInformation == null) return;

            float newMinX = float.MaxValue;
            float newMaxX = float.MinValue;
            float newMaxY = float.MinValue;

            float rt = peakInformation[1];
            float intensity = peakInformation[3];

            if (e.Delta > 0) {
                newMinX = (float)rt - (float)((rt - (float)this.chromatogramTicEicViewModel.DisplayRangeRtMin) * 0.9);
                newMaxX = (float)rt + (float)(((float)this.chromatogramTicEicViewModel.DisplayRangeRtMax - rt) * 0.9);
                newMaxY = (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMax * 0.9F;
            }
            else {
                newMinX = (float)rt - (float)((rt - (float)this.chromatogramTicEicViewModel.DisplayRangeRtMin) * 1.1);
                newMaxX = (float)rt + (float)(((float)this.chromatogramTicEicViewModel.DisplayRangeRtMax - rt) * 1.1);
                newMaxY = (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMax * 1.1F;
            }

            if (this.currentMousePoint.X > this.leftMargin && this.currentMousePoint.Y > this.ActualHeight - this.bottomMargin) {
                if (newMinX < this.chromatogramTicEicViewModel.MinRt) {
                    this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicViewModel.MinRt;
                }
                else {
                    this.chromatogramTicEicViewModel.DisplayRangeRtMin = newMinX;
                }

                if (newMaxX > this.chromatogramTicEicViewModel.MaxRt) {
                    this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicViewModel.MaxRt;
                }
                else {
                    this.chromatogramTicEicViewModel.DisplayRangeRtMax = newMaxX;
                }

            }
            else if (this.currentMousePoint.X <= this.leftMargin && this.currentMousePoint.Y > this.topMargin && this.currentMousePoint.Y < this.ActualHeight - this.bottomMargin) {
                if (newMaxY > this.chromatogramTicEicViewModel.MaxIntensity) {
                    this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicViewModel.MaxIntensity;
                }
                else {
                    this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = newMaxY;
                }
            }

            RefreshUI();
        }

        #region // properties
        public ChromatogramAlignedEicFE ChromatogramAlignedEicFE {
            get { return chromatogramAlignedEicFE; }
            set { chromatogramAlignedEicFE = value; }
        }

        public ChromatogramTicEicViewModel ChromatogramTicEicViewModel {
            get { return chromatogramTicEicViewModel; }
            set { chromatogramTicEicViewModel = value; }
        }


        public double LeftMargin {
            get { return leftMargin; }
            set { leftMargin = value; }
        }

        public double TopMargin {
            get { return topMargin; }
            set { topMargin = value; }
        }

        public double TopMarginForLabel {
            get { return topMarginForLabel; }
            set { topMarginForLabel = value; }
        }

        public double RightMargin {
            get { return rightMargin; }
            set { rightMargin = value; }
        }

        public double BottomMargin {
            get { return bottomMargin; }
            set { bottomMargin = value; }
        }

        public double TriangleSize {
            get { return triangleSize; }
            set { triangleSize = value; }
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

        public bool LeftMouseButtonLeftEdgeCapture {
            get { return leftMouseButtonLeftEdgeCapture; }
            set { leftMouseButtonLeftEdgeCapture = value; }
        }

        public bool LeftMouseButtonRightEdgeCapture {
            get { return leftMouseButtonRightEdgeCapture; }
            set { leftMouseButtonRightEdgeCapture = value; }
        }

        public bool RightMouseButtonChromatogramUpDownZoom {
            get { return rightMouseButtonChromatogramUpDownZoom; }
            set { rightMouseButtonChromatogramUpDownZoom = value; }
        }

        public bool RightMouseButtonChromatogramLeftRightZoom {
            get { return rightMouseButtonChromatogramLeftRightZoom; }
            set { rightMouseButtonChromatogramLeftRightZoom = value; }
        }

        public bool RightMouseButtonChromatogramRectangleZoom {
            get { return rightMouseButtonChromatogramRectangleZoom; }
            set { rightMouseButtonChromatogramRectangleZoom = value; }
        }

        public bool KeyDownCheck {
            get { return keyDownCheck; }
            set { keyDownCheck = value; }
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

    }
}
