using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CompMs.Graphics.Core.Base
{
    /// <summary>
    /// ChromatogramTicEicUI.xaml の相互作用ロジック
    /// </summary>
    public partial class DefaultUC : UserControl
    {
        private DefaultFE<DrawVisual> fe;       
        public ActionContainer Action { get; set; } = new ActionContainer();

        public DefaultUC() {
            InitializeComponent();
            //Property settting
            this.fe = new DefaultFE<DrawVisual>(new DrawVisual(), Action);
            this.Content = this.fe;
            this.SizeChanged += new SizeChangedEventHandler(UserControl_SizeChanged);
        }

        public DefaultUC(DrawVisual drawing) {
            InitializeComponent();

            //Property settting
            this.fe = new DefaultFE<DrawVisual>(drawing, Action);
            this.Content = this.fe;

            this.SizeChanged += new SizeChangedEventHandler(UserControl_SizeChanged);
            this.MouseMove += new MouseEventHandler(UserControl_MouseMove);
            this.MouseLeave += new MouseEventHandler(UserControl_MouseLeave);
            this.MouseRightButtonUp += new MouseButtonEventHandler(UserControl_MouseRightButtonUp);
            this.MouseRightButtonDown += new MouseButtonEventHandler(UserControl_MouseRightButtonDown);
            this.MouseLeftButtonUp += new MouseButtonEventHandler(UserControl_MouseLeftButtonUp);
            this.MouseLeftButtonDown += new MouseButtonEventHandler(UserControl_MouseLeftButtonDown);
            this.MouseDoubleClick += new MouseButtonEventHandler(UserControl_MouseLeftDoubleClick);
            this.MouseWheel += new MouseWheelEventHandler(UserControl_MouseWheel);
        }

        public DefaultUC(DrawVisual drawing, MouseActionSetting setting) {
            InitializeComponent();

            //Property settting
            this.fe = new DefaultFE<DrawVisual>(drawing, Action, setting);
            this.Content = this.fe;

            this.SizeChanged += new SizeChangedEventHandler(UserControl_SizeChanged);
            this.MouseMove += new MouseEventHandler(UserControl_MouseMove);
            this.MouseLeave += new MouseEventHandler(UserControl_MouseLeave);
            this.MouseRightButtonUp += new MouseButtonEventHandler(UserControl_MouseRightButtonUp);
            this.MouseRightButtonDown += new MouseButtonEventHandler(UserControl_MouseRightButtonDown);
            this.MouseLeftButtonUp += new MouseButtonEventHandler(UserControl_MouseLeftButtonUp);
            this.MouseLeftButtonDown += new MouseButtonEventHandler(UserControl_MouseLeftButtonDown);
            this.MouseDoubleClick += new MouseButtonEventHandler(UserControl_MouseLeftDoubleClick);
            this.MouseWheel += new MouseWheelEventHandler(UserControl_MouseWheel);


            if (!setting.CanMouseAction) {
                this.MouseMove -= new MouseEventHandler(UserControl_MouseMove);
                this.MouseLeave -= new MouseEventHandler(UserControl_MouseLeave);
                this.MouseRightButtonUp -= new MouseButtonEventHandler(UserControl_MouseRightButtonUp);
                this.MouseRightButtonDown -= new MouseButtonEventHandler(UserControl_MouseRightButtonDown);
                this.MouseLeftButtonUp -= new MouseButtonEventHandler(UserControl_MouseLeftButtonUp);
                this.MouseLeftButtonDown -= new MouseButtonEventHandler(UserControl_MouseLeftButtonDown);
                this.MouseDoubleClick -= new MouseButtonEventHandler(UserControl_MouseLeftDoubleClick);
                this.MouseWheel -= new MouseWheelEventHandler(UserControl_MouseWheel);
            }else if (!setting.CanZoomRubber) {
                this.MouseRightButtonUp -= new MouseButtonEventHandler(UserControl_MouseRightButtonUp);
                this.MouseRightButtonDown -= new MouseButtonEventHandler(UserControl_MouseRightButtonDown);
                this.MouseMove -= new MouseEventHandler(UserControl_MouseMove);
                this.MouseLeave -= new MouseEventHandler(UserControl_MouseLeave);
            }
            if(setting.FixMaxX && setting.FixMinX && setting.FixMaxY && setting.FixMinY) {
                this.MouseWheel -= new MouseWheelEventHandler(UserControl_MouseWheel);
            }
        }
        public void RefreshUI() {
            fe.Draw();
        }


        public void UserControl_SizeChanged(object sender, SizeChangedEventArgs e) {
            RefreshUI();
        }


        public virtual void UserControl_MouseLeftDoubleClick(object sender, MouseButtonEventArgs e) {
            if (fe.drawing == null) return;
            if (e.StylusDevice != null) return;// Avoid Touch Event

            // Set Mouse Position
            if (e.LeftButton == MouseButtonState.Pressed) {
                fe.ac.CurrentMousePoint = Mouse.GetPosition(this);

                if (fe.ac.CurrentMousePoint.X > fe.drawing.Area.Margin.Left && fe.ac.CurrentMousePoint.Y > fe.ActualHeight - fe.drawing.Area.Margin.Bottom) {
                    fe.drawing.MaxX = fe.drawing.SeriesList.MaxX;
                    fe.drawing.MinX = fe.drawing.SeriesList.MinX;
                    RefreshUI();
                }
                else if (fe.ac.CurrentMousePoint.X < fe.drawing.Area.Margin.Left && fe.ac.CurrentMousePoint.Y < fe.ActualHeight - fe.drawing.Area.Margin.Bottom) {
                    fe.drawing.MaxY = fe.drawing.SeriesList.MaxY;
                    fe.drawing.MinY = fe.drawing.SeriesList.MinY;
                    RefreshUI();
                }
                else if (fe.ac.CurrentMousePoint.X > fe.drawing.Area.Margin.Left && fe.ac.CurrentMousePoint.Y > fe.drawing.Area.Margin.Top) {
                    fe.ResetGraphDisplayRange();
                }
            }
        }

        public virtual void UserControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            if (fe.drawing == null) return;

            // Set Mouse Position
            fe.ac.RightButtonStartClickPoint = Mouse.GetPosition(this);

            if (fe.ac.RightButtonStartClickPoint.X <= fe.drawing.Area.Margin.Left && fe.ac.RightButtonStartClickPoint.Y > fe.drawing.Area.Margin.Top && fe.ac.RightButtonStartClickPoint.Y < fe.ActualHeight - fe.drawing.Area.Margin.Bottom) fe.ac.RightMouseButtonChromatogramUpDownZoom = true;
            else if (fe.ac.RightButtonStartClickPoint.X > fe.drawing.Area.Margin.Left && fe.ac.RightButtonStartClickPoint.Y >= fe.ActualHeight - fe.drawing.Area.Margin.Bottom) fe.ac.RightMouseButtonChromatogramLeftRightZoom = true;
            else fe.ac.RightMouseButtonChromatogramRectangleZoom = true;
        }

        public virtual void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (fe.drawing == null) return;
            if (e.StylusDevice != null) return; // Avoid Touch Event

            // Set Mouse Position
            fe.ac.LeftButtonStartClickPoint = Mouse.GetPosition(this);
            fe.ac.GraphScrollInitialRtMin = (float)fe.drawing.MinX;
            fe.ac.GraphScrollInitialRtMax = fe.drawing.MaxX;
            fe.ac.GraphScrollInitialIntensityMin = fe.drawing.MinY;
            fe.ac.GraphScrollInitialIntensityMax = fe.drawing.MaxY;

        }

        public virtual void UserControl_MouseMove(object sender, MouseEventArgs e) {
            if (fe.drawing == null) return;

            // Store Current Mouse Point
            fe.ac.CurrentMousePoint = Mouse.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed) {
                fe.ac.LeftButtonEndClickPoint = Mouse.GetPosition(this);

                //                if (fe.rtDiffViewModel.EditMode == ChromatogramEditMode.Display) {
                fe.GraphScroll();
                //              }
            }

            if (e.RightButton == MouseButtonState.Pressed) {
                if (fe.ac.RightButtonStartClickPoint.X != -1 && fe.ac.RightButtonStartClickPoint.Y != -1) {

                    if (fe.ac.RightMouseButtonChromatogramRectangleZoom) {
                        fe.ac.RightButtonEndClickPoint = Mouse.GetPosition(this);
                        //                if (fe.rtDiffViewModel.EditMode == ChromatogramEditMode.Display) {
                        fe.ZoomRubberDraw();
                        //              }
                    }
                    else if (fe.ac.RightMouseButtonChromatogramLeftRightZoom) {
                        float[] peakInformation = fe.GetDataPositionOnMousePoint(fe.ac.RightButtonStartClickPoint);
                        if (peakInformation == null) return;

                        float mousePointRt = 0;
                        float newMinRt = float.MaxValue;
                        float newMaxRt = float.MinValue;

                        mousePointRt = peakInformation[1];

                        if (Mouse.GetPosition(this).X - fe.ac.RightButtonEndClickPoint.X > 0) {
                            fe.ac.RightButtonEndClickPoint = Mouse.GetPosition(this);

                            newMinRt = mousePointRt - (float)((mousePointRt - (float)fe.drawing.MinX) * 0.98);
                            newMaxRt = mousePointRt + (float)(((float)fe.drawing.MaxX - mousePointRt) * 0.98);

                            if (newMinRt + 0.0001 < newMaxRt) {
                                fe.drawing.MinX = newMinRt;
                                fe.drawing.MaxX = newMaxRt;
                            }
                        }
                        else if (Mouse.GetPosition(this).X - fe.ac.RightButtonEndClickPoint.X < 0) {

                            fe.ac.RightButtonEndClickPoint = Mouse.GetPosition(this);

                            newMinRt = mousePointRt - (float)((mousePointRt - (float)fe.drawing.MinX) * 1.02);
                            newMaxRt = mousePointRt + (float)(((float)fe.drawing.MaxX - mousePointRt) * 1.02);

                            if (newMinRt < fe.drawing.SeriesList.MinX)
                                fe.drawing.MinX = fe.drawing.SeriesList.MinX;
                            else
                                fe.drawing.MinX = newMinRt;

                            if (newMaxRt > fe.drawing.SeriesList.MaxX)
                                fe.drawing.MaxX = fe.drawing.SeriesList.MaxX;
                            else
                                fe.drawing.MaxX = newMaxRt;
                        }
                        RefreshUI();
                    }
                    else if (fe.ac.RightMouseButtonChromatogramUpDownZoom) {

                        float[] peakInformation = fe.GetDataPositionOnMousePoint(fe.ac.RightButtonStartClickPoint);
                        if (peakInformation == null) return;

                        float mousePointIntensity = 0;
                        float newMinIntensity = float.MaxValue;
                        float newMaxIntensity = float.MinValue;

                        mousePointIntensity = peakInformation[3];

                        // Mouse On Y-Axis                
                        if (Mouse.GetPosition(this).Y - fe.ac.RightButtonEndClickPoint.Y < 0) {
                            fe.ac.RightButtonEndClickPoint = Mouse.GetPosition(this);

                            if ((float)fe.drawing.MaxY < 0.0001)
                                return;

                            newMaxIntensity = (float)fe.drawing.MaxY * (float)0.98;
                            if (newMaxIntensity > 0 && newMaxIntensity > fe.drawing.MinY)
                                fe.drawing.MaxY = newMaxIntensity;
                        }
                        else if (Mouse.GetPosition(this).Y - fe.ac.RightButtonEndClickPoint.Y > 0) {
                            fe.ac.RightButtonEndClickPoint = Mouse.GetPosition(this);
                            fe.drawing.MaxY = fe.drawing.MaxY * 1.02F;

                            if (fe.drawing.MaxY > fe.drawing.SeriesList.MaxY)
                                fe.drawing.MaxY = fe.drawing.SeriesList.MaxY;
                        }
                        RefreshUI();
                    }
                }
                else {
                    fe.ac.RightButtonEndClickPoint = new Point(-1, -1);
                }
            }
        }

        public virtual void UserControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (fe.drawing == null) return;

            if (fe.ac.RightMouseButtonChromatogramRectangleZoom) {
                fe.ac.RightButtonEndClickPoint = Mouse.GetPosition(this);

                //        if (fe.rtDiffViewModel.EditMode == ChromatogramEditMode.Display) {
                fe.GraphZoom();
                //        }
                RefreshUI();
            }

            // Reset Mouse Position
            fe.ac.RightButtonStartClickPoint = new Point(-1, -1);
            fe.ac.RightButtonEndClickPoint = new Point(-1, -1);
            fe.ac.RightMouseButtonChromatogramLeftRightZoom = false;
            fe.ac.RightMouseButtonChromatogramRectangleZoom = false;
            fe.ac.RightMouseButtonChromatogramUpDownZoom = false;

        }

        public virtual void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (fe.drawing == null) return;
            RefreshUI();

            // Reset Mouse Position
            fe.ac.LeftButtonStartClickPoint = new Point(-1, -1);
            fe.ac.LeftButtonEndClickPoint = new Point(-1, -1);
        }

        public virtual void UserControl_MouseLeave(object sender, MouseEventArgs e) {
            if (fe.drawing == null) return;
            //ac.RightButtonEndClickPoint.X = -1;
            fe.ac.RightButtonEndClickPoint = new Point(-1, -1);
            RefreshUI();
        }

        public virtual void UserControl_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (fe.drawing == null) return;

            fe.ac.CurrentMousePoint = Mouse.GetPosition(this);

            var peakInformation = fe.GetDataPositionOnMousePoint(fe.ac.CurrentMousePoint);
            if (peakInformation == null) return;

            float newMinX = float.MaxValue;
            float newMaxX = float.MinValue;
            float newMinY = float.MaxValue;
            float newMaxY = float.MinValue;

            float x = peakInformation[1];
            float y = peakInformation[3];

            if (e.Delta > 0) {
                newMinX = (float)x - (float)((x - (float)fe.drawing.MinX) * 0.9);
                newMaxX = (float)x + (float)(((float)fe.drawing.MaxX - x) * 0.9);
                newMinY = (float)y - (float)((y - (float)fe.drawing.MinY) * 0.9);
                newMaxY = (float)y + (float)(((float)fe.drawing.MaxY - y) * 0.9);
            }
            else {
                newMinX = (float)x - (float)((x - (float)fe.drawing.MinX) * 1.1);
                newMaxX = (float)x + (float)(((float)fe.drawing.MaxX - x) * 1.1);
                newMinY = (float)y - (float)((y - (float)fe.drawing.MinY) * 1.1);
                newMaxY = (float)y + (float)(((float)fe.drawing.MaxY - y) * 1.1);
            }


            if (fe.ac.CurrentMousePoint.X > fe.drawing.Area.Margin.Left && fe.ac.CurrentMousePoint.Y > fe.ActualHeight - fe.drawing.Area.Margin.Bottom) {
                if (newMinX < fe.drawing.SeriesList.MinX) {
                    fe.drawing.MinX = fe.drawing.SeriesList.MinX;
                }
                else {
                    if (!fe.mouseActionSetting.FixMinX)
                        fe.drawing.MinX = newMinX;
                }

                if (newMaxX > fe.drawing.SeriesList.MaxX) {
                    fe.drawing.MaxX = fe.drawing.SeriesList.MaxX;
                }
                else {
                    if (!fe.mouseActionSetting.FixMaxX)
                        fe.drawing.MaxX = newMaxX;
                }

            }
            else if (fe.ac.CurrentMousePoint.X <= fe.drawing.Area.Margin.Left && fe.ac.CurrentMousePoint.Y > fe.drawing.Area.Margin.Top && fe.ac.CurrentMousePoint.Y < fe.ActualHeight - fe.drawing.Area.Margin.Bottom) {
                if (newMaxY > fe.drawing.SeriesList.MaxY) {
                    fe.drawing.MaxY = fe.drawing.SeriesList.MaxY;
                }
                else {
                    if (!fe.mouseActionSetting.FixMaxY)
                        fe.drawing.MaxY = newMaxY;
                }

                if (newMinY < fe.drawing.SeriesList.MinY) {
                    fe.drawing.MinY = fe.drawing.SeriesList.MinY;
                }
                else {
                    if (!fe.mouseActionSetting.FixMinY)
                        fe.drawing.MinY = newMinY;
                }
            }
            RefreshUI();
        }
    }
}

