using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Chromatogram.ManualPeakModification;

namespace Rfx.Riken.OsakaUniv.ManualPeakMod
{
    /// <summary>
    /// ChromatogramTicEicUI.xaml の相互作用ロジック
    /// </summary>
    /// 

    public enum PeakModType { Original, Aligned, Picking, TargetPick }

    public partial class PeakModUC : UserControl
    {
        private PeakModFE fe;
        public DrawVisualManualPeakModification drawing;
        public AlignedChromatogramModificationVM VM { get; set; }

        public PeakModType Type { get; set; }
        public List<PeakProperty> PeakPropertyList { get; set; }
       
        // Point for Mouse Event
        public Point currentMousePoint { get; set; } = new Point(-1, -1); // Current Location of Mouse Pointer
        public Point leftButtonStartClickPoint { get; set; } = new Point(-1, -1); // for Mouse Left Button        
        public Point leftButtonEndClickPoint { get; set; } = new Point(-1, -1); // for Mouse Left Button (this coordinates are changed by MouseMove)
        public Point rightButtonStartClickPoint { get; set; } = new Point(-1, -1); // Start Click Point of Mouse Right Button
        public Point rightButtonEndClickPoint { get; set; } = new Point(-1, -1); // End Click Point of Mouse Left Button (this coordinates are changed by MouseMove)

        public bool leftMouseButtonLeftEdgeCapture { get; set; } = false;
        public bool leftMouseButtonRightEdgeCapture { get; set; } = false;
        public bool rightMouseButtonChromatogramUpDownZoom { get; set; } = false;
        public bool rightMouseButtonChromatogramLeftRightZoom { get; set; } = false;
        public bool rightMouseButtonChromatogramRectangleZoom { get; set; } = false;
        public bool keyDownCheck { get; set; } = false;

        // Graph Move Intial Values
        private float graphScrollInitialRtMin = -1; // Initial minXval for Graph Slide Event
        private float graphScrollInitialRtMax = -1; // Initial maxXval for Graph Slide Event
        private float graphScrollInitialIntensityMin = -1; // Initial minYval for Graph Slide Event
        private float graphScrollInitialIntensityMax = -1; // Initial maxYval for Graph Slide Event
        private MouseActionSetting mouseActionSetting = new MouseActionSetting();

        public PeakModUC() {
            InitializeComponent();

            //Property settting
            this.drawing = null;
            this.fe = new PeakModFE(new DrawVisualManualPeakModification(), this);
            this.Content = this.fe;

            this.MouseMove -= new MouseEventHandler(this.userControl_MouseMove);
            this.MouseLeave -= new MouseEventHandler(this.userControl_MouseLeave);
            this.MouseRightButtonUp -= new MouseButtonEventHandler(this.userControl_MouseRightButtonUp);
            this.MouseRightButtonDown -= new MouseButtonEventHandler(this.userControl_MouseRightButtonDown);
            this.MouseDoubleClick -= new MouseButtonEventHandler(this.useerControl_MouseLeftDoubleClick);
            this.MouseWheel -= new MouseWheelEventHandler(this.userControl_MouseWheel);
        }

        public PeakModUC(DrawVisualManualPeakModification drawing) {
            InitializeComponent();

            //Property settting
            this.drawing = drawing;
            this.fe = new PeakModFE(this.drawing, this);
            this.Content = this.fe;

            this.MouseLeftButtonUp -= new MouseButtonEventHandler(this.userControl_MouseLeftButtonUp);
            this.MouseLeftButtonDown -= new MouseButtonEventHandler(this.userControl_MouseLeftButtonDown);
        }

        // for manual peak pick (using chromatogram builder) for single chromatogram data points
        // it means that seriesList should contain one series chromatogram
        public PeakModUC(DrawVisualManualPeakModification drawing, bool isTargetPick) {
            InitializeComponent();

            //Property settting
            this.drawing = drawing;
            this.fe = new PeakModFE(this.drawing, this);
            this.Content = this.fe;
            this.Type = PeakModType.TargetPick;
            this.drawing.IsTargetManualPickMode = true;
            RefreshUI();
        }

