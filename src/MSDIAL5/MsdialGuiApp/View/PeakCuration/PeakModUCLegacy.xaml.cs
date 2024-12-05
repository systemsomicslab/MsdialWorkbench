using CompMs.App.Msdial.Model.PeakCuration;
using CompMs.App.Msdial.ViewModel.PeakCuration;
using CompMs.Graphics.Chromatogram.ManualPeakModification;
using CompMs.Graphics.Core.Base;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CompMs.App.Msdial.View.PeakCuration
{
    public enum PeakModType { Original, Aligned, Picking, TargetPick }
    /// <summary>
    /// Interaction logic for PeakModUCLegacy.xaml
    /// </summary>
    public partial class PeakModUCLegacy : UserControl {
        private readonly PeakModFELegacy _fe;
        public DrawVisualManualPeakModification? drawing;
        private AlignedChromatogramModificationViewModelLegacy? VM { get; set; }

        public PeakModType Type { get; set; }
        public List<PeakPropertyLegacy> PeakPropertyList { get; set; }

        // Point for Mouse Event
        private Point _currentMousePoint = new Point(-1, -1); // Current Location of Mouse Pointer
        private Point _leftButtonStartClickPoint = new Point(-1, -1); // for Mouse Left Button        
        private Point _leftButtonEndClickPoint = new Point(-1, -1); // for Mouse Left Button (this coordinates are changed by MouseMove)
        private Point _rightButtonStartClickPoint = new Point(-1, -1); // Start Click Point of Mouse Right Button
        private Point _rightButtonEndClickPoint = new Point(-1, -1); // End Click Point of Mouse Left Button (this coordinates are changed by MouseMove)

        private bool _leftMouseButtonLeftEdgeCapture = false;
        private bool _leftMouseButtonRightEdgeCapture = false;
        private bool _rightMouseButtonChromatogramUpDownZoom = false;
        private bool _rightMouseButtonChromatogramLeftRightZoom = false;
        private bool _rightMouseButtonChromatogramRectangleZoom = false;
        private bool _keyDownCheck = false;

        // Graph Move Intial Values
        private float _graphScrollInitialRtMin = -1; // Initial minXval for Graph Slide Event
        private float _graphScrollInitialRtMax = -1; // Initial maxXval for Graph Slide Event
        private float _graphScrollInitialIntensityMin = -1; // Initial minYval for Graph Slide Event
        private float _graphScrollInitialIntensityMax = -1; // Initial maxYval for Graph Slide Event
        private readonly MouseActionSetting _mouseActionSetting = new MouseActionSetting();

        public PeakModUCLegacy() {
            InitializeComponent();

            //Property settting
            this.drawing = null;
            this._fe = new PeakModFELegacy(new DrawVisualManualPeakModification(), this);
            this.Content = this._fe;
            PeakPropertyList = new List<PeakPropertyLegacy>(0);

            this.MouseMove -= new MouseEventHandler(this.UserControl_MouseMove);
            this.MouseLeave -= new MouseEventHandler(this.UserControl_MouseLeave);
            this.MouseRightButtonUp -= new MouseButtonEventHandler(this.UserControl_MouseRightButtonUp);
            this.MouseRightButtonDown -= new MouseButtonEventHandler(this.UserControl_MouseRightButtonDown);
            this.MouseDoubleClick -= new MouseButtonEventHandler(this.UserControl_MouseLeftDoubleClick);
            this.MouseWheel -= new MouseWheelEventHandler(this.UserControl_MouseWheel);
        }

        public PeakModUCLegacy(DrawVisualManualPeakModification drawing) {
            InitializeComponent();

            //Property settting
            this.drawing = drawing;
            this._fe = new PeakModFELegacy(this.drawing, this);
            this.Content = this._fe;
            PeakPropertyList = new List<PeakPropertyLegacy>(0);

            this.MouseLeftButtonUp -= new MouseButtonEventHandler(this.UserControl_MouseLeftButtonUp);
            this.MouseLeftButtonDown -= new MouseButtonEventHandler(this.UserControl_MouseLeftButtonDown);
        }

        // for manual peak pick (using chromatogram builder) for single chromatogram data points
        // it means that seriesList should contain one series chromatogram
        public PeakModUCLegacy(DrawVisualManualPeakModification drawing, bool isTargetPick) {
            InitializeComponent();

            _ = isTargetPick;
            //Property settting
            this.drawing = drawing;
            this._fe = new PeakModFELegacy(this.drawing, this);
            this.Content = this._fe;
            this.Type = PeakModType.TargetPick;
            this.drawing.IsTargetManualPickMode = true;
            PeakPropertyList = new List<PeakPropertyLegacy>(0);
            RefreshUI();
        }

        internal PeakModUCLegacy(AlignedChromatogramModificationViewModelLegacy vm, 
            DrawVisualManualPeakModification drawing, 
            MouseActionSetting setting, PeakModType type = PeakModType.Original, 
            List<PeakPropertyLegacy>? peakPropertyList = null) {
            InitializeComponent();

            //Property settting
            this.VM = vm;
            this.drawing = drawing;
            this._mouseActionSetting = setting;
            this._fe = new PeakModFELegacy(this.drawing, this);
            this.Content = this._fe;
            this.Type = type;
            this.PeakPropertyList = peakPropertyList ?? new List<PeakPropertyLegacy>(0);

            if (!setting.CanMouseAction) {
                this.MouseMove -= new MouseEventHandler(this.UserControl_MouseMove);
                this.MouseLeave -= new MouseEventHandler(this.UserControl_MouseLeave);
                this.MouseRightButtonUp -= new MouseButtonEventHandler(this.UserControl_MouseRightButtonUp);
                this.MouseRightButtonDown -= new MouseButtonEventHandler(this.UserControl_MouseRightButtonDown);
                this.MouseLeftButtonUp -= new MouseButtonEventHandler(this.UserControl_MouseLeftButtonUp);
                this.MouseLeftButtonDown -= new MouseButtonEventHandler(this.UserControl_MouseLeftButtonDown);
                this.MouseDoubleClick -= new MouseButtonEventHandler(this.UserControl_MouseLeftDoubleClick);
                this.MouseWheel -= new MouseWheelEventHandler(this.UserControl_MouseWheel);
            }
            else if (!setting.CanZoomRubber) {
                this.MouseRightButtonUp -= new MouseButtonEventHandler(this.UserControl_MouseRightButtonUp);
                this.MouseRightButtonDown -= new MouseButtonEventHandler(this.UserControl_MouseRightButtonDown);
                this.MouseMove -= new MouseEventHandler(this.UserControl_MouseMove);
                this.MouseLeave -= new MouseEventHandler(this.UserControl_MouseLeave);
            }
            if (setting.FixMaxX && setting.FixMinX && setting.FixMaxY && setting.FixMinY) {
                this.MouseWheel -= new MouseWheelEventHandler(this.UserControl_MouseWheel);
            }
            RefreshUI();
        }



        public void RefreshUI() {
            this._fe.ReflectMouseProp();
            this._fe.Draw();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e) {
            RefreshUI();
        }

        private void UserControl_MouseLeftDoubleClick(object sender, MouseButtonEventArgs e) {
            if (this.drawing == null) return;
            if (e.StylusDevice != null) return;// Avoid Touch Event

            // Set Mouse Position
            if (e.LeftButton == MouseButtonState.Pressed) {
                this._currentMousePoint = Mouse.GetPosition(this);

                if (this._currentMousePoint.X > this.drawing.Area.Margin.Left && this._currentMousePoint.Y > this.ActualHeight - this.drawing.Area.Margin.Bottom) {
                    this.drawing.MaxX = this.drawing.SeriesList.MaxX;
                    this.drawing.MinX = this.drawing.SeriesList.MinX;
                    RefreshUI();
                }
                else if (this._currentMousePoint.X < this.drawing.Area.Margin.Left && this._currentMousePoint.Y < this.ActualHeight - this.drawing.Area.Margin.Bottom) {
                    this.drawing.MaxY = this.drawing.SeriesList.MaxY;
                    this.drawing.MinY = this.drawing.SeriesList.MinY;
                    RefreshUI();
                }
                else if (this._currentMousePoint.X > this.drawing.Area.Margin.Left && this._currentMousePoint.Y > this.drawing.Area.Margin.Top) {
                    this._fe.ResetGraphDisplayRange();
                }
            }
            else if (e.RightButton == MouseButtonState.Pressed) {
                if (this.drawing.IsTargetManualPickMode) {

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

        private void UserControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            if (this.drawing == null) return;

            // Set Mouse Position
            this._rightButtonStartClickPoint = Mouse.GetPosition(this);

            if (this._rightButtonStartClickPoint.X <= this.drawing.Area.Margin.Left &&
                this._rightButtonStartClickPoint.Y > this.drawing.Area.Margin.Top &&
                this._rightButtonStartClickPoint.Y < this.ActualHeight - this.drawing.Area.Margin.Bottom)
                this._rightMouseButtonChromatogramUpDownZoom = true;
            else if (this._rightButtonStartClickPoint.X > this.drawing.Area.Margin.Left &&
                this._rightButtonStartClickPoint.Y >= this.ActualHeight - this.drawing.Area.Margin.Bottom)
                this._rightMouseButtonChromatogramLeftRightZoom = true;
            else {
                if (this.drawing.IsTargetManualPickMode && Keyboard.Modifiers == ModifierKeys.Shift) {
                    this._rightMouseButtonChromatogramRectangleZoom = true;
                }
                else if (this.drawing.IsTargetManualPickMode) {
                    //if (this.drawing.IsTargetManualPickMode && Keyboard.Modifiers == ModifierKeys.Shift) {
                    this.drawing.IsShiftRightFolding = true;
                    RefreshUI();
                }
                else {
                    this._rightMouseButtonChromatogramRectangleZoom = true;
                }
            }
        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (this.drawing == null) return;
            if (e.StylusDevice != null) return; // Avoid Touch Event


            // Set Mouse Position
            this._leftButtonStartClickPoint = Mouse.GetPosition(this);
            this._graphScrollInitialRtMin = (float)this.drawing.MinX;
            this._graphScrollInitialRtMax = drawing.MaxX;
            this._graphScrollInitialIntensityMin = drawing.MinY;
            this._graphScrollInitialIntensityMax = drawing.MaxY;

            if (this.drawing.IsTargetManualPickMode) {
                RefreshUI();
            }
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e) {
            if (this.drawing == null) return;

            // Store Current Mouse Point
            this._currentMousePoint = Mouse.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed) {
                this._leftButtonEndClickPoint = Mouse.GetPosition(this);

                //                if (this.rtDiffViewModel.EditMode == ChromatogramEditMode.Display) {
                if (!this.drawing.LeftMouseButtonLeftEdgeCapture && !this.drawing.LeftMouseButtonRightEdgeCapture) {
                    this._fe.GraphScroll();
                    return;
                }
                //              }
            }

            if (this.drawing.IsTargetManualPickMode && this.drawing.IsShiftRightFolding) {
                RefreshUI();
                return;
            }

            if (e.RightButton == MouseButtonState.Pressed) {
                if (this._rightButtonStartClickPoint.X != -1 && this._rightButtonStartClickPoint.Y != -1) {

                    if (this._rightMouseButtonChromatogramRectangleZoom) {
                        this._rightButtonEndClickPoint = Mouse.GetPosition(this);
                        //                if (this.rtDiffViewModel.EditMode == ChromatogramEditMode.Display) {
                        if (this.Type == PeakModType.Aligned || this.Type == PeakModType.Original) {
                            //Debug.WriteLine("Right mac");
                            _fe.RectangleRubberDraw();
                        }
                        else
                            this._fe.ZoomRubberDraw();
                        return;
                        //              }
                    }
                    else if (this._rightMouseButtonChromatogramLeftRightZoom) {
                        float[]? peakInformation = this._fe.getDataPositionOnMousePoint(this._rightButtonStartClickPoint);
                        if (peakInformation == null) return;
                        float mousePointRt = peakInformation[1];

                        float newMinRt;
                        float newMaxRt;
                        if (Mouse.GetPosition(this).X - this._rightButtonEndClickPoint.X > 0) {
                            this._rightButtonEndClickPoint = Mouse.GetPosition(this);

                            newMinRt = mousePointRt - (float)((mousePointRt - (float)this.drawing.MinX) * 0.98);
                            newMaxRt = mousePointRt + (float)(((float)this.drawing.MaxX - mousePointRt) * 0.98);

                            if (newMinRt + 0.0001 < newMaxRt) {
                                this.drawing.MinX = newMinRt;
                                this.drawing.MaxX = newMaxRt;
                            }
                        }
                        else if (Mouse.GetPosition(this).X - this._rightButtonEndClickPoint.X < 0) {

                            this._rightButtonEndClickPoint = Mouse.GetPosition(this);

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
                        this._fe.Draw();
                    }
                    else if (this._rightMouseButtonChromatogramUpDownZoom) {

                        float[]? peakInformation = this._fe.getDataPositionOnMousePoint(this._rightButtonStartClickPoint);
                        if (peakInformation == null) return;

                        // Mouse On Y-Axis                
                        if (Mouse.GetPosition(this).Y - this._rightButtonEndClickPoint.Y < 0) {
                            this._rightButtonEndClickPoint = Mouse.GetPosition(this);

                            if ((float)this.drawing.MaxY < 0.0001)
                                return;

                            float newMaxIntensity = (float)this.drawing.MaxY * (float)0.98;
                            if (newMaxIntensity > 0 && newMaxIntensity > this.drawing.MinY)
                                this.drawing.MaxY = newMaxIntensity;
                        }
                        else if (Mouse.GetPosition(this).Y - this._rightButtonEndClickPoint.Y > 0) {
                            this._rightButtonEndClickPoint = Mouse.GetPosition(this);
                            this.drawing.MaxY *= 1.02F;

                            if (this.drawing.MaxY > this.drawing.SeriesList.MaxY)
                                this.drawing.MaxY = this.drawing.SeriesList.MaxY;
                        }
                        this._fe.Draw();
                    }
                }
                else {
                    this._rightButtonEndClickPoint = new Point(-1, -1);
                }
            }

            if (this.drawing.IsTargetManualPickMode) {
                RefreshUI();
            }
        }

        private void UserControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (this.drawing == null) return;

            if (this.drawing.IsTargetManualPickMode && this.drawing.IsShiftRightFolding) {
                this.drawing.IsShiftRightFolding = false;
                this.drawing.IsShiftRightReleased = true;

                RefreshUI();

                this.drawing.IsShiftRightReleased = false;

                RefreshUI();

                return;
            }

            if (this._rightMouseButtonChromatogramRectangleZoom) {
                this._rightButtonEndClickPoint = Mouse.GetPosition(this);
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
                    UtilityLegacy.ChangeAlignedRtProperty(this.PeakPropertyList, minX, maxX);
                    _fe.Draw();
                    this.VM?.UpdateAlignedChromUC();
                    Mouse.OverrideCursor = null;
                }
                else if (this.Type == PeakModType.Aligned) {
                    Mouse.OverrideCursor = Cursors.Wait;
                    foreach (var sample in PeakPropertyList) {
                        sample.ModifyPeakEdge(minX, maxX);
                    }
                    this.VM?.UpdateAlignedChromUC();
                    this.VM?.UpdatePickingChromUC();
                    Mouse.OverrideCursor = null;
                }
                else {
                    this._rightButtonEndClickPoint = Mouse.GetPosition(this);
                    this._fe.GraphZoom();
                    this._fe.Draw();
                }
            }

            // Reset Mouse Position
            this._rightButtonStartClickPoint = new Point(-1, -1);
            this._rightButtonEndClickPoint = new Point(-1, -1);
            this._rightMouseButtonChromatogramLeftRightZoom = false;
            this._rightMouseButtonChromatogramRectangleZoom = false;
            this._rightMouseButtonChromatogramUpDownZoom = false;

        }

        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (this.drawing == null) return;
            this._fe.Draw();

            // Reset Mouse Position
            this._leftButtonStartClickPoint = new Point(-1, -1);
            this._leftButtonEndClickPoint = new Point(-1, -1);

            // Reset capture
            this.LeftMouseButtonLeftEdgeCapture = false;
            this.LeftMouseButtonRightEdgeCapture = false;


            if (this.drawing.IsTargetManualPickMode) {
                this.drawing.IsMouseLeftUpDone = true;
                RefreshUI();
                this.drawing.IsMouseLeftUpDone = false;
                RefreshUI();
            }

        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e) {
            if (this.drawing == null) return;
            //this.rightButtonEndClickPoint.X = -1;
            this.drawing.IsShiftRightFolding = false;
            this._fe.Draw();
        }

        private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (this.drawing == null) return;

            this._currentMousePoint = Mouse.GetPosition(this);

            var peakInformation = this._fe.getDataPositionOnMousePoint(this._currentMousePoint);
            if (peakInformation == null) return;

            float x = peakInformation[1];
            float y = peakInformation[3];

            float newMinX;
            float newMaxX;
            float newMaxY;
            //float newMinY;
            if (e.Delta > 0) {
                newMinX = (float)x - (float)((x - (float)this.drawing.MinX) * 0.9);
                newMaxX = (float)x + (float)(((float)this.drawing.MaxX - x) * 0.9);
                //newMinY = (float)y - (float)((y - (float)this.drawing.MinY) * 0.9);
                newMaxY = (float)y + (float)(((float)this.drawing.MaxY - y) * 0.9);
            }
            else {
                newMinX = (float)x - (float)((x - (float)this.drawing.MinX) * 1.1);
                newMaxX = (float)x + (float)(((float)this.drawing.MaxX - x) * 1.1);
                //newMinY = (float)y - (float)((y - (float)this.drawing.MinY) * 1.1);
                newMaxY = (float)y + (float)(((float)this.drawing.MaxY - y) * 1.1);
            }


            if (this._currentMousePoint.X > this.drawing.Area.Margin.Left && this._currentMousePoint.Y > this.ActualHeight - this.drawing.Area.Margin.Bottom) {
                if (newMinX < this.drawing.SeriesList.MinX) {
                    this.drawing.MinX = this.drawing.SeriesList.MinX;
                }
                else {
                    if (!this._mouseActionSetting.FixMinX)
                        this.drawing.MinX = newMinX;
                }

                if (newMaxX > this.drawing.SeriesList.MaxX) {
                    this.drawing.MaxX = this.drawing.SeriesList.MaxX;
                }
                else {
                    if (!this._mouseActionSetting.FixMaxX)
                        this.drawing.MaxX = newMaxX;
                }

            }
            else if (this._currentMousePoint.X <= this.drawing.Area.Margin.Left && this._currentMousePoint.Y > this.drawing.Area.Margin.Top && this._currentMousePoint.Y < this.ActualHeight - this.drawing.Area.Margin.Bottom) {
                if (newMaxY > this.drawing.SeriesList.MaxY) {
                    this.drawing.MaxY = this.drawing.SeriesList.MaxY;
                }
                else {
                    if (!this._mouseActionSetting.FixMaxY)
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
            get { return _currentMousePoint; }
            set { _currentMousePoint = value; }
        }

        public Point LeftButtonStartClickPoint {
            get { return _leftButtonStartClickPoint; }
            set { _leftButtonStartClickPoint = value; }
        }

        public Point LeftButtonEndClickPoint {
            get { return _leftButtonEndClickPoint; }
            set { _leftButtonEndClickPoint = value; }
        }

        public Point RightButtonStartClickPoint {
            get { return _rightButtonStartClickPoint; }
            set { _rightButtonStartClickPoint = value; }
        }

        public Point RightButtonEndClickPoint {
            get { return _rightButtonEndClickPoint; }
            set { _rightButtonEndClickPoint = value; }
        }

        public bool LeftMouseButtonLeftEdgeCapture {
            get { return _leftMouseButtonLeftEdgeCapture; }
            set { _leftMouseButtonLeftEdgeCapture = value; }
        }

        public bool LeftMouseButtonRightEdgeCapture {
            get { return _leftMouseButtonRightEdgeCapture; }
            set { _leftMouseButtonRightEdgeCapture = value; }
        }

        public bool RightMouseButtonChromatogramUpDownZoom {
            get { return _rightMouseButtonChromatogramUpDownZoom; }
            set { _rightMouseButtonChromatogramUpDownZoom = value; }
        }

        public bool RightMouseButtonChromatogramLeftRightZoom {
            get { return _rightMouseButtonChromatogramLeftRightZoom; }
            set { _rightMouseButtonChromatogramLeftRightZoom = value; }
        }

        public bool RightMouseButtonChromatogramRectangleZoom {
            get { return _rightMouseButtonChromatogramRectangleZoom; }
            set { _rightMouseButtonChromatogramRectangleZoom = value; }
        }

        public bool KeyDownCheck {
            get { return _keyDownCheck; }
            set { _keyDownCheck = value; }
        }

        public float GraphScrollInitialRtMin {
            get { return _graphScrollInitialRtMin; }
            set { _graphScrollInitialRtMin = value; }
        }

        public float GraphScrollInitialRtMax {
            get { return _graphScrollInitialRtMax; }
            set { _graphScrollInitialRtMax = value; }
        }

        public float GraphScrollInitialIntensityMin {
            get { return _graphScrollInitialIntensityMin; }
            set { _graphScrollInitialIntensityMin = value; }
        }

        public float GraphScrollInitialIntensityMax {
            get { return _graphScrollInitialIntensityMax; }
            set { _graphScrollInitialIntensityMax = value; }
        }
        #endregion

    }
}
