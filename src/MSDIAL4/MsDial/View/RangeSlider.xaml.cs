using System;
using System.Windows;
using System.Windows.Controls;

namespace Rfx.Riken.OsakaUniv
{
	/// <summary>
	/// Interaction logic for RangeSlider.xaml
	/// </summary>
	public partial class RangeSlider : UserControl
    {
     //   private MainWindow mainWindow;
       // private MainWindowDisplayVM mainWindowDisplayVM;

        public RangeSlider()
        {
            InitializeComponent();

            //this.Loaded += RangeSlider_Loaded;
            //this.mainWindow = (MainWindow)Application.Current.MainWindow;

            Minimum = 0;
            Maximum = 100;
            UpperValue = 100;
            LowerValue = 0;
            IntervalValue = 100;
        }

        //public RangeSlider(MainWindow mainWindow, MainWindowDisplayVM mainWindowDisplayVM)
        //{
        //    InitializeComponent();

        //    this.Loaded += RangeSlider_Loaded;
        //    this.mainWindow = mainWindow;
        //    this.mainWindowDisplayVM = mainWindowDisplayVM;

        //    Minimum = 0;
        //    Maximum = 100;
        //    UpperValue = 100;
        //    LowerValue = 0;
        //    IntervalValue = 100;
        //}

        void RangeSlider_Loaded(object sender, RoutedEventArgs e)
        {
            LowerSlider.ValueChanged += LowerSlider_ValueChanged;
            UpperSlider.ValueChanged += UpperSlider_ValueChanged;
        }

        //public void Refresh()
        //{
        //    if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.peakView)
        //    {
        //        if (this.mainWindow.FocusedFileID < 0) return;
        //        if (((PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

        //        ((PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.AmplitudeDisplayLowerFilter = (float)LowerSlider.Value;
        //        ((PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.AmplitudeDisplayUpperFilter = (float)UpperSlider.Value;
        //        ((PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content).RefreshUI();

        //        this.mainWindow.Label_DisplayPeakNum.Content = "Num." + Math.Round(((PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count * (float)((UpperSlider.Value - LowerSlider.Value) * 0.01), 0);
        //    }
        //    else if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.alignmentView)
        //    {
        //        if (this.mainWindow.FocusedAlignmentFileID < 0) return;
        //        if (((PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

        //        ((PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.AmplitudeDisplayLowerFilter = (float)LowerSlider.Value;
        //        ((PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.AmplitudeDisplayUpperFilter = (float)UpperSlider.Value;
        //        ((PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).RefreshUI();
        //    }
        //}

        private void LowerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //this.mainWindowDisplayVM.AmpSliderUpperValue = (float)UpperSlider.Value;
            //this.mainWindowDisplayVM.AmpSliderLowerValue = (float)LowerSlider.Value;

            UpperSlider.Value = Math.Max(UpperSlider.Value, LowerSlider.Value);
            //LowerValue = LowerSlider.Value;
            //IntervalValue = UpperValue - LowerValue;

            //if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.peakView)
            //{
            //    if (this.mainWindow.FocusedFileID < 0) return;
            //    if (((PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

            //    ((PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.AmplitudeDisplayLowerFilter = (float)LowerSlider.Value;
            //    ((PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content).RefreshUI();

            //    this.mainWindow.Label_DisplayPeakNum.Content = "Num." + Math.Round(((PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count * (float)((UpperSlider.Value - LowerSlider.Value) * 0.01), 0);
            //}
            //else if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.alignmentView)
            //{
            //    if (this.mainWindow.FocusedAlignmentFileID < 0) return;
            //    if (((PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

            //    ((PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.AmplitudeDisplayLowerFilter = (float)LowerSlider.Value;
            //    ((PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).RefreshUI();
            //}
        }

        private void UpperSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LowerSlider.Value = Math.Min(UpperSlider.Value, LowerSlider.Value);
            //UpperValue = UpperSlider.Value;
            //IntervalValue = UpperValue - LowerValue;

            //if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.peakView)
            //{
            //    if (this.mainWindow.FocusedFileID < 0) return;
            //    if (((PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

            //    ((PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.AmplitudeDisplayUpperFilter = (float)UpperSlider.Value;
            //    ((PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content).RefreshUI();

            //    this.mainWindow.Label_DisplayPeakNum.Content = "Num." + Math.Round(((PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count * (float)((UpperSlider.Value - LowerSlider.Value) * 0.01), 0);
            //}
            //else if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.alignmentView)
            //{
            //    if (this.mainWindow.FocusedAlignmentFileID < 0) return;
            //    if (((PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

            //    ((PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.AmplitudeDisplayUpperFilter = (float)UpperSlider.Value;
            //    ((PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).RefreshUI();
            //}
        }

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(RangeSlider), new UIPropertyMetadata(0d));

        public double LowerValue
        {
            get { return (double)GetValue(LowerValueProperty); }
            set { SetValue(LowerValueProperty, value); }
        }

        public static readonly DependencyProperty LowerValueProperty =
            DependencyProperty.Register("LowerValue", typeof(double), typeof(RangeSlider), new UIPropertyMetadata(0d));

        public double UpperValue
        {
            get { return (double)GetValue(UpperValueProperty); }
            set { SetValue(UpperValueProperty, value); }
        }

        public static readonly DependencyProperty UpperValueProperty =
            DependencyProperty.Register("UpperValue", typeof(double), typeof(RangeSlider), new UIPropertyMetadata(0d));

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(RangeSlider), new UIPropertyMetadata(1d));

        public double IntervalValue
        {
            get { return (double)(UpperValue - LowerValue); }
            set { SetValue(IntervalValueProperty, value); }
        }

        public static readonly DependencyProperty IntervalValueProperty =
            DependencyProperty.Register("IntervalValue", typeof(double), typeof(RangeSlider), new UIPropertyMetadata(0d));


    }
}
