using System;
using System.Windows;
using System.Windows.Media;

namespace CompMs.Graphics.Core.Base
{
    public class ActionContainer{
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
        #region // properties

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

    public class ModifiedFE<TDrawVisual> : DefaultFE<TDrawVisual> where TDrawVisual: IDrawVisual, new()
    {
        public ModifiedFE() : base() { }
        public ModifiedFE(TDrawVisual drawing, ActionContainer ac) : base(drawing, ac) { }
        public ModifiedFE(TDrawVisual drawing, ActionContainer ac, MouseActionSetting actionSetting) : base(drawing, ac, actionSetting) { }

        /*
        public override void Draw() {
            this.visualCollection.Clear();
            if (this.drawing == null) this.drawing = new TDrawVisual();
            this.drawing.ChangeChartArea(this.ActualWidth, this.ActualHeight);
            this.drawingVisual = this.drawing.GetChart();
            this.visualCollection.Add(this.drawingVisual);
            System.Diagnostics.Debug.WriteLine(" ok ");
        }
        */
    }

    public class DefaultFE<TDrawVisual> : FrameworkElement where TDrawVisual: IDrawVisual, new()
    {
        public VisualCollection visualCollection;
        public DrawingVisual drawingVisual;
        public TDrawVisual drawing;
        public ActionContainer ac;
        public MouseActionSetting mouseActionSetting = new MouseActionSetting();

        // Rubber
        private SolidColorBrush rubberRectangleColor = Brushes.DarkGray;
        private Brush rubberRectangleBackGround; // Background for Zooming Regctangle
        private Pen rubberRectangleBorder; // Border for Zooming Rectangle  

        public DefaultFE() { }

        public DefaultFE(TDrawVisual drawing) {
            visualCollection = new VisualCollection(this);
            this.drawing = drawing;
        }

        public DefaultFE(TDrawVisual drawing, ActionContainer ac) {
            this.drawing = drawing;
            this.ac = ac;
            visualCollection = new VisualCollection(this);

            rubberRectangleBorder = new Pen(rubberRectangleColor, 1.0);
            rubberRectangleBorder.Freeze();
            rubberRectangleBackGround = Utility.CombineAlphaAndColor(0.25, rubberRectangleColor);
            rubberRectangleBackGround.Freeze();
        }

        public DefaultFE(TDrawVisual drawing, ActionContainer ac, MouseActionSetting actionSetting) {
            this.drawing = drawing;
            this.ac = ac;
            this.mouseActionSetting = actionSetting;
            visualCollection = new VisualCollection(this);

            rubberRectangleBorder = new Pen(rubberRectangleColor, 1.0);
            rubberRectangleBorder.Freeze();
            rubberRectangleBackGround = Utility.CombineAlphaAndColor(0.25, rubberRectangleColor);
            rubberRectangleBackGround.Freeze();
        }

        public virtual void Draw() {
            this.visualCollection.Clear();
            if (this.drawing == null) this.drawing = new TDrawVisual();
            this.drawing.ChangeChartArea(this.ActualWidth, this.ActualHeight);
            this.drawingVisual = this.drawing.GetChart();
            this.visualCollection.Add(this.drawingVisual);
        }

        public void Draw(float width, float height) {
            this.visualCollection.Clear();
            if (this.drawing == null) this.drawing = new TDrawVisual();
            this.drawing.ChangeChartArea(width, height);
            this.drawingVisual = this.drawing.GetChart();
            this.visualCollection.Add(this.drawingVisual);
        }

        public virtual void ZoomRubberDraw() {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.ac.RightButtonStartClickPoint.X, this.ac.RightButtonStartClickPoint.Y), new Point(this.ac.RightButtonEndClickPoint.X, this.ac.RightButtonEndClickPoint.Y)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public void ResetGraphDisplayRange() {
            this.drawing.Initialize();
            Draw();
        }

