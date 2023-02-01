using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.BarChart
{
    public class BarChartBean : ViewModelBase
    {
        private List<BarElement> barElements;
        private List<BarElement> displayedBarElements;

        private double maxValue;
        private double minValue;
        private string yAxisTitle;
        private string xAxisTitle;
        private string mainTitle;

        private int displayedBeginID;
        private int displayedEndID;

        private bool isBoxPlot;

        public BarChartBean(List<BarElement> barElements, 
            string mainTitle, string xAxisTitle = "", string yAxisTitle = "")
        {
            if (barElements == null || barElements.Count == 0) {
                initialization(null);
                return;
            }

            this.barElements = barElements;
            this.mainTitle = mainTitle;
            this.xAxisTitle = xAxisTitle;
            this.yAxisTitle = yAxisTitle;

            initialization(barElements);
        }

        public BarChartBean()
        {
            this.mainTitle = "";
            this.xAxisTitle = "";
            this.yAxisTitle = "";

            initialization(null);
        }

        private void initialization(List<BarElement> barElements)
        {
            if (barElements == null) return;
            this.maxValue = double.MinValue;
            this.minValue = double.MaxValue;
            this.displayedBarElements = new List<BarElement>();

            foreach (var element in barElements)
            {
                if (element.IsBoxPlot == false) {
                    var max = element.Value + element.Error;
                    var min = 0;
                    if (this.maxValue < max) this.maxValue = max;
                    if (this.minValue > min) this.minValue = min;
                }
                else {
                    var max = element.MaxValue;
                    var min = element.MinValue;
                    if (this.maxValue < max) this.maxValue = max;
                    if (this.minValue > min) this.minValue = min;
                    this.IsBoxPlot = true;
                }

                this.displayedBarElements.Add(element);
            }
            if (this.isBoxPlot == true) {
                this.minValue = this.minValue * 0.9;
            }

            this.displayedBeginID = 0;
            this.displayedEndID = barElements.Count - 1;
        }

        public void DisplayElementRearrangement(int beginID, int endID)
        {
            if (beginID < 0 || endID > this.barElements.Count - 1) return;
            if (endID - beginID < 5) return;
            if (beginID == this.displayedBeginID && endID == this.displayedEndID) return;

            this.displayedBeginID = beginID;
            this.displayedEndID = endID;

            this.displayedBarElements = new List<BarElement>();
            this.maxValue = double.MinValue;
            this.minValue = double.MaxValue;

            for (int i = beginID; i <= endID; i++) {

                var element = this.barElements[i];
                if (element.IsBoxPlot == false) {
                    var max = element.Value + element.Error;
                    var min = 0.0;
                    if (this.maxValue < max) this.maxValue = max;
                    if (this.minValue > min) this.minValue = min;
                }
                else {
                    var max = element.MaxValue;
                    var min = element.MinValue;
                    if (this.maxValue < max) this.maxValue = max;
                    if (this.minValue > min) this.minValue = min;
                    this.IsBoxPlot = true;
                }
                this.displayedBarElements.Add(this.barElements[i]);
            }
            if (this.isBoxPlot == true) {
                this.minValue = this.minValue * 0.9;
            }
        }

        public List<BarElement> BarElements
        {
            get { return barElements; }
            set { barElements = value; }
        }

        public List<BarElement> DisplayedBarElements
        {
            get { return displayedBarElements; }
            set { displayedBarElements = value; }
        }

        public int DisplayedBeginID
        {
            get { return displayedBeginID; }
            set { displayedBeginID = value; }
        }

        public int DisplayedEndID
        {
            get { return displayedEndID; }
            set { displayedEndID = value; }
        }

        public double MaxValue
        {
            get { return maxValue; }
            set { maxValue = value; }
        }

        public double MinValue
        {
            get { return minValue; }
            set { minValue = value; }
        }

        public string YAxisTitle
        {
            get { return yAxisTitle; }
            set { yAxisTitle = value; }
        }

        public string XAxisTitle
        {
            get { return xAxisTitle; }
            set { xAxisTitle = value; }
        }

        public string MainTitle
        {
            get { return mainTitle; }
            set { mainTitle = value; }
        }

        public bool IsBoxPlot {
            get {
                return isBoxPlot;
            }

            set {
                isBoxPlot = value;
            }
        }
    }
}
