using CompMs.App.Msdial.View.PeakCuration;
using CompMs.Graphics.Chromatogram.ManualPeakModification;
using System;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.PeakCuration
{
    public class PeakModFELegacy : FrameworkElement {
        private VisualCollection visualCollection;
        private DrawingVisual? drawingVisual;
        private DrawVisualManualPeakModification drawing;
        private PeakModUCLegacy uc;

        // Rubber
        private SolidColorBrush rubberRectangleColor = Brushes.DarkGray;
        private Brush rubberRectangleBackGround; // Background for Zooming Regctangle
        private Pen rubberRectangleBorder; // Border for Zooming Rectangle  

        public PeakModFELegacy(DrawVisualManualPeakModification drawing, PeakModUCLegacy uc) {
            this.uc = uc;
            visualCollection = new VisualCollection(this);
            this.drawing = drawing;

            rubberRectangleBorder = new Pen(rubberRectangleColor, 1.0);
            rubberRectangleBorder.Freeze();
            rubberRectangleBackGround = CompMs.Graphics.Core.Base.Utility.CombineAlphaAndColor(0.25, rubberRectangleColor);
            rubberRectangleBackGround.Freeze();
        }

        public void Draw() {
            this.visualCollection.Clear();
            if (this.drawing == null) return;
            this.drawing.ChangeChartArea(this.ActualWidth, this.ActualHeight);
            this.drawingVisual = this.drawing.GetChart();
            this.visualCollection.Add(this.drawingVisual);
        }

        public void ReflectMouseProp() {
            if (this.drawing is null) {
            }
            else {
                this.drawing.CurrentMousePoint = this.uc.CurrentMousePoint;
                this.drawing.LeftButtonStartClickPoint = this.uc.LeftButtonStartClickPoint;
                this.drawing.LeftButtonEndClickPoint = this.uc.LeftButtonEndClickPoint;
                this.drawing.RightButtonStartClickPoint = this.uc.RightButtonStartClickPoint;
            }
        }

        public void Draw(float width, float height) {
            this.visualCollection.Clear();
            this.drawing.ChangeChartArea(width, height);
            this.drawingVisual = this.drawing.GetChart();
            this.visualCollection.Add(this.drawingVisual);
        }

        public void ZoomRubberDraw() {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();


            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder,
                new Rect(
                    new Point(this.uc.RightButtonStartClickPoint.X, this.uc.RightButtonStartClickPoint.Y),
                    new Point(this.uc.RightButtonEndClickPoint.X, this.uc.RightButtonEndClickPoint.Y)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public void RectangleRubberDraw() {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();

            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder,
                new Rect(
                    new Point(this.uc.RightButtonStartClickPoint.X, (this.drawing.Area.Height - this.drawing.Area.Margin.Bottom + this.drawing.Area.LabelSpace.Bottom)),
                    new Point(this.uc.RightButtonEndClickPoint.X, this.drawing.Area.Margin.Top)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public void ResetGraphDisplayRange() {
            this.drawing.Initialize();
            Draw();
        }

        public float[]? getDataPositionOnMousePoint(Point mousePoint) {
            if (this.drawing is null)
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
            if (Math.Abs(this.uc.RightButtonStartClickPoint.X - this.uc.RightButtonEndClickPoint.X) < 5 && Math.Abs(this.uc.RightButtonStartClickPoint.Y - this.uc.RightButtonEndClickPoint.Y) < 5)
                return;

            // Avoid Focus exceeding data point resolution            
            if (Math.Abs(this.uc.RightButtonStartClickPoint.X - this.uc.RightButtonEndClickPoint.X) / drawing.xPacket < 0.01) {
                return;
            }

            // Zoom X-Coordinate        
            if (this.uc.RightButtonStartClickPoint.X > this.uc.RightButtonEndClickPoint.X) {
                if (this.uc.RightButtonStartClickPoint.X > this.drawing.Area.Margin.Left) {
                    if (this.uc.RightButtonStartClickPoint.X <= this.ActualWidth - this.drawing.Area.Margin.Right) {
                        this.drawing.MaxX = this.drawing.MinX + (float)((this.uc.RightButtonStartClickPoint.X - this.drawing.Area.Margin.Left) / this.drawing.xPacket);
                    }
                    if (this.uc.RightButtonEndClickPoint.X >= this.drawing.Area.Margin.Left) {
                        this.drawing.MinX = this.drawing.MinX + (float)((this.uc.RightButtonEndClickPoint.X - this.drawing.Area.Margin.Left) / this.drawing.xPacket);
                    }
                }

            }
            else {
                if (this.uc.RightButtonEndClickPoint.X > this.drawing.Area.Margin.Left) {
                    if (this.uc.RightButtonEndClickPoint.X <= this.ActualWidth - this.drawing.Area.Margin.Right) {
                        this.drawing.MaxX = this.drawing.MinX + (float)((this.uc.RightButtonEndClickPoint.X - this.drawing.Area.Margin.Left) / this.drawing.xPacket);
                    }
                    if (this.uc.RightButtonStartClickPoint.X >= this.drawing.Area.Margin.Left) {
                        this.drawing.MinX = this.drawing.MinX + (float)((this.uc.RightButtonStartClickPoint.X - this.drawing.Area.Margin.Left) / this.drawing.xPacket);
                    }
                }
            }

            // Zoom Y-Coordinate               
            if (this.uc.RightButtonStartClickPoint.Y > this.uc.RightButtonEndClickPoint.Y) {
                this.drawing.MaxY = this.drawing.MinY + (float)((this.ActualHeight - this.drawing.Area.Margin.Bottom - this.uc.RightButtonEndClickPoint.Y) / this.drawing.yPacket);
                this.drawing.MinY = this.drawing.MinY + (float)((this.ActualHeight - this.drawing.Area.Margin.Bottom - this.uc.RightButtonStartClickPoint.Y) / this.drawing.yPacket);

            }
            else {
                this.drawing.MaxY = this.drawing.MinY + (float)((this.ActualHeight - this.drawing.Area.Margin.Bottom - this.uc.RightButtonStartClickPoint.Y) / this.drawing.yPacket);
                this.drawing.MinY = this.drawing.MinY + (float)((this.ActualHeight - this.drawing.Area.Margin.Bottom - this.uc.RightButtonEndClickPoint.Y) / this.drawing.yPacket);
            }
        }

        public void GraphScroll() {
            if (this.uc.LeftButtonStartClickPoint.X == -1 || this.uc.LeftButtonStartClickPoint.Y == -1)
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
            if (this.uc.LeftButtonStartClickPoint.X > this.uc.LeftButtonEndClickPoint.X) {
                distanceX = this.uc.LeftButtonStartClickPoint.X - this.uc.LeftButtonEndClickPoint.X;

                this.drawing.MinX = this.uc.GraphScrollInitialRtMin + (float)(distanceX / this.drawing.xPacket);
                this.drawing.MaxX = this.uc.GraphScrollInitialRtMax + (float)(distanceX / this.drawing.xPacket);

                if (this.drawing.MaxX > this.drawing.SeriesList.MaxX) {
                    this.drawing.MaxX = this.drawing.SeriesList.MaxX;
                    this.drawing.MinX = this.drawing.SeriesList.MaxX - durationX;
                }
            }
            else {
                distanceX = this.uc.LeftButtonEndClickPoint.X - this.uc.LeftButtonStartClickPoint.X;

                this.drawing.MinX = this.uc.GraphScrollInitialRtMin - (float)(distanceX / this.drawing.xPacket);
                this.drawing.MaxX = this.uc.GraphScrollInitialRtMax - (float)(distanceX / this.drawing.xPacket);

                if (this.drawing.MinX < this.drawing.SeriesList.MinX) {
                    this.drawing.MinX = this.drawing.SeriesList.MinX;
                    this.drawing.MaxX = this.drawing.SeriesList.MinX + durationX;
                }
            }

            // Y-Direction
            durationY = (float)this.drawing.MaxY - (float)this.drawing.MinY;
            if (this.uc.LeftButtonStartClickPoint.Y < this.uc.LeftButtonEndClickPoint.Y) {
                distanceY = this.uc.LeftButtonEndClickPoint.Y - this.uc.LeftButtonStartClickPoint.Y;

                this.drawing.MinY = this.uc.GraphScrollInitialIntensityMin + (float)(distanceY / this.drawing.yPacket);
                this.drawing.MaxY = this.uc.GraphScrollInitialIntensityMax + (float)(distanceY / this.drawing.yPacket);

                if (this.drawing.MaxY > this.drawing.SeriesList.MaxY) {
                    this.drawing.MaxY = this.drawing.SeriesList.MaxY;
                    this.drawing.MinY = this.drawing.SeriesList.MaxY - durationY;
                }
            }
            else {
                distanceY = this.uc.LeftButtonStartClickPoint.Y - this.uc.LeftButtonEndClickPoint.Y;

                this.drawing.MinY = this.uc.GraphScrollInitialIntensityMin - (float)(distanceY / this.drawing.yPacket);
                this.drawing.MaxY = this.uc.GraphScrollInitialIntensityMax - (float)(distanceY / this.drawing.yPacket);

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