        public float[] GetDataPositionOnMousePoint(Point mousePoint) {
            if (this.drawing == null)
                return null;

            float[] peakInformation;
            float scanNumber, retentionTime, mzValue, intensity;

            scanNumber = -1;
            retentionTime = (float)this.drawing.MinX + (float)((mousePoint.X - this.drawing.Area.Margin.Left) / this.drawing.xPacket);
            mzValue = 0;
            intensity = (float)this.drawing.MinY + (float)((this.ActualHeight - mousePoint.Y - this.drawing.Area.Margin.Bottom) / this.drawing.yPacket);

            peakInformation = new float[] { scanNumber, retentionTime, mzValue, intensity };

            return peakInformation;
        }

        public void GraphZoom() {
            // Avoid Miss Double Click Operation
            if (Math.Abs(this.ac.RightButtonStartClickPoint.X - this.ac.RightButtonEndClickPoint.X) < 5 && Math.Abs(this.ac.RightButtonStartClickPoint.Y - this.ac.RightButtonEndClickPoint.Y) < 5)
                return;

            // Avoid Focus exceeding data point resolution            
            if (Math.Abs(this.ac.RightButtonStartClickPoint.X - this.ac.RightButtonEndClickPoint.X) / drawing.xPacket < 0.01) {
                return;
            }

            // Zoom X-Coordinate        
            if (this.ac.RightButtonStartClickPoint.X > this.ac.RightButtonEndClickPoint.X) {
                if (this.ac.RightButtonStartClickPoint.X > this.drawing.Area.Margin.Left) {
                    if (this.ac.RightButtonStartClickPoint.X <= this.ActualWidth - this.drawing.Area.Margin.Right) {
                        this.drawing.MaxX = this.drawing.MinX + (float)((this.ac.RightButtonStartClickPoint.X - this.drawing.Area.Margin.Left) / this.drawing.xPacket);
                    }
                    if (this.ac.RightButtonEndClickPoint.X >= this.drawing.Area.Margin.Left) {
                        this.drawing.MinX = this.drawing.MinX + (float)((this.ac.RightButtonEndClickPoint.X - this.drawing.Area.Margin.Left) / this.drawing.xPacket);
                    }
                }

            }
            else {
                if (this.ac.RightButtonEndClickPoint.X > this.drawing.Area.Margin.Left) {
                    if (this.ac.RightButtonEndClickPoint.X <= this.ActualWidth - this.drawing.Area.Margin.Right) {
                        this.drawing.MaxX = this.drawing.MinX + (float)((this.ac.RightButtonEndClickPoint.X - this.drawing.Area.Margin.Left) / this.drawing.xPacket);
                    }
                    if (this.ac.RightButtonStartClickPoint.X >= this.drawing.Area.Margin.Left) {
                        this.drawing.MinX = this.drawing.MinX + (float)((this.ac.RightButtonStartClickPoint.X - this.drawing.Area.Margin.Left) / this.drawing.xPacket);
                    }
                }
            }

            // Zoom Y-Coordinate               
            if (this.ac.RightButtonStartClickPoint.Y > this.ac.RightButtonEndClickPoint.Y) {
                this.drawing.MaxY = this.drawing.MinY + (float)((this.ActualHeight - this.drawing.Area.Margin.Bottom - this.ac.RightButtonEndClickPoint.Y) / this.drawing.yPacket);
                this.drawing.MinY = this.drawing.MinY + (float)((this.ActualHeight - this.drawing.Area.Margin.Bottom - this.ac.RightButtonStartClickPoint.Y) / this.drawing.yPacket);

            }
            else {
                this.drawing.MaxY = this.drawing.MinY + (float)((this.ActualHeight - this.drawing.Area.Margin.Bottom - this.ac.RightButtonStartClickPoint.Y) / this.drawing.yPacket);
                this.drawing.MinY = this.drawing.MinY + (float)((this.ActualHeight - this.drawing.Area.Margin.Bottom - this.ac.RightButtonEndClickPoint.Y) / this.drawing.yPacket);
            }
        }

