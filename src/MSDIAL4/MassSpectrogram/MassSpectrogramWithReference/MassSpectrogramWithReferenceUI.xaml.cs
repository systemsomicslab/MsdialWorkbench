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
    /// MassSpectrogramWithReferenceUI.xaml の相互作用ロジック
    /// </summary>
    public partial class MassSpectrogramWithReferenceUI : UserControl
    {
        public MassSpectrogramWithReferenceUI()
        {
            InitializeComponent();

            //Property settting
            this.massSpectrogramViewModel = null;
            this.massSpectrogramWithReferenceFE = new MassSpectrogramWithReferenceFE(this.massSpectrogramViewModel, this);
            this.Content = this.massSpectrogramWithReferenceFE;
        }

        private MassSpectrogramWithReferenceFE massSpectrogramWithReferenceFE;
        private MassSpectrogramViewModel massSpectrogramViewModel;

        //Graph area format
        private double leftMargin = 45;
        private double topMargin = 30;
        private double topMarginForLabel = 20;
        private double rightMargin = 10;
        private double bottomMargin = 40;
        private double bottomMarginForLabel = 20;

        private double triangleSize = 6;

        public double XToYscaleTransform { get { return this.ActualHeight / this.ActualWidth; } }
        public double YToXscaleTransform { get { return this.ActualWidth / this.ActualHeight; } }

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

        private float graphScrollInitialMassMinReference = -1; // Initial minXval for Graph Slide Event
        private float graphScrollInitialMassMaxReference = -1; // Initial maxXval for Graph Slide Event
        private float graphScrollInitialIntensityMinReference = -1; // Initial minYval for Graph Slide Event
        private float graphScrollInitialIntensityMaxReference = -1; // Initial maxYval for Graph Slide Event

        public MassSpectrogramWithReferenceUI(MassSpectrogramViewModel massSpectrogramViewModel)
        {
            InitializeComponent();
            
            //Property settting
            this.massSpectrogramViewModel = massSpectrogramViewModel;
            this.massSpectrogramWithReferenceFE = new MassSpectrogramWithReferenceFE(this.massSpectrogramViewModel, this);
            this.Content = this.massSpectrogramWithReferenceFE;
        }

        public MassSpectrogramWithReferenceUI(MassSpectrogramViewModel massSpectrogramViewModel, int topMarginForLabel) {
            InitializeComponent();

            //Property settting
            this.topMarginForLabel = topMarginForLabel;
            this.massSpectrogramViewModel = massSpectrogramViewModel;
            this.massSpectrogramWithReferenceFE = new MassSpectrogramWithReferenceFE(this.massSpectrogramViewModel, this);
            this.Content = this.massSpectrogramWithReferenceFE;
        }


        #region // properties
        public MassSpectrogramWithReferenceFE MassSpectrogramWithReferenceFE
        {
            get { return massSpectrogramWithReferenceFE; }
            set { massSpectrogramWithReferenceFE = value; }
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


        public float GraphScrollInitialMassMinReference
        {
            get { return graphScrollInitialMassMinReference; }
            set { graphScrollInitialMassMinReference = value; }
        }

        public float GraphScrollInitialMassMaxReference
        {
            get { return graphScrollInitialMassMaxReference; }
            set { graphScrollInitialMassMaxReference = value; }
        }

        public float GraphScrollInitialIntensityMinReference
        {
            get { return graphScrollInitialIntensityMinReference; }
            set { graphScrollInitialIntensityMinReference = value; }
        }

        public float GraphScrollInitialIntensityMaxReference
        {
            get { return graphScrollInitialIntensityMaxReference; }
            set { graphScrollInitialIntensityMaxReference = value; }
        }

        public double BottomMarginForLabel
        {
            get { return bottomMarginForLabel; }
            set { bottomMarginForLabel = value; }
        }

        public double TopMarginForLabel
        {
            get { return topMarginForLabel; }
            set { topMarginForLabel = value; }
        }
        #endregion

        public void RefreshUI()
        {
            this.massSpectrogramWithReferenceFE.MassSpectrogramDraw();
        }

        private void userControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshUI();
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
            if (this.massSpectrogramViewModel.DisplayRangeMassMax == null || this.massSpectrogramViewModel.DisplayRangeMassMin == null) return;
            if (e.StylusDevice != null) return; // Avoid Touch Event

            // Set Mouse Position
            this.leftButtonStartClickPoint = Mouse.GetPosition(this);

            // Set Initial Values for Graph Scroll
            this.graphScrollInitialMassMin = (float)this.massSpectrogramViewModel.DisplayRangeMassMin;
            this.graphScrollInitialMassMax = (float)this.massSpectrogramViewModel.DisplayRangeMassMax;
            this.graphScrollInitialIntensityMin = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin;
            this.graphScrollInitialIntensityMax = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax;

            this.graphScrollInitialMassMinReference = (float)this.massSpectrogramViewModel.DisplayRangeMassMinReference;
            this.graphScrollInitialMassMaxReference = (float)this.massSpectrogramViewModel.DisplayRangeMassMaxReference;
            this.graphScrollInitialIntensityMinReference = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMinReference;
            this.graphScrollInitialIntensityMaxReference = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference;
        }

        private void userControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.massSpectrogramViewModel == null) return;

            // Store Current Mouse Point
            this.currentMousePoint = Mouse.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.leftButtonEndClickPoint = Mouse.GetPosition(this);
                this.massSpectrogramWithReferenceFE.GraphScroll();
            }

            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (this.rightButtonStartClickPoint.X != -1 && this.rightButtonStartClickPoint.Y != -1)
                {

                    if (this.rightMouseButtonChromatogramRectangleZoom)
                    {
                        this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                        this.massSpectrogramWithReferenceFE.ZoomRubberDraw();
                    }
                    else if (this.rightMouseButtonChromatogramLeftRightZoom)
                    {
                        float[] peakInformation = this.massSpectrogramWithReferenceFE.getDataPositionOnMousePoint(this.rightButtonStartClickPoint);
                        if (peakInformation == null) return;

                        float mousePointMass = 0;
                        float newMinMass = float.MaxValue;
                        float newMaxMass = float.MinValue;

                        mousePointMass = peakInformation[0];

                        var displayRangeMin = Math.Min((decimal)this.massSpectrogramViewModel.DisplayRangeMassMin, (decimal)this.massSpectrogramViewModel.DisplayRangeMassMinReference);
                        var displayRangeMax = Math.Max((decimal)this.massSpectrogramViewModel.DisplayRangeMassMax, (decimal)this.massSpectrogramViewModel.DisplayRangeMassMaxReference);

                        if (Mouse.GetPosition(this).X - this.rightButtonEndClickPoint.X > 0)
                        {
                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);

                            newMinMass = mousePointMass - (float)((mousePointMass - (float)displayRangeMin) * 0.98);
                            newMaxMass = mousePointMass + (float)(((float)displayRangeMax - mousePointMass) * 0.98);

                            if (newMinMass + 0.0001 < newMaxMass)
                            {
                                this.massSpectrogramViewModel.DisplayRangeMassMin = newMinMass;
                                this.massSpectrogramViewModel.DisplayRangeMassMax = newMaxMass;

                                this.massSpectrogramViewModel.DisplayRangeMassMinReference = newMinMass;
                                this.massSpectrogramViewModel.DisplayRangeMassMaxReference = newMaxMass;
                            }
                        }
                        else if (Mouse.GetPosition(this).X - this.rightButtonEndClickPoint.X < 0)
                        {

                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);

                            newMinMass = mousePointMass - (float)((mousePointMass - (float)displayRangeMin) * 1.02);
                            newMaxMass = mousePointMass + (float)(((float)displayRangeMax - mousePointMass) * 1.02);

                            if (newMinMass < Math.Min(this.massSpectrogramViewModel.MinMass, this.massSpectrogramViewModel.MinMassReference))
                            {
                                this.massSpectrogramViewModel.DisplayRangeMassMin = Math.Min(this.massSpectrogramViewModel.MinMass, this.massSpectrogramViewModel.MinMassReference);
                                this.massSpectrogramViewModel.DisplayRangeMassMinReference = Math.Min(this.massSpectrogramViewModel.MinMass, this.massSpectrogramViewModel.MinMassReference);
                            }
                            else
                            {
                                this.massSpectrogramViewModel.DisplayRangeMassMin = newMinMass;
                                this.massSpectrogramViewModel.DisplayRangeMassMinReference = newMinMass;
                            }

                            if (newMaxMass > Math.Max(this.massSpectrogramViewModel.MaxMass, this.massSpectrogramViewModel.MaxMassReference))
                            {
                                this.massSpectrogramViewModel.DisplayRangeMassMax = Math.Max(this.massSpectrogramViewModel.MaxMass, this.massSpectrogramViewModel.MaxMassReference);
                                this.massSpectrogramViewModel.DisplayRangeMassMaxReference = Math.Max(this.massSpectrogramViewModel.MaxMass, this.massSpectrogramViewModel.MaxMassReference);
                            }
                            else
                            {
                                this.massSpectrogramViewModel.DisplayRangeMassMax = newMaxMass;
                                this.massSpectrogramViewModel.DisplayRangeMassMaxReference = newMaxMass;
                            }
                        }
                        this.massSpectrogramWithReferenceFE.MassSpectrogramDraw();
                    }
                    else if (this.rightMouseButtonChromatogramUpDownZoom)
                    {

                        float[] peakInformation = this.massSpectrogramWithReferenceFE.getDataPositionOnMousePoint(this.rightButtonStartClickPoint);
                        if (peakInformation == null) return;

                        float mousePointIntensity = 0;
                        float newMaxIntensityReference = float.MinValue;
                        float newMaxIntensity = float.MinValue;

                        mousePointIntensity = peakInformation[1];

                        // Mouse On Y-Axis                
                        if (Mouse.GetPosition(this).Y - this.rightButtonEndClickPoint.Y < 0)
                        {
                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);

                            if ((float)this.massSpectrogramViewModel.DisplayRangeIntensityMax < 0.0001 || (float)this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference < 0.0001)
                                return;

                            newMaxIntensity = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax * (float)0.98;
                            newMaxIntensityReference = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference * (float)0.98;
                            if (newMaxIntensity > 0 && newMaxIntensity > this.massSpectrogramViewModel.DisplayRangeIntensityMin && newMaxIntensityReference > 0 && newMaxIntensityReference > this.massSpectrogramViewModel.DisplayRangeIntensityMinReference)
                            {
                                this.massSpectrogramViewModel.DisplayRangeIntensityMax = newMaxIntensity;
                                this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference = newMaxIntensityReference;
                            }
                        }
                        else if (Mouse.GetPosition(this).Y - this.rightButtonEndClickPoint.Y > 0)
                        {
                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                            this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.DisplayRangeIntensityMax * 1.02F;
                            this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference = this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference * 1.02F;

                            if (this.massSpectrogramViewModel.DisplayRangeIntensityMax > this.massSpectrogramViewModel.MaxIntensity && this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference > this.massSpectrogramViewModel.MaxIntensityReference)
                            {
                                this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.MaxIntensity;
                                this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference = this.massSpectrogramViewModel.MaxIntensityReference;
                            }
                        }
                        this.massSpectrogramWithReferenceFE.MassSpectrogramDraw();
                    }
                }
                else
                {
                    this.rightButtonEndClickPoint = new Point(-1, -1);
                }
            }
        }

        private void userControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.massSpectrogramViewModel == null) return;

            if (this.rightMouseButtonChromatogramRectangleZoom)
            {
                this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                this.massSpectrogramWithReferenceFE.GraphZoom();
                this.massSpectrogramWithReferenceFE.MassSpectrogramDraw();
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
            this.massSpectrogramWithReferenceFE.MassSpectrogramDraw();

            // Reset Mouse Position
            this.leftButtonStartClickPoint = new Point(-1, -1);
            this.leftButtonEndClickPoint = new Point(-1, -1);
        }

        private void userControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (this.massSpectrogramViewModel == null) return;
            this.rightButtonEndClickPoint.X = -1;
            this.massSpectrogramWithReferenceFE.MassSpectrogramDraw();
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
                    this.massSpectrogramViewModel.DisplayRangeMassMin = Math.Min(this.massSpectrogramViewModel.MinMass, this.massSpectrogramViewModel.MinMassReference);
                    this.massSpectrogramViewModel.DisplayRangeMassMax = Math.Max(this.massSpectrogramViewModel.MaxMass, this.massSpectrogramViewModel.MaxMassReference);

                    this.massSpectrogramViewModel.DisplayRangeMassMinReference = Math.Min(this.massSpectrogramViewModel.MinMass, this.massSpectrogramViewModel.MinMassReference);
                    this.massSpectrogramViewModel.DisplayRangeMassMaxReference = Math.Max(this.massSpectrogramViewModel.MaxMass, this.massSpectrogramViewModel.MaxMassReference);

                    RefreshUI();
                }
                else if (this.currentMousePoint.X < this.leftMargin && this.currentMousePoint.Y < this.ActualHeight - this.bottomMargin)
                {
                    this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramViewModel.MinIntensity;
                    this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.MaxIntensity;

                    this.massSpectrogramViewModel.DisplayRangeIntensityMinReference = this.massSpectrogramViewModel.MinIntensityReference;
                    this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference = this.massSpectrogramViewModel.MaxIntensityReference;

                    RefreshUI();
                }
                else if (this.currentMousePoint.X > this.leftMargin && this.currentMousePoint.Y > this.topMargin)
                {
                    this.massSpectrogramWithReferenceFE.ResetGraphDisplayRange();
                }
            }
        }

        private void userControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (this.massSpectrogramViewModel == null) return;
            
            this.currentMousePoint = Mouse.GetPosition(this);
            
            var peakInformation = this.massSpectrogramWithReferenceFE.getDataPositionOnMousePoint(this.currentMousePoint);
            if (peakInformation == null) return;

            float newMinX = float.MaxValue;
            float newMaxX = float.MinValue;
            float newMaxY = float.MinValue;
            float newMaxRefY = float.MinValue;
            
            float mass = peakInformation[0];
            float intensity = peakInformation[1];

            var displayRangeMin = Math.Min((decimal)this.massSpectrogramViewModel.DisplayRangeMassMin, (decimal)this.massSpectrogramViewModel.DisplayRangeMassMinReference);
            var displayRangeMax = Math.Max((decimal)this.massSpectrogramViewModel.DisplayRangeMassMax, (decimal)this.massSpectrogramViewModel.DisplayRangeMassMaxReference);

            if (e.Delta > 0)
            {
                newMinX = (float)mass - (float)((mass - (float)displayRangeMin) * 0.9);
                newMaxX = (float)mass + (float)(((float)displayRangeMax - mass) * 0.9);
                newMaxY = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax * 0.9F;
                newMaxRefY = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference * 0.9F;
            }
            else
            {
                newMinX = (float)mass - (float)((mass - (float)displayRangeMin) * 1.1);
                newMaxX = (float)mass + (float)(((float)displayRangeMax - mass) * 1.1);
                newMaxY = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax * 1.1F;
                newMaxRefY = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference * 1.1F;
            }

            if (this.currentMousePoint.X > this.leftMargin && this.currentMousePoint.Y > this.ActualHeight - this.bottomMargin)
            {
                if (newMinX < Math.Min(this.massSpectrogramViewModel.MinMass, this.massSpectrogramViewModel.MinMassReference))
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMin = Math.Min(this.massSpectrogramViewModel.MinMass, this.massSpectrogramViewModel.MinMassReference);
                    this.massSpectrogramViewModel.DisplayRangeMassMinReference = Math.Min(this.massSpectrogramViewModel.MinMass, this.massSpectrogramViewModel.MinMassReference);
                }
                else
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMin = newMinX;
                    this.massSpectrogramViewModel.DisplayRangeMassMinReference = newMinX;
                }

                if (newMaxX > Math.Max(this.massSpectrogramViewModel.MaxMass, this.massSpectrogramViewModel.MaxMassReference))
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMax = Math.Max(this.massSpectrogramViewModel.MaxMass, this.massSpectrogramViewModel.MaxMassReference);
                    this.massSpectrogramViewModel.DisplayRangeMassMaxReference = Math.Max(this.massSpectrogramViewModel.MaxMass, this.massSpectrogramViewModel.MaxMassReference);
                }
                else
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMax = newMaxX;
                    this.massSpectrogramViewModel.DisplayRangeMassMaxReference = newMaxX;
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

                if (newMaxRefY > this.massSpectrogramViewModel.MaxIntensityReference)
                {
                    this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference = this.massSpectrogramViewModel.MaxIntensityReference;
                }
                else
                {
                    this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference = newMaxRefY;
                }
            }
            
            RefreshUI();
        }
    }
}