        public PeakModUC(AlignedChromatogramModificationVM vm, DrawVisualManualPeakModification drawing, MouseActionSetting setting, PeakModType type = PeakModType.Original, List<PeakProperty> peakPropertyList = null) {
            InitializeComponent();

            //Property settting
            this.VM = vm;
            this.drawing = drawing;
            this.mouseActionSetting = setting;
            this.fe = new PeakModFE(this.drawing, this);
            this.Content = this.fe;
            this.Type = type;
            this.PeakPropertyList = peakPropertyList;

            if (!setting.CanMouseAction) {
                this.MouseMove -= new MouseEventHandler(this.userControl_MouseMove);
                this.MouseLeave -= new MouseEventHandler(this.userControl_MouseLeave);
                this.MouseRightButtonUp -= new MouseButtonEventHandler(this.userControl_MouseRightButtonUp);
                this.MouseRightButtonDown -= new MouseButtonEventHandler(this.userControl_MouseRightButtonDown);
                this.MouseLeftButtonUp -= new MouseButtonEventHandler(this.userControl_MouseLeftButtonUp);
                this.MouseLeftButtonDown -= new MouseButtonEventHandler(this.userControl_MouseLeftButtonDown);
                this.MouseDoubleClick -= new MouseButtonEventHandler(this.useerControl_MouseLeftDoubleClick);
                this.MouseWheel -= new MouseWheelEventHandler(this.userControl_MouseWheel);
            }
            else if (!setting.CanZoomRubber) {
                this.MouseRightButtonUp -= new MouseButtonEventHandler(this.userControl_MouseRightButtonUp);
                this.MouseRightButtonDown -= new MouseButtonEventHandler(this.userControl_MouseRightButtonDown);
                this.MouseMove -= new MouseEventHandler(this.userControl_MouseMove);
                this.MouseLeave -= new MouseEventHandler(this.userControl_MouseLeave);
            }
            if (setting.FixMaxX && setting.FixMinX && setting.FixMaxY && setting.FixMinY) {
                this.MouseWheel -= new MouseWheelEventHandler(this.userControl_MouseWheel);
            }
            RefreshUI();
        }



        public void RefreshUI() {
            this.fe.ReflectMouseProp();
            this.fe.Draw();
        }

        private void userControl_SizeChanged(object sender, SizeChangedEventArgs e) {
            RefreshUI();
        }

        private void useerControl_MouseLeftDoubleClick(object sender, MouseButtonEventArgs e) {
            if (this.drawing == null) return;
            if (e.StylusDevice != null) return;// Avoid Touch Event

            // Set Mouse Position
            if (e.LeftButton == MouseButtonState.Pressed) {
                this.currentMousePoint = Mouse.GetPosition(this);

                if (this.currentMousePoint.X > this.drawing.Area.Margin.Left && this.currentMousePoint.Y > this.ActualHeight - this.drawing.Area.Margin.Bottom) {
                    this.drawing.MaxX = this.drawing.SeriesList.MaxX;
                    this.drawing.MinX = this.drawing.SeriesList.MinX;
                    RefreshUI();
                }
                else if (this.currentMousePoint.X < this.drawing.Area.Margin.Left && this.currentMousePoint.Y < this.ActualHeight - this.drawing.Area.Margin.Bottom) {
                    this.drawing.MaxY = this.drawing.SeriesList.MaxY;
                    this.drawing.MinY = this.drawing.SeriesList.MinY;
                    RefreshUI();
                }
                else if (this.currentMousePoint.X > this.drawing.Area.Margin.Left && this.currentMousePoint.Y > this.drawing.Area.Margin.Top) {
                    this.fe.ResetGraphDisplayRange();
                }
            }
            else if (e.RightButton == MouseButtonState.Pressed) {
                if (this.drawing.IsTargetManualPickMode) {

                    Debug.WriteLine(Keyboard.Modifiers);
                    if (Keyboard.Modifiers.ToString() == "Control, Shift") {
                        this.drawing.IsPeakDeleteCommand = true;
                        RefreshUI();

                        this.drawing.IsPeakDeleteCommand = false;
                        RefreshUI();
                    }
                    else {
                        this.drawing.IsMouseDoubleClicked = true;
                        RefreshUI();

                        this.drawing.IsMouseDoubleClicked = false;
                        RefreshUI();
                    }

                  
                }
            }
        }

        private void userControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            if (this.drawing == null) return;

            // Set Mouse Position
            this.rightButtonStartClickPoint = Mouse.GetPosition(this);

