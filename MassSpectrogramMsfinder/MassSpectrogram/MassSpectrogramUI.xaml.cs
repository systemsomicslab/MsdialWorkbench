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
    /// MassSpectrogramUI.xaml の相互作用ロジック
    /// </summary>
    public partial class MassSpectrogramUI : UserControl
    {
        public MassSpectrogramUI()
        {
            InitializeComponent();

            //Property settting
            this.massSpectrogramViewModel = null;
            this.massSpectrogramFE = new MassSpectrogramFE(this.massSpectrogramViewModel, this);
            this.Content = this.massSpectrogramFE;
        }

        private MassSpectrogramFE massSpectrogramFE;
        private MassSpectrogramViewModel massSpectrogramViewModel;

        //Graph area format
        private double leftMargin = 45;
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
        private float graphScrollInitialMassMin = -1; // Initial minXval for Graph Slide Event
        private float graphScrollInitialMassMax = -1; // Initial maxXval for Graph Slide Event
        private float graphScrollInitialIntensityMin = -1; // Initial minYval for Graph Slide Event
        private float graphScrollInitialIntensityMax = -1; // Initial maxYval for Graph Slide Event

        public MassSpectrogramUI(MassSpectrogramViewModel massSpectrogramViewModel)
        {
            InitializeComponent();
            
            //Property settting
            this.massSpectrogramViewModel = massSpectrogramViewModel;
            this.massSpectrogramFE = new MassSpectrogramFE(this.massSpectrogramViewModel, this);
            this.Content = this.massSpectrogramFE;
        }

        private void userControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            this.massSpectrogramFE.MassSpectrogramDraw();
        }

        private void userControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (this.massSpectrogramViewModel == null) return;
            this.rightButtonEndClickPoint.X = -1;
            this.massSpectrogramFE.MassSpectrogramDraw();
        }

        private void userControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.massSpectrogramViewModel == null) return;

            // Set Mouse Position
            this.rightButtonStartClickPoint = Mouse.GetPosition(this);

            if (this.rightButtonStartClickPoint.X <= this.leftMargin && this.rightButtonStartClickPoint.Y > this.topMargin && this.rightButtonStartClickPoint.Y < this.ActualHeight - this.bottomMargin) this.rightMouseButtonChromatogramUpDownZoom = true;
            else if (this.rightButtonStartClickPoint.X > this.leftMargin && this.rightButtonStartClickPoint.Y >= this.ActualHeight - this.bottomMargin) this.rightMouseButtonChromatogramLeftRightZoom = true;
            else this.rightMouseButtonChromatogramRectangleZoom = true;
        }

        private void userControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.massSpectrogramViewModel == null) return;
            if (e.StylusDevice != null) return; // Avoid Touch Event

            // Set Mouse Position
            this.leftButtonStartClickPoint = Mouse.GetPosition(this);

            // Set Initial Values for Graph Scroll
            this.graphScrollInitialMassMin = (float)this.massSpectrogramViewModel.DisplayRangeMassMin;
            this.graphScrollInitialMassMax = (float)this.massSpectrogramViewModel.DisplayRangeMassMax;
            this.graphScrollInitialIntensityMin = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin;
            this.graphScrollInitialIntensityMax = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax;

        }

        private void userControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.massSpectrogramViewModel == null) return;

            // Store Current Mouse Point
            this.currentMousePoint = Mouse.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.leftButtonEndClickPoint = Mouse.GetPosition(this);
                this.massSpectrogramFE.GraphScroll();
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                if (this.rightButtonStartClickPoint.X != -1 && this.rightButtonStartClickPoint.Y != -1)
                {

                    if (this.rightMouseButtonChromatogramRectangleZoom)
                    {
                        this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                        this.massSpectrogramFE.ZoomRubberDraw();
                    }
                    else if (this.rightMouseButtonChromatogramLeftRightZoom)
                    {
                        float[] peakInformation = this.massSpectrogramFE.getDataPositionOnMousePoint(this.rightButtonStartClickPoint);
                        if (peakInformation == null) return;

                        float mousePointMass = 0;
                        float newMinMass = float.MaxValue;
                        float newMaxMass = float.MinValue;

                        mousePointMass = peakInformation[0];

                        if (Mouse.GetPosition(this).X - this.rightButtonEndClickPoint.X > 0)
                        {
                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);

                            newMinMass = mousePointMass - (float)((mousePointMass - (float)this.massSpectrogramViewModel.DisplayRangeMassMin) * 0.98);
                            newMaxMass = mousePointMass + (float)(((float)this.massSpectrogramViewModel.DisplayRangeMassMax - mousePointMass) * 0.98);

                            if (newMinMass + 0.0001 < newMaxMass)
                            {
                                this.massSpectrogramViewModel.DisplayRangeMassMax = newMaxMass;
                                this.massSpectrogramViewModel.DisplayRangeMassMin = newMinMass;
                            }
                        }
                        else if (Mouse.GetPosition(this).X - this.rightButtonEndClickPoint.X < 0)
                        {

                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);

                            newMinMass = mousePointMass - (float)((mousePointMass - (float)this.massSpectrogramViewModel.DisplayRangeMassMin) * 1.02);
                            newMaxMass = mousePointMass + (float)(((float)this.massSpectrogramViewModel.DisplayRangeMassMax - mousePointMass) * 1.02);


                            if (newMaxMass > this.massSpectrogramViewModel.MaxMass)
                            {
                                this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.MaxMass;
                            }
                            else
                            {
                                this.massSpectrogramViewModel.DisplayRangeMassMax = newMaxMass;
                            }

                            if (newMinMass < this.massSpectrogramViewModel.MinMass)
                            {
                                this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.MinMass;
                            }
                            else
                            {
                                this.massSpectrogramViewModel.DisplayRangeMassMin = newMinMass;
                            }

                        }
                        this.massSpectrogramFE.MassSpectrogramDraw();
                    }
                    else if (this.rightMouseButtonChromatogramUpDownZoom)
                    {

                        float[] peakInformation = this.massSpectrogramFE.getDataPositionOnMousePoint(this.rightButtonStartClickPoint);
                        if (peakInformation == null) return;

                        float mousePointIntensity = 0;
                        float newMaxIntensity = float.MinValue;

                        mousePointIntensity = peakInformation[1];

                        // Mouse On Y-Axis                
                        if (Mouse.GetPosition(this).Y - this.rightButtonEndClickPoint.Y < 0)
                        {
                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);

                            if ((float)this.massSpectrogramViewModel.DisplayRangeIntensityMax < 0.0001)
                                return;

                            newMaxIntensity = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax * (float)0.98;
                            if (newMaxIntensity > 0 && newMaxIntensity > this.massSpectrogramViewModel.DisplayRangeIntensityMin)
                            {
                                this.massSpectrogramViewModel.DisplayRangeIntensityMax = newMaxIntensity;
                            }
                        }
                        else if (Mouse.GetPosition(this).Y - this.rightButtonEndClickPoint.Y > 0)
                        {
                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                            this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.DisplayRangeIntensityMax * 1.02F;

                            if (this.massSpectrogramViewModel.DisplayRangeIntensityMax > this.massSpectrogramViewModel.MaxIntensity)
                            {
                                this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.MaxIntensity;
                            }
                        }
                        this.massSpectrogramFE.MassSpectrogramDraw();
                    }
                }
                else
                {
                    this.rightButtonEndClickPoint = new Point(-1, -1);
                }
            }
            else
            {
                RefreshUI();
            }
        }

        private void userControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.massSpectrogramViewModel == null) return;

            if (this.rightMouseButtonChromatogramRectangleZoom)
            {
                this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                this.massSpectrogramFE.GraphZoom();

                this.massSpectrogramFE.MassSpectrogramDraw();
            }

            // Reset Mouse Position
            this.rightButtonStartClickPoint = new Point(-1, -1);
            this.rightButtonEndClickPoint = new Point(-1, -1);
            this.rightMouseButtonChromatogramLeftRightZoom = false;
            this.rightMouseButtonChromatogramRectangleZoom = false;
            this.rightMouseButtonChromatogramUpDownZoom = false;

        }

        private void userControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.massSpectrogramViewModel == null) return;
            this.massSpectrogramFE.MassSpectrogramDraw();

            // Reset Mouse Position
            this.leftButtonStartClickPoint = new Point(-1, -1);
            this.leftButtonEndClickPoint = new Point(-1, -1);
        }


        public MassSpectrogramFE MassSpectrogramFE
        {
            get { return massSpectrogramFE; }
            set { massSpectrogramFE = value; }
        }

        public MassSpectrogramViewModel MassSpectrogramViewModel
        {
            get { return massSpectrogramViewModel; }
            set { massSpectrogramViewModel = value; }
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

        public double TriangleSize
        {
            get { return triangleSize; }
            set { triangleSize = value; }
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

        public double TopMarginForLabel
        {
            get { return topMarginForLabel; }
            set { topMarginForLabel = value; }
        }

        private void userControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.massSpectrogramViewModel == null) return;
            if (e.StylusDevice != null) return; // Avoid Touch Event

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.currentMousePoint = Mouse.GetPosition(this);

                if (this.currentMousePoint.X > this.leftMargin && this.currentMousePoint.Y > this.ActualHeight - this.bottomMargin)
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.MaxMass;
                    this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.MinMass;

                    RefreshUI();
                }
                else if (this.currentMousePoint.X < this.leftMargin && this.currentMousePoint.Y < this.ActualHeight - this.bottomMargin)
                {
                    this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.MaxIntensity;
                    this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramViewModel.MinIntensity;

                    RefreshUI();
                }
                else if (this.currentMousePoint.X > this.leftMargin && this.currentMousePoint.Y > this.topMargin)
                {
                    this.massSpectrogramFE.ResetGraphDisplayRange();
                }
            }
        }

        private void userControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (this.massSpectrogramViewModel == null) return;

            this.currentMousePoint = Mouse.GetPosition(this);

            var peakInformation = this.massSpectrogramFE.getDataPositionOnMousePoint(this.currentMousePoint);
            if (peakInformation == null) return;

            float newMinX = float.MaxValue;
            float newMaxX = float.MinValue;
            float newMaxY = float.MinValue;

            float mass = peakInformation[0];
            float intensity = peakInformation[1];

            if (e.Delta > 0)
            {
                newMinX = (float)mass - (float)((mass - (float)this.massSpectrogramViewModel.DisplayRangeMassMin) * 0.9);
                newMaxX = (float)mass + (float)(((float)this.massSpectrogramViewModel.DisplayRangeMassMax - mass) * 0.9);
                newMaxY = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax * 0.9F;
            }
            else
            {
                newMinX = (float)mass - (float)((mass - (float)this.massSpectrogramViewModel.DisplayRangeMassMin) * 1.1);
                newMaxX = (float)mass + (float)(((float)this.massSpectrogramViewModel.DisplayRangeMassMax - mass) * 1.1);
                newMaxY = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax * 1.1F;
            }

            if (this.currentMousePoint.X > this.leftMargin && this.currentMousePoint.Y > this.ActualHeight - this.bottomMargin)
            {
                if (newMaxX > this.massSpectrogramViewModel.MaxMass)
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.MaxMass;
                }
                else
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMax = newMaxX;
                }

                if (newMinX < this.massSpectrogramViewModel.MinMass)
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.MinMass;
                }
                else
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMin = newMinX;
                }
            }
            else if (this.currentMousePoint.X <= this.leftMargin && this.currentMousePoint.Y > this.topMargin && this.currentMousePoint.Y < this.ActualHeight - this.bottomMargin)
            {
                if (newMaxY > this.massSpectrogramViewModel.MaxIntensity)
                {
                    this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.MaxIntensity;
                }
                else
                {
                    this.massSpectrogramViewModel.DisplayRangeIntensityMax = newMaxY;
                }
            }

            RefreshUI();
        }
    }
}