        public void GraphScroll() {
            if (this.ac.LeftButtonStartClickPoint.X == -1 || this.ac.LeftButtonStartClickPoint.Y == -1)
                return;
            /*
            if (this.drawing.MinX == null || this.drawing.MaxX == null) {
                this.drawing.MinX = this.drawing.SeriesList.MinX;
                this.drawing.MaxX = this.drawing.SeriesList.MaxX;
            }

            if (this.drawing.MinY == null || this.drawing.MaxY == null) {
                this.drawing.MinY = this.drawing.SeriesList.MinY;
                this.drawing.MaxY = this.drawing.SeriesList.MaxY;
            }
            */
            float durationX = (float)this.drawing.MaxX - (float)this.drawing.MinX;
            double distanceX = 0;

            float durationY;
            double distanceY = 0;

            // X-Direction
            if (this.ac.LeftButtonStartClickPoint.X > this.ac.LeftButtonEndClickPoint.X) {
                distanceX = this.ac.LeftButtonStartClickPoint.X - this.ac.LeftButtonEndClickPoint.X;

                this.drawing.MinX = this.ac.GraphScrollInitialRtMin + (float)(distanceX / this.drawing.xPacket);
                this.drawing.MaxX = this.ac.GraphScrollInitialRtMax + (float)(distanceX / this.drawing.xPacket);

                if (this.drawing.MaxX > this.drawing.SeriesList.MaxX) {
                    this.drawing.MaxX = this.drawing.SeriesList.MaxX;
                    this.drawing.MinX = this.drawing.SeriesList.MaxX - durationX;
                }
            }
            else {
                distanceX = this.ac.LeftButtonEndClickPoint.X - this.ac.LeftButtonStartClickPoint.X;

                this.drawing.MinX = this.ac.GraphScrollInitialRtMin - (float)(distanceX / this.drawing.xPacket);
                this.drawing.MaxX = this.ac.GraphScrollInitialRtMax - (float)(distanceX / this.drawing.xPacket);

                if (this.drawing.MinX < this.drawing.SeriesList.MinX) {
                    this.drawing.MinX = this.drawing.SeriesList.MinX;
                    this.drawing.MaxX = this.drawing.SeriesList.MinX + durationX;
                }
            }

            // Y-Direction
            durationY = (float)this.drawing.MaxY - (float)this.drawing.MinY;
            if (this.ac.LeftButtonStartClickPoint.Y < this.ac.LeftButtonEndClickPoint.Y) {
                distanceY = this.ac.LeftButtonEndClickPoint.Y - this.ac.LeftButtonStartClickPoint.Y;

                this.drawing.MinY = this.ac.GraphScrollInitialIntensityMin + (float)(distanceY / this.drawing.yPacket);
                this.drawing.MaxY = this.ac.GraphScrollInitialIntensityMax + (float)(distanceY / this.drawing.yPacket);

                if (this.drawing.MaxY > this.drawing.SeriesList.MaxY) {
                    this.drawing.MaxY = this.drawing.SeriesList.MaxY;
                    this.drawing.MinY = this.drawing.SeriesList.MaxY - durationY;
                }
            }
            else {
                distanceY = this.ac.LeftButtonStartClickPoint.Y - this.ac.LeftButtonEndClickPoint.Y;

                this.drawing.MinY = this.ac.GraphScrollInitialIntensityMin - (float)(distanceY / this.drawing.yPacket);
                this.drawing.MaxY = this.ac.GraphScrollInitialIntensityMax - (float)(distanceY / this.drawing.yPacket);

                if (this.drawing.MinY < this.drawing.SeriesList.MinY) {
                    this.drawing.MinY = this.drawing.SeriesList.MinY;
                    this.drawing.MaxY = this.drawing.SeriesList.MinY + durationY;
                }
            }
            Draw();
        }


        #region // Required Methods for VisualCollection Object
        protected override int VisualChildrenCount {
            get { return visualCollection.Count; }
        }

        protected override Visual GetVisualChild(int index) {
            if (index < 0 || index >= visualCollection.Count) {
                throw new ArgumentOutOfRangeException();
            }
            return visualCollection[index];
        }
        #endregion // Required Methods for VisualCollection Object
    }
}
