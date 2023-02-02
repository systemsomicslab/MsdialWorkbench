using Msdial.Gcms.Dataprocess.Algorithm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public enum PairwisePlotDisplayLabel
    {
        None,
        X,
        Y,
        Metabolite,
        Isotope,
        Adduct,
        Label
    }

    public enum RetentionUnit
    {
        RT, RI
    }

    public class PairwisePlotBean : INotifyPropertyChanged
    {
        private ObservableCollection<int> indexes;
        private ObservableCollection<double> xAxisDatapointCollection;
        private ObservableCollection<double> yAxisDatapointCollection;
        private ObservableCollection<SolidColorBrush> plotBrushCollection;
        private ObservableCollection<string> labelCollection;
        private ObservableCollection<PeakAreaBean> peakAreaBeanCollection;
        private ObservableCollection<AlignmentPropertyBean> alignmentPropertyBeanCollection;
        private List<MS1DecResult> ms1DecResults;
        private ObservableCollection<DriftSpotBean> driftSpots;
        private ObservableCollection<AlignedDriftSpotPropertyBean> alignmentDriftSpotBean;

        private PairwisePlotDisplayLabel displayLabel;
        private RetentionUnit retentionUnit;

        private float minX;
        private float maxX;
        private float minY;
        private float maxY;

        private float? displayRangeMinX;
        private float? displayRangeMaxX;
        private float? displayRangeMinY;
        private float? displayRangeMaxY;

        private string graphTitle;
        private string xAxisTitle;
        private string yAxisTitle;

        private int selectedPlotId;
        private int selectedMs1DecID;

        private float rectangleRangeYmax;
        private float rectangleRangeYmin;
        private float rectangleRangeXmax;
        private float rectangleRangeXmin;

        private float amplitudeDisplayLowerFilter = 0;
        private float amplitudeDisplayUpperFilter = 100;
        private bool identifiedOnlyDisplayFilter = false;
        private bool annotatedOnlyDisplayFilter = false;
        private bool msmsOnlyDisplayFilter = false;
        private bool molcularIonFilter = false;
        private bool unknownFilter = false;
        private bool uniqueionFilter = false;
        private bool blankFilter = false;
        private bool ccsFilter = false;

		private int scanNumber = -1;

        //displayed points
        private int displayedSpotCount;


        public PairwisePlotBean(string graphTitle, string xAxisTitle, string yAxisTitle, ObservableCollection<double> xAxisDatapointCollection, 
            ObservableCollection<double> yAxisDatapointCollection, ObservableCollection<string> labelCollection, 
            ObservableCollection<SolidColorBrush> plotBrushCollection, PairwisePlotDisplayLabel displayLabel)
        {
			Debug.WriteLine("New PwPB 1");
            this.xAxisDatapointCollection = new ObservableCollection<double>();
            this.xAxisDatapointCollection = xAxisDatapointCollection;
            this.yAxisDatapointCollection = new ObservableCollection<double>();
            this.yAxisDatapointCollection = yAxisDatapointCollection;
            this.labelCollection = new ObservableCollection<string>();
            this.labelCollection = labelCollection;
            this.plotBrushCollection = new ObservableCollection<SolidColorBrush>();
            this.plotBrushCollection = plotBrushCollection;
            this.displayLabel = displayLabel;

            this.graphTitle = graphTitle;
            this.xAxisTitle = xAxisTitle;
            this.yAxisTitle = yAxisTitle;

            this.selectedPlotId = -1;
            this.selectedMs1DecID = -1;

            SetInitializeValue();
        }

        public PairwisePlotBean(string graphTitle, string xAxisTitle, string yAxisTitle, ObservableCollection<double> xAxisDatapointCollection,
            ObservableCollection<double> yAxisDatapointCollection, ObservableCollection<string> labelCollection,
            ObservableCollection<SolidColorBrush> plotBrushCollection, ObservableCollection<int> indexes, PairwisePlotDisplayLabel displayLabel) {
            Debug.WriteLine("New PwPB 3");

            this.indexes = indexes;
            this.xAxisDatapointCollection = xAxisDatapointCollection;
            this.yAxisDatapointCollection = yAxisDatapointCollection;
            this.labelCollection = labelCollection;
            this.plotBrushCollection = plotBrushCollection;
            this.displayLabel = displayLabel;

            this.graphTitle = graphTitle;
            this.xAxisTitle = xAxisTitle;
            this.yAxisTitle = yAxisTitle;

            this.selectedPlotId = -1;
            this.selectedMs1DecID = -1;

            SetInitializeValue();
        }

        public PairwisePlotBean(string graphTitle, string xAxisTitle, string yAxisTitle, ObservableCollection<double> xAxisDatapointCollection, 
            ObservableCollection<double> yAxisDatapointCollection, ObservableCollection<PeakAreaBean> peakAreaBeanCollection,
            ObservableCollection<SolidColorBrush> plotBrushCollection, PairwisePlotDisplayLabel displayLabel)
        {
			Debug.WriteLine("New PwPB 2");
			this.xAxisDatapointCollection = new ObservableCollection<double>();
            this.xAxisDatapointCollection = xAxisDatapointCollection;
            this.yAxisDatapointCollection = new ObservableCollection<double>();
            this.yAxisDatapointCollection = yAxisDatapointCollection;
            this.peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();
            this.peakAreaBeanCollection = peakAreaBeanCollection;
            this.plotBrushCollection = new ObservableCollection<SolidColorBrush>();
            this.plotBrushCollection = plotBrushCollection;

            this.graphTitle = graphTitle;
            this.xAxisTitle = xAxisTitle;
            this.yAxisTitle = yAxisTitle;

            this.displayLabel = displayLabel;

            this.selectedPlotId = -1;
            this.selectedMs1DecID = -1;
            this.rectangleRangeXmax = -1;
            this.rectangleRangeXmin = -1;
            this.rectangleRangeYmax = -1;
            this.rectangleRangeYmin = -1;

            SetInitializeValue(peakAreaBeanCollection);
        }

        public PairwisePlotBean(string graphTitle, string xAxisTitle, string yAxisTitle, ObservableCollection<double> xAxisDatapointCollection,
            ObservableCollection<double> yAxisDatapointCollection, ObservableCollection<DriftSpotBean> driftSpots,
            ObservableCollection<SolidColorBrush> plotBrushCollection, PairwisePlotDisplayLabel displayLabel, 
            float minDriftTime, float maxDriftTime, float minMz, float maxMz, float targetMz) {
            Debug.WriteLine("New PwPB drift");
            this.xAxisDatapointCollection = new ObservableCollection<double>();
            this.xAxisDatapointCollection = xAxisDatapointCollection;
            this.yAxisDatapointCollection = new ObservableCollection<double>();
            this.yAxisDatapointCollection = yAxisDatapointCollection;
            this.peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();
            this.driftSpots = driftSpots;
            this.plotBrushCollection = new ObservableCollection<SolidColorBrush>();
            this.plotBrushCollection = plotBrushCollection;

            this.graphTitle = graphTitle;
            this.xAxisTitle = xAxisTitle;
            this.yAxisTitle = yAxisTitle;

            this.displayLabel = displayLabel;

            this.selectedPlotId = -1;
            this.selectedMs1DecID = -1;
            this.rectangleRangeXmax = -1;
            this.rectangleRangeXmin = -1;
            this.rectangleRangeYmax = -1;
            this.rectangleRangeYmin = -1;

            //SetInitializeValue(minDriftTime, maxDriftTime, minMz, maxMz, targetMz);
            SetInitializeValue(targetMz, driftSpots);
        }

       

        public PairwisePlotBean(string graphTitle, string xAxisTitle, string yAxisTitle, ObservableCollection<double> xAxisDatapointCollection,
            ObservableCollection<double> yAxisDatapointCollection, ObservableCollection<AlignedDriftSpotPropertyBean> driftSpots,
            ObservableCollection<SolidColorBrush> plotBrushCollection, PairwisePlotDisplayLabel displayLabel, float targetMz) {
            Debug.WriteLine("New PwPB aligned drift");
            this.xAxisDatapointCollection = new ObservableCollection<double>();
            this.xAxisDatapointCollection = xAxisDatapointCollection;
            this.yAxisDatapointCollection = new ObservableCollection<double>();
            this.yAxisDatapointCollection = yAxisDatapointCollection;
            this.peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();
            this.AlignmentDriftSpotBean = driftSpots;
            this.plotBrushCollection = new ObservableCollection<SolidColorBrush>();
            this.plotBrushCollection = plotBrushCollection;

            this.graphTitle = graphTitle;
            this.xAxisTitle = xAxisTitle;
            this.yAxisTitle = yAxisTitle;

            this.displayLabel = displayLabel;

            this.selectedPlotId = -1;
            this.selectedMs1DecID = -1;
            this.rectangleRangeXmax = -1;
            this.rectangleRangeXmin = -1;
            this.rectangleRangeYmax = -1;
            this.rectangleRangeYmin = -1;

            //SetInitializeValue(targetMz);
            SetInitializeValue(targetMz, driftSpots);
        }

       
        public PairwisePlotBean(string graphTitle, string xAxisTitle, string yAxisTitle, ObservableCollection<double> xAxisDatapointCollection, 
            ObservableCollection<double> yAxisDatapointCollection, ObservableCollection<AlignmentPropertyBean> alignmentPropertyBeanCollection, 
            ObservableCollection<SolidColorBrush> plotBrushCollection, PairwisePlotDisplayLabel displayLabel)
        {
			Debug.WriteLine("New PwPB 3");
			this.xAxisDatapointCollection = new ObservableCollection<double>();
            this.xAxisDatapointCollection = xAxisDatapointCollection;
            this.yAxisDatapointCollection = new ObservableCollection<double>();
            this.yAxisDatapointCollection = yAxisDatapointCollection;
            this.alignmentPropertyBeanCollection = new ObservableCollection<AlignmentPropertyBean>();
            this.alignmentPropertyBeanCollection = alignmentPropertyBeanCollection;
            this.plotBrushCollection = new ObservableCollection<SolidColorBrush>();
            this.plotBrushCollection = plotBrushCollection;

            this.graphTitle = graphTitle;
            this.xAxisTitle = xAxisTitle;
            this.yAxisTitle = yAxisTitle;

            this.displayLabel = displayLabel;

            this.selectedPlotId = -1;
            this.selectedMs1DecID = -1;
            this.rectangleRangeXmax = -1;
            this.rectangleRangeXmin = -1;
            this.rectangleRangeYmax = -1;
            this.rectangleRangeYmin = -1;

            SetInitializeValue(alignmentPropertyBeanCollection);
        }

        public PairwisePlotBean(string graphTitle, string xAxisTitle, string yAxisTitle, ObservableCollection<double> xAxisDatapointCollection, 
            ObservableCollection<double> yAxisDatapointCollection, ObservableCollection<PeakAreaBean> peakAreaBeanCollection, List<MS1DecResult> ms1DecResults, 
            ObservableCollection<SolidColorBrush> plotBrushCollection, PairwisePlotDisplayLabel displayLabel)
        {
			Debug.WriteLine("New PwPB 4");
			this.xAxisDatapointCollection = new ObservableCollection<double>();
            this.yAxisDatapointCollection = new ObservableCollection<double>();
            this.peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();
            this.plotBrushCollection = new ObservableCollection<SolidColorBrush>();
            this.ms1DecResults = new List<MS1DecResult>();
            
            this.xAxisDatapointCollection = xAxisDatapointCollection;
            this.yAxisDatapointCollection = yAxisDatapointCollection;
            this.peakAreaBeanCollection = peakAreaBeanCollection;
            this.plotBrushCollection = plotBrushCollection;
            this.ms1DecResults = ms1DecResults;

            this.graphTitle = graphTitle;
            this.xAxisTitle = xAxisTitle;
            this.yAxisTitle = yAxisTitle;

            this.displayLabel = displayLabel;

            this.selectedPlotId = -1;
            this.selectedMs1DecID = -1;
            this.rectangleRangeXmax = -1;
            this.rectangleRangeXmin = -1;
            this.rectangleRangeYmax = -1;
            this.rectangleRangeYmin = -1;

            SetInitializeValue(peakAreaBeanCollection);
        }

       
        public PairwisePlotBean(string graphTitle, string xAxisTitle, string yAxisTitle, ObservableCollection<double> xAxisDatapointCollection,
            ObservableCollection<double> yAxisDatapointCollection, ObservableCollection<AlignmentPropertyBean> alignmentPropertyBeanCollection, List<MS1DecResult> ms1DecResults,
            ObservableCollection<SolidColorBrush> plotBrushCollection, PairwisePlotDisplayLabel displayLabel)
        {
			Debug.WriteLine("New PwPB 5");
			this.xAxisDatapointCollection = new ObservableCollection<double>();
            this.yAxisDatapointCollection = new ObservableCollection<double>();
            this.alignmentPropertyBeanCollection = new ObservableCollection<AlignmentPropertyBean>();
            this.plotBrushCollection = new ObservableCollection<SolidColorBrush>();
            this.ms1DecResults = new List<MS1DecResult>();

            this.xAxisDatapointCollection = xAxisDatapointCollection;
            this.yAxisDatapointCollection = yAxisDatapointCollection;
            this.alignmentPropertyBeanCollection = alignmentPropertyBeanCollection;
            this.plotBrushCollection = plotBrushCollection;
            this.ms1DecResults = ms1DecResults;

            this.graphTitle = graphTitle;
            this.xAxisTitle = xAxisTitle;
            this.yAxisTitle = yAxisTitle;

            this.displayLabel = displayLabel;

            this.selectedPlotId = -1;
            this.selectedMs1DecID = -1;
            this.rectangleRangeXmax = -1;
            this.rectangleRangeXmin = -1;
            this.rectangleRangeYmax = -1;
            this.rectangleRangeYmin = -1;

            SetInitializeValue(alignmentPropertyBeanCollection);
        }

       

        public PairwisePlotBean()
        {
			Debug.WriteLine("New PwPB 6");
			this.xAxisDatapointCollection = null;
            this.yAxisDatapointCollection = null;
            this.labelCollection = null;
            this.plotBrushCollection = null;

            this.graphTitle = "File name";
            this.xAxisTitle = "Retention time [min]";
            this.yAxisTitle = "m/z";

            this.minX = 0;
            this.maxX = 10;
            this.minY = 0;
            this.maxY = 10;
            this.selectedPlotId = -1;
            this.selectedMs1DecID = -1;

            this.displayRangeMaxX = this.maxX;
            this.displayRangeMaxY = this.maxY;
            this.displayRangeMinX = this.minX;
            this.displayRangeMinY = this.minY;
        }

        private void SetInitializeValue()
        {
            this.minX = float.MaxValue;
            this.minY = float.MaxValue;
            this.maxX = float.MinValue;
            this.maxY = float.MinValue;

            for (int i = 0; i < xAxisDatapointCollection.Count; i++)
            {
                if (this.minX > (float)xAxisDatapointCollection[i]) this.minX = (float)xAxisDatapointCollection[i];
                if (this.minY > (float)yAxisDatapointCollection[i]) this.minY = (float)yAxisDatapointCollection[i];
                if (this.maxX < (float)xAxisDatapointCollection[i]) this.maxX = (float)xAxisDatapointCollection[i];
                if (this.maxY < (float)yAxisDatapointCollection[i]) this.maxY = (float)yAxisDatapointCollection[i];
            }

            //if (Math.Abs(this.maxX - this.minX) < 2) {
            //    this.maxX += 5;
            //    this.minX -= 5;
            //}

            //if (Math.Abs(this.maxY - this.minY) < 2) {
            //    this.maxY += 5;
            //    this.minY -= 5;
            //}

            float xMargin, yMargin;
            xMargin = (this.maxX - this.minX) / 10;
            yMargin = (this.maxY - this.minY) / 10;

            this.minX -= xMargin;
            this.minY -= yMargin;
            this.maxX += xMargin;
            this.maxY += yMargin;

            this.displayRangeMinX = this.minX;
            this.displayRangeMinY = this.minY;
            this.displayRangeMaxX = this.maxX;
            this.displayRangeMaxY = this.maxY;
        }

        private void SetInitializeValue(ObservableCollection<AlignmentPropertyBean> spots) {
            this.minX = float.MaxValue;
            this.minY = float.MaxValue;
            this.maxX = float.MinValue;
            this.maxY = float.MinValue;

            for (int i = 0; i < spots.Count; i++) {
                var spot = spots[i];
                var width = spot.AveragePeakWidth * 2;
                if (width > 1) width = 1;

                if (this.minX > (float)xAxisDatapointCollection[i] - width) this.minX = (float)xAxisDatapointCollection[i] - width;
                if (this.minY > (float)yAxisDatapointCollection[i]) this.minY = (float)yAxisDatapointCollection[i];
                if (this.maxX < (float)xAxisDatapointCollection[i] + width) this.maxX = (float)xAxisDatapointCollection[i] + width;
                if (this.maxY < (float)yAxisDatapointCollection[i]) this.maxY = (float)yAxisDatapointCollection[i];
            }

            if (this.minX < 0) this.minX = 0;

            if (Math.Abs(this.maxY - this.minY) < 2) {
                this.maxY += 5;
                this.minY -= 5;
            }

            float xMargin, yMargin;
            xMargin = (this.maxX - this.minX) / 10;
            yMargin = (this.maxY - this.minY) / 10;

            this.minX -= xMargin;
            this.minY -= yMargin;
            this.maxX += xMargin;
            this.maxY += yMargin;

            this.displayRangeMinX = this.minX;
            this.displayRangeMinY = this.minY;
            this.displayRangeMaxX = this.maxX;
            this.displayRangeMaxY = this.maxY;
        }

        private void SetInitializeValue(ObservableCollection<PeakAreaBean> spots) {
            this.minX = float.MaxValue;
            this.minY = float.MaxValue;
            this.maxX = float.MinValue;
            this.maxY = float.MinValue;

            for (int i = 0; i < spots.Count; i++) {
                var spot = spots[i];
                var width = spot.RtAtRightPeakEdge - spot.RtAtLeftPeakEdge;
                if (width > 1) width = 1;
                if (this.minX > (float)xAxisDatapointCollection[i] - width) this.minX = (float)xAxisDatapointCollection[i] - width;
                if (this.minY > (float)yAxisDatapointCollection[i]) this.minY = (float)yAxisDatapointCollection[i];
                if (this.maxX < (float)xAxisDatapointCollection[i] + width) this.maxX = (float)xAxisDatapointCollection[i] + width;
                if (this.maxY < (float)yAxisDatapointCollection[i]) this.maxY = (float)yAxisDatapointCollection[i];
            }

            if (this.minX < 0) this.minX = 0;

            if (Math.Abs(this.maxY - this.minY) < 2) {
                this.maxY += 5;
                this.minY -= 5;
            }

            float xMargin, yMargin;
            xMargin = (this.maxX - this.minX) / 10;
            yMargin = (this.maxY - this.minY) / 10;

            this.minX -= xMargin;
            this.minY -= yMargin;
            this.maxX += xMargin;
            this.maxY += yMargin;

            this.displayRangeMinX = this.minX;
            this.displayRangeMinY = this.minY;
            this.displayRangeMaxX = this.maxX;
            this.displayRangeMaxY = this.maxY;
        }

        private void SetInitializeValue(float targetMz, ObservableCollection<AlignedDriftSpotPropertyBean> spots) {
            this.minX = float.MaxValue;
            this.minY = float.MaxValue;
            this.maxX = float.MinValue;
            this.maxY = float.MinValue;

            for (int i = 0; i < spots.Count; i++) {
                var spot = spots[i];
                var width = spot.AveragePeakWidth * 2;
                if (width > 3) width = 3;
                if (this.minX > (float)xAxisDatapointCollection[i] - width) this.minX = (float)xAxisDatapointCollection[i] - width;
                if (this.minY > (float)yAxisDatapointCollection[i]) this.minY = (float)yAxisDatapointCollection[i];
                if (this.maxX < (float)xAxisDatapointCollection[i] + width) this.maxX = (float)xAxisDatapointCollection[i] + width;
                if (this.maxY < (float)yAxisDatapointCollection[i]) this.maxY = (float)yAxisDatapointCollection[i];
            }

            if (Math.Abs(this.maxY - this.minY) < 2) {
                this.maxY += 5;
                this.minY -= 5;
            }
            if (this.minX < 0) this.minX = 0;

            float xMargin, yMargin;
            xMargin = (this.maxX - this.minX) / 10;
            yMargin = (this.maxY - this.minY) / 10;

            this.minX -= xMargin;
            this.minY -= yMargin;
            this.maxX += xMargin;
            this.maxY += yMargin;

            this.displayRangeMinX = this.minX;
            this.displayRangeMinY = this.minY;
            this.displayRangeMaxX = this.maxX;
            this.displayRangeMaxY = this.maxY;
        }
        private void SetInitializeValue(float targetMz, ObservableCollection<DriftSpotBean> spots) {
            this.minX = float.MaxValue;
            this.minY = float.MaxValue;
            this.maxX = float.MinValue;
            this.maxY = float.MinValue;

            for (int i = 0; i < spots.Count; i++) {
                var spot = spots[i];
                var width = spot.DriftTimeAtRightPeakEdge - spot.DriftTimeAtLeftPeakEdge;
                if (width > 3) width = 3;
                if (this.minX > (float)xAxisDatapointCollection[i] - width) this.minX = (float)xAxisDatapointCollection[i] - width;
                if (this.minY > (float)yAxisDatapointCollection[i]) this.minY = (float)yAxisDatapointCollection[i];
                if (this.maxX < (float)xAxisDatapointCollection[i] + width) this.maxX = (float)xAxisDatapointCollection[i] + width;
                if (this.maxY < (float)yAxisDatapointCollection[i]) this.maxY = (float)yAxisDatapointCollection[i];
            }

            if (Math.Abs(this.maxY - this.minY) < 2) {
                this.maxY += 5;
                this.minY -= 5;
            }
            if (this.minX < 0) this.minX = 0;

            float xMargin, yMargin;
            xMargin = (this.maxX - this.minX) / 10;
            yMargin = (this.maxY - this.minY) / 10;

            this.minX -= xMargin;
            this.minY -= yMargin;
            this.maxX += xMargin;
            this.maxY += yMargin;

            this.displayRangeMinX = this.minX;
            this.displayRangeMinY = this.minY;
            this.displayRangeMaxX = this.maxX;
            this.displayRangeMaxY = this.maxY;
        }

        private void SetInitializeValue(float minX, float maxX, float minY, float maxY, float targetMz) {

            SetInitializeValue();

            //this.minX = minX;
            //this.minY = minY;
            //this.maxX = maxX;
            //this.maxY = maxY;

            //for (int i = 0; i < xAxisDatapointCollection.Count; i++) {
            //    if (this.minX > (float)xAxisDatapointCollection[i]) this.minX = (float)xAxisDatapointCollection[i];
            //    if (this.minY > (float)yAxisDatapointCollection[i]) this.minY = (float)yAxisDatapointCollection[i];
            //    if (this.maxX < (float)xAxisDatapointCollection[i]) this.maxX = (float)xAxisDatapointCollection[i];
            //    if (this.maxY < (float)yAxisDatapointCollection[i]) this.maxY = (float)yAxisDatapointCollection[i];
            //}

            //float xMargin, yMargin;
            //xMargin = (this.maxX - this.minX) / 10;
            //yMargin = (this.maxY - this.minY) / 10;

            //this.minX -= xMargin;
            //this.minY -= yMargin;
            //this.maxX += xMargin;
            //this.maxY += yMargin;

            this.displayRangeMinX = this.minX;
            //this.displayRangeMinY = targetMz - 10;
            this.displayRangeMinY = this.minY;
            this.displayRangeMaxX = this.maxX;
            //this.displayRangeMaxY = targetMz + 10;
            this.DisplayRangeMaxY = this.maxY;
        }

        private void SetInitializeValue(float targetMz) {
            SetInitializeValue();
            this.displayRangeMinX = this.minX;
            //this.displayRangeMinY = targetMz - 10;
            this.displayRangeMinY = this.minY;
            this.displayRangeMaxX = this.maxX;
            // this.displayRangeMaxY = targetMz + 10;
            this.DisplayRangeMaxY = this.maxY;
        }


        #region properties
        public ObservableCollection<double> XAxisDatapointCollection
        {
            get { return xAxisDatapointCollection; }
            set { xAxisDatapointCollection = value; }
        }

        public ObservableCollection<double> YAxisDatapointCollection
        {
            get { return yAxisDatapointCollection; }
            set { yAxisDatapointCollection = value; }
        }

        public bool MolcularIonFilter
        {
            get { return molcularIonFilter; }
            set { molcularIonFilter = value; }
        }

        public ObservableCollection<SolidColorBrush> PlotBrushCollection
        {
            get { return plotBrushCollection; }
            set { plotBrushCollection = value; }
        }

        public ObservableCollection<string> LabelCollection
        {
            get { return labelCollection; }
            set { labelCollection = value; }
        }

        public ObservableCollection<PeakAreaBean> PeakAreaBeanCollection
        {
            get { return peakAreaBeanCollection; }
            set { peakAreaBeanCollection = value; }
        }

        public ObservableCollection<DriftSpotBean> DriftSpots {
            get { return driftSpots; }
            set { driftSpots = value; }
        }

        public ObservableCollection<AlignmentPropertyBean> AlignmentPropertyBeanCollection
        {
            get { return alignmentPropertyBeanCollection; }
            set { alignmentPropertyBeanCollection = value; }
        }

        public List<MS1DecResult> Ms1DecResults
        {
            get { return ms1DecResults; }
            set { ms1DecResults = value; }
        }

        public PairwisePlotDisplayLabel DisplayLabel
        {
            get { return displayLabel; }
            set { displayLabel = value; }
        }

        public RetentionUnit RetentionUnit
        {
            get { return retentionUnit; }
            set { retentionUnit = value; }
        }

        public float MinX
        {
            get { return minX; }
            set { minX = value; }
        }

        public float MaxX
        {
            get { return maxX; }
            set { maxX = value; }
        }

        public float MinY
        {
            get { return minY; }
            set { minY = value; }
        }

        public float MaxY
        {
            get { return maxY; }
            set { maxY = value; }
        }

        public float RectangleRangeYmax
        {
            get { return rectangleRangeYmax; }
            set { rectangleRangeYmax = value; }
        }

        public float RectangleRangeYmin
        {
            get { return rectangleRangeYmin; }
            set { rectangleRangeYmin = value; }
        }

        public float RectangleRangeXmax
        {
            get { return rectangleRangeXmax; }
            set { rectangleRangeXmax = value; }
        }

        public float RectangleRangeXmin
        {
            get { return rectangleRangeXmin; }
            set { rectangleRangeXmin = value; }
        }

        
        public float? DisplayRangeMinX
        {
            get { return displayRangeMinX; }
            set { if (displayRangeMinX == value) return; displayRangeMinX = value; OnPropertyChanged("DisplayRangeMinX"); }
        }

        public float? DisplayRangeMaxX
        {
            get { return displayRangeMaxX; }
            set { if (displayRangeMaxX == value) return; displayRangeMaxX = value; OnPropertyChanged("DisplayRangeMaxX"); }
        }

        public float? DisplayRangeMinY
        {
            get { return displayRangeMinY; }
            set { if (displayRangeMinY == value) return; displayRangeMinY = value; OnPropertyChanged("DisplayRangeMinY"); }
        }

        public float? DisplayRangeMaxY
        {
            get { return displayRangeMaxY; }
            set { if (displayRangeMaxY == value) return; displayRangeMaxY = value; OnPropertyChanged("DisplayRangeMaxY"); }
        }

        public string GraphTitle
        {
            get { return graphTitle; }
            set { graphTitle = value; }
        }

        public string XAxisTitle
        {
            get { return xAxisTitle; }
            set { xAxisTitle = value; }
        }

        public string YAxisTitle
        {
            get { return yAxisTitle; }
            set { yAxisTitle = value; }
        }

        public int SelectedPlotId
        {
            get { return selectedPlotId; }
            set { if (selectedPlotId == value)
					return;
				Debug.WriteLine(" PWPB MS2 plot id changed to: " + value);
				selectedPlotId = value;
				OnPropertyChanged("SelectedPlotId");
			}
        }

        public int SelectedMs1DecID
        {
            get { return selectedMs1DecID; }
            set { if (selectedMs1DecID == value)
					return;
				Debug.WriteLine(" PWPB MS1 plot id changed to: " + value);
				selectedMs1DecID = value;
				OnPropertyChanged("SelectedMs1DecID");
			}
        }

		public int ScanNumber {
			get { return scanNumber; }
			set {
				if (value == scanNumber) { return; }
				scanNumber = value;
				OnPropertyChanged("ScanNumber");
			}
		}

		public float AmplitudeDisplayLowerFilter
        {
            get { return amplitudeDisplayLowerFilter; }
            set { amplitudeDisplayLowerFilter = value; }
        }

        public float AmplitudeDisplayUpperFilter
        {
            get { return amplitudeDisplayUpperFilter; }
            set { amplitudeDisplayUpperFilter = value; }
        }

        public bool IdentifiedOnlyDisplayFilter
        {
            get { return identifiedOnlyDisplayFilter; }
            set { identifiedOnlyDisplayFilter = value; }
        }

        public bool AnnotatedOnlyDisplayFilter
        {
            get { return annotatedOnlyDisplayFilter; }
            set { annotatedOnlyDisplayFilter = value; }
        }

        public bool MsmsOnlyDisplayFilter
        {
            get { return msmsOnlyDisplayFilter; }
            set { msmsOnlyDisplayFilter = value; }
        }

        public bool UnknownFilter
        {
            get { return unknownFilter; }
            set { unknownFilter = value; }
        }

        public int DisplayedSpotCount {
            get {
                return displayedSpotCount;
            }

            set {
                displayedSpotCount = value;
            }
        }

        public bool UniqueionFilter {
            get {
                return uniqueionFilter;
            }

            set {
                uniqueionFilter = value;
            }
        }

        public bool BlankFilter {
            get {
                return blankFilter;
            }

            set {
                blankFilter = value;
            }
        }

        public ObservableCollection<int> Indexes {
            get {
                return indexes;
            }

            set {
                indexes = value;
            }
        }

        public ObservableCollection<AlignedDriftSpotPropertyBean> AlignmentDriftSpotBean {
            get {
                return alignmentDriftSpotBean;
            }

            set {
                alignmentDriftSpotBean = value;
            }
        }

        public bool CcsFilter {
            get {
                return ccsFilter;
            }

            set {
                ccsFilter = value;
            }
        }
        #endregion

        #region // Required Methods for INotifyPropertyChanged
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler eventHandlers = this.PropertyChanged;
            if (null != eventHandlers)
                eventHandlers(this, e);
        }
        #endregion // Required Methods for INotifyPropertyChanged
    
    }
}
