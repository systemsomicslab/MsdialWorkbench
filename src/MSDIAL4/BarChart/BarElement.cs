using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Common.BarChart
{
    public class BarElement
    {
        // general prop
        private int id;
        private string legend;
        private SolidColorBrush brush;
        private bool isBoxPlot; // bar chart in default

        // normal bar chart
        private double value;
        private double error;

        // box plot
        private double maxValue;
        private double seventyFiveValue;
        private double median;
        private double twentyFiveValue;
        private double minValue;

        public BarElement()
        {
            brush = Brushes.Blue;
        }

        public SolidColorBrush Brush
        {
            get { return brush; }
            set { brush = value; }
        }

        public double Value
        {
            get { return this.value; }
            set { this.value = value; }
        }

        public double Error
        {
            get { return error; }
            set { error = value; }
        }

        public string Legend
        {
            get { return legend; }
            set { legend = value; }
        }

        public int Id {
            get { return id; }
            set { id = value; }
        }

        public double MaxValue {
            get {
                return maxValue;
            }

            set {
                maxValue = value;
            }
        }

        public double SeventyFiveValue {
            get {
                return seventyFiveValue;
            }

            set {
                seventyFiveValue = value;
            }
        }

        public double Median {
            get {
                return median;
            }

            set {
                median = value;
            }
        }

        public double TwentyFiveValue {
            get {
                return twentyFiveValue;
            }

            set {
                twentyFiveValue = value;
            }
        }

        public double MinValue {
            get {
                return minValue;
            }

            set {
                minValue = value;
            }
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
