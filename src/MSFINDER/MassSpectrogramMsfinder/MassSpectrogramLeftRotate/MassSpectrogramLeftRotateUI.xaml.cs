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
    /// MassSpectrogramLeftRotateUI.xaml の相互作用ロジック
    /// </summary>
    public partial class MassSpectrogramLeftRotateUI : UserControl
    {
        private MassSpectrogramLeftRotateFE massSpectrogramLeftRotateFE;
        private MassSpectrogramViewModel massSpectrogramViewModel;

        //Graph area format
        private double leftMargin = 30;
        private double leftMarginForLabel = 20;
        private double topMargin = 55;
        private double rightMargin = 30;
        private double bottomMargin = 43;
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

        public MassSpectrogramLeftRotateUI()
        {
            InitializeComponent();

            //Property settting
            this.massSpectrogramViewModel = null;
            this.massSpectrogramLeftRotateFE = new MassSpectrogramLeftRotateFE(this.massSpectrogramViewModel, this);
            this.Content = this.massSpectrogramLeftRotateFE;
        }

        public MassSpectrogramLeftRotateUI(MassSpectrogramViewModel massSpectrogramViewModel)
        {
            InitializeComponent();

            //Property settting
            this.massSpectrogramViewModel = massSpectrogramViewModel;
            this.massSpectrogramLeftRotateFE = new MassSpectrogramLeftRotateFE(this.massSpectrogramViewModel, this);
            this.Content = this.massSpectrogramLeftRotateFE;
        }

        private void userControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            this.massSpectrogramLeftRotateFE.MassSpectrogramDraw();
        }

        private void userControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.massSpectrogramViewModel == null) return;

            // Set Mouse Position
            this.rightButtonStartClickPoint = Mouse.GetPosition(this);

            if (this.rightButtonStartClickPoint.X >= this.ActualWidth - this.rightMargin && this.rightButtonStartClickPoint.Y > this.topMargin && this.rightButtonStartClickPoint.Y < this.ActualHeight - this.bottomMargin) this.rightMouseButtonChromatogramUpDownZoom = true;
        }

        private void userControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.massSpectrogramViewModel == null) return;

            // Store Current Mouse Point
            this.currentMousePoint = Mouse.GetPosition(this);

            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (this.rightButtonStartClickPoint.X != -1 && this.rightButtonStartClickPoint.Y != -1)
                {
                    if (this.rightMouseButtonChromatogramUpDownZoom)
                    {
                        float[] peakInformation = this.massSpectrogramLeftRotateFE.getDataPositionOnMousePoint(this.rightButtonStartClickPoint);
                        if (peakInformation == null) return;

                        float mousePointIntensity = 0;
                        float newMinIntensity = float.MaxValue;
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
                                this.massSpectrogramViewModel.DisplayRangeIntensityMax = newMaxIntensity;
                        }
                        else if (Mouse.GetPosition(this).Y - this.rightButtonEndClickPoint.Y > 0)
                        {
                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                            this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.DisplayRangeIntensityMax * 1.02F;

                            if (this.massSpectrogramViewModel.DisplayRangeIntensityMax > this.massSpectrogramViewModel.MaxIntensity)
                                this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.MaxIntensity;
                        }
                        this.massSpectrogramLeftRotateFE.MassSpectrogramDraw();
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

            // Reset Mouse Position
            this.rightButtonStartClickPoint = new Point(-1, -1);
            this.rightButtonEndClickPoint = new Point(-1, -1);
            this.rightMouseButtonChromatogramLeftRightZoom = false;
            this.rightMouseButtonChromatogramRectangleZoom = false;
            this.rightMouseButtonChromatogramUpDownZoom = false;

        }

        private void userControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (this.massSpectrogramViewModel == null) return;
            this.rightButtonEndClickPoint.X = -1;
            this.massSpectrogramLeftRotateFE.MassSpectrogramDraw();
        }

        public MassSpectrogramLeftRotateFE MassSpectrogramLeftRotateFE
        {
            get { return massSpectrogramLeftRotateFE; }
            set { massSpectrogramLeftRotateFE = value; }
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

        public double LeftMarginForLabel
        {
            get { return leftMarginForLabel; }
            set { leftMarginForLabel = value; }
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
    }
}