            if (this.rightButtonStartClickPoint.X <= this.drawing.Area.Margin.Left &&
                this.rightButtonStartClickPoint.Y > this.drawing.Area.Margin.Top &&
                this.rightButtonStartClickPoint.Y < this.ActualHeight - this.drawing.Area.Margin.Bottom)
                this.rightMouseButtonChromatogramUpDownZoom = true;
            else if (this.rightButtonStartClickPoint.X > this.drawing.Area.Margin.Left &&
                this.rightButtonStartClickPoint.Y >= this.ActualHeight - this.drawing.Area.Margin.Bottom)
                this.rightMouseButtonChromatogramLeftRightZoom = true;
            else {
                if (this.drawing.IsTargetManualPickMode && Keyboard.Modifiers == ModifierKeys.Shift) {
                    this.rightMouseButtonChromatogramRectangleZoom = true;
                }
                else if (this.drawing.IsTargetManualPickMode) {
                //if (this.drawing.IsTargetManualPickMode && Keyboard.Modifiers == ModifierKeys.Shift) {
                    this.drawing.IsShiftRightFolding = true;
                    RefreshUI();
                }
                else {
                    this.rightMouseButtonChromatogramRectangleZoom = true;
                }
            }
        }

        private void userControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (this.drawing == null) return;
            if (e.StylusDevice != null) return; // Avoid Touch Event

            Debug.WriteLine("left button down");

            // Set Mouse Position
            this.leftButtonStartClickPoint = Mouse.GetPosition(this);
            this.graphScrollInitialRtMin = (float)this.drawing.MinX;
            this.graphScrollInitialRtMax = drawing.MaxX;
            this.graphScrollInitialIntensityMin = drawing.MinY;
            this.graphScrollInitialIntensityMax = drawing.MaxY;

            if (this.drawing.IsTargetManualPickMode) {
                RefreshUI();
            }
        }

        private void userControl_MouseMove(object sender, MouseEventArgs e) {
            if (this.drawing == null) return;

            // Store Current Mouse Point
            this.currentMousePoint = Mouse.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed) {
                this.leftButtonEndClickPoint = Mouse.GetPosition(this);

                //                if (this.rtDiffViewModel.EditMode == ChromatogramEditMode.Display) {
                if (!this.drawing.LeftMouseButtonLeftEdgeCapture && !this.drawing.LeftMouseButtonRightEdgeCapture) {
                    this.fe.GraphScroll();
                    return;
                }
                //              }
            }

            if (this.drawing.IsTargetManualPickMode && this.drawing.IsShiftRightFolding) {
                RefreshUI();
                return;
            }

            if (e.RightButton == MouseButtonState.Pressed) {
                if (this.rightButtonStartClickPoint.X != -1 && this.rightButtonStartClickPoint.Y != -1) {

                    if (this.rightMouseButtonChromatogramRectangleZoom) {
                        this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                        //                if (this.rtDiffViewModel.EditMode == ChromatogramEditMode.Display) {
                        if (this.Type == PeakModType.Aligned || this.Type == PeakModType.Original) {
                            //Debug.WriteLine("Right mac");
                            fe.RectangleRubberDraw();
                        }
                        else
                            this.fe.ZoomRubberDraw();
                        return;
                        //              }
                    }
                    else if (this.rightMouseButtonChromatogramLeftRightZoom) {
                        float[] peakInformation = this.fe.getDataPositionOnMousePoint(this.rightButtonStartClickPoint);
                        if (peakInformation == null) return;

                        float mousePointRt = 0;
                        float newMinRt = float.MaxValue;
                        float newMaxRt = float.MinValue;

                        mousePointRt = peakInformation[1];

                        if (Mouse.GetPosition(this).X - this.rightButtonEndClickPoint.X > 0) {
                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);

                            newMinRt = mousePointRt - (float)((mousePointRt - (float)this.drawing.MinX) * 0.98);
                            newMaxRt = mousePointRt + (float)(((float)this.drawing.MaxX - mousePointRt) * 0.98);

                            if (newMinRt + 0.0001 < newMaxRt) {
                                this.drawing.MinX = newMinRt;
                                this.drawing.MaxX = newMaxRt;
                            }
                        }
                        else if (Mouse.GetPosition(this).X - this.rightButtonEndClickPoint.X < 0) {

                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);

                            newMinRt = mousePointRt - (float)((mousePointRt - (float)this.drawing.MinX) * 1.02);
                            newMaxRt = mousePointRt + (float)(((float)this.drawing.MaxX - mousePointRt) * 1.02);

                            if (newMinRt < this.drawing.SeriesList.MinX)
                                this.drawing.MinX = this.drawing.SeriesList.MinX;
                            else
                                this.drawing.MinX = newMinRt;

                            if (newMaxRt > this.drawing.SeriesList.MaxX)
                                this.drawing.MaxX = this.drawing.SeriesList.MaxX;
                            else
                                this.drawing.MaxX = newMaxRt;
                        }
                        this.fe.Draw();
                    }
                    else if (this.rightMouseButtonChromatogramUpDownZoom) {

                        float[] peakInformation = this.fe.getDataPositionOnMousePoint(this.rightButtonStartClickPoint);
                        if (peakInformation == null) return;

                        float mousePointIntensity = 0;
                        float newMinIntensity = float.MaxValue;
                        float newMaxIntensity = float.MinValue;

                        mousePointIntensity = peakInformation[3];

                        // Mouse On Y-Axis                
                        if (Mouse.GetPosition(this).Y - this.rightButtonEndClickPoint.Y < 0) {
                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);

                            if ((float)this.drawing.MaxY < 0.0001)
                                return;

                            newMaxIntensity = (float)this.drawing.MaxY * (float)0.98;
                            if (newMaxIntensity > 0 && newMaxIntensity > this.drawing.MinY)
                                this.drawing.MaxY = newMaxIntensity;
                        }
                        else if (Mouse.GetPosition(this).Y - this.rightButtonEndClickPoint.Y > 0) {
                            this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                            this.drawing.MaxY = this.drawing.MaxY * 1.02F;

                            if (this.drawing.MaxY > this.drawing.SeriesList.MaxY)
                                this.drawing.MaxY = this.drawing.SeriesList.MaxY;
                        }
                        this.fe.Draw();
                    }
                }
                else {
                    this.rightButtonEndClickPoint = new Point(-1, -1);
                }
            }

            if (this.drawing.IsTargetManualPickMode) {
                RefreshUI();
            }
        }

        private void userControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (this.drawing == null) return;

            if (this.drawing.IsTargetManualPickMode && this.drawing.IsShiftRightFolding) {
                this.drawing.IsShiftRightFolding = false;
                this.drawing.IsShiftRightReleased = true;

                RefreshUI();

                this.drawing.IsShiftRightReleased = false;

                RefreshUI();

                return;
            }

            if (this.rightMouseButtonChromatogramRectangleZoom) {
                this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                var maxX = -1.0;
                var minX = -1.0;
                // Zoom X-Coordinate        
                if (this.RightButtonStartClickPoint.X > RightButtonEndClickPoint.X) {
                    if (RightButtonStartClickPoint.X > this.drawing.Area.Margin.Left) {
                        if (RightButtonStartClickPoint.X <= this.ActualWidth - this.drawing.Area.Margin.Right) {
                            maxX = this.drawing.MinX + (float)((RightButtonStartClickPoint.X - this.drawing.Area.Margin.Left) / this.drawing.xPacket);
                        }
                        if (RightButtonEndClickPoint.X >= this.drawing.Area.Margin.Left) {
                            minX = this.drawing.MinX + (float)((RightButtonEndClickPoint.X - this.drawing.Area.Margin.Left) / this.drawing.xPacket);
                        }
                    }

                }
                else {
                    if (RightButtonEndClickPoint.X > this.drawing.Area.Margin.Left) {
                        if (RightButtonEndClickPoint.X <= this.ActualWidth - this.drawing.Area.Margin.Right) {
                            maxX = this.drawing.MinX + (float)((RightButtonEndClickPoint.X - this.drawing.Area.Margin.Left) / this.drawing.xPacket);
                        }
                        if (RightButtonStartClickPoint.X >= this.drawing.Area.Margin.Left) {
                            minX = this.drawing.MinX + (float)((RightButtonStartClickPoint.X - this.drawing.Area.Margin.Left) / this.drawing.xPacket);
                        }
                    }
                }


                if (this.Type == PeakModType.Original) {
                    Mouse.OverrideCursor = Cursors.Wait;
                    Utility.ChangeAlignedRtProperty(this.PeakPropertyList, minX, maxX);
                    fe.Draw();
                    this.VM.UpdateAlignedChromUC();
                    Mouse.OverrideCursor = null;
                }
                else if(this.Type == PeakModType.Aligned) {
                    Mouse.OverrideCursor = Cursors.Wait;
                    Utility.ModifyPeakEdge(this.PeakPropertyList, minX, maxX);
                    this.VM.UpdateAlignedChromUC();
                    this.VM.UpdatePickingChromUC();
                    Mouse.OverrideCursor = null;
                }
                else {
                    this.rightButtonEndClickPoint = Mouse.GetPosition(this);
                    this.fe.GraphZoom();
                    this.fe.Draw();
                }
            }

            // Reset Mouse Position
            this.rightButtonStartClickPoint = new Point(-1, -1);
            this.rightButtonEndClickPoint = new Point(-1, -1);
            this.rightMouseButtonChromatogramLeftRightZoom = false;
            this.rightMouseButtonChromatogramRectangleZoom = false;
            this.rightMouseButtonChromatogramUpDownZoom = false;

        }

        private void userControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (this.drawing == null) return;
            this.fe.Draw();

            // Reset Mouse Position
            this.leftButtonStartClickPoint = new Point(-1, -1);
            this.leftButtonEndClickPoint = new Point(-1, -1);

            // Reset capture
            this.LeftMouseButtonLeftEdgeCapture = false;
            this.LeftMouseButtonRightEdgeCapture = false;

            Debug.WriteLine("left button up");


            if (this.drawing.IsTargetManualPickMode) {
                this.drawing.IsMouseLeftUpDone = true;
                RefreshUI();
                this.drawing.IsMouseLeftUpDone = false;
                RefreshUI();
            }

        }

        private void userControl_MouseLeave(object sender, MouseEventArgs e) {
            if (this.drawing == null) return;
            //this.rightButtonEndClickPoint.X = -1;
            this.drawing.IsShiftRightFolding = false;
            this.fe.Draw();
        }

        private void userControl_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (this.drawing == null) return;

            this.currentMousePoint = Mouse.GetPosition(this);

            var peakInformation = this.fe.getDataPositionOnMousePoint(this.currentMousePoint);
            if (peakInformation == null) return;

            float newMinX = float.MaxValue;
            float newMaxX = float.MinValue;
            float newMinY = float.MaxValue;
            float newMaxY = float.MinValue;

            float x = peakInformation[1];
            float y = peakInformation[3];

            if (e.Delta > 0) {
                newMinX = (float)x - (float)((x - (float)this.drawing.MinX) * 0.9);
                newMaxX = (float)x + (float)(((float)this.drawing.MaxX - x) * 0.9);
                newMinY = (float)y - (float)((y - (float)this.drawing.MinY) * 0.9);
                newMaxY = (float)y + (float)(((float)this.drawing.MaxY - y) * 0.9);
            }
            else {
                newMinX = (float)x - (float)((x - (float)this.drawing.MinX) * 1.1);
                newMaxX = (float)x + (float)(((float)this.drawing.MaxX - x) * 1.1);
                newMinY = (float)y - (float)((y - (float)this.drawing.MinY) * 1.1);
                newMaxY = (float)y + (float)(((float)this.drawing.MaxY - y) * 1.1);
            }


            if (this.currentMousePoint.X > this.drawing.Area.Margin.Left && this.currentMousePoint.Y > this.ActualHeight - this.drawing.Area.Margin.Bottom) {
                if (newMinX < this.drawing.SeriesList.MinX) {
                    this.drawing.MinX = this.drawing.SeriesList.MinX;
                }
                else {
                    if (!this.mouseActionSetting.FixMinX)
                        this.drawing.MinX = newMinX;
                }

                if (newMaxX > this.drawing.SeriesList.MaxX) {
                    this.drawing.MaxX = this.drawing.SeriesList.MaxX;
                }
                else {
                    if (!this.mouseActionSetting.FixMaxX)
                        this.drawing.MaxX = newMaxX;
                }

            }
            else if (this.currentMousePoint.X <= this.drawing.Area.Margin.Left && this.currentMousePoint.Y > this.drawing.Area.Margin.Top && this.currentMousePoint.Y < this.ActualHeight - this.drawing.Area.Margin.Bottom) {
                if (newMaxY > this.drawing.SeriesList.MaxY) {
                    this.drawing.MaxY = this.drawing.SeriesList.MaxY;
                }
                else {
                    if (!this.mouseActionSetting.FixMaxY)
                        this.drawing.MaxY = newMaxY;
                }

                //if (newMinY < this.drawing.SeriesList.MinY) {
                //    this.drawing.MinY = this.drawing.SeriesList.MinY;
                //}
                //else {
                //    if (!this.mouseActionSetting.FixMinY)
                //        this.drawing.MinY = newMinY;
                //}

            }

            RefreshUI();
        }

     
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
}
