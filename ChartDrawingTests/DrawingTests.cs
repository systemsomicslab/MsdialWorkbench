using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Media;

namespace ChartDrawing.Tests
{
    [TestClass()]
    public class DrawingTests
    {
        [TestMethod()]
        [Ignore("Ignore view tests.")]
        public void SaveChartTest() {
            Assert.Inconclusive();
            var area = new Area() { Height = 500, Width = 500 };
            area.AxisY.MinorScaleEnabled = false;
            var list = getTestValues();
            var title = new Title() { Label = "Test figure" };
            var drawing = new DrawVisual(area, title, list);
            drawing.InitializeElements();
            var dv = drawing.GetChart();
            //drawing.SaveChart(dv);
         }

        [TestMethod()]
        [Ignore("Ignore view tests.")]
        public void SaveMSwithRefTest() {
            var area = new Area() { Height = 500, Width = 500, LabelSpace = new LabelSpace() { Top = 50, Bottom = 30 }, Margin = new Margin(30, 30, 10, 20) };
            area.AxisX.IsItalicLabel = true;
            area.AxisX.FontSize = 20;
            area.AxisY.FontSize = 20;
            area.AxisX.AxisLabel = "m/z";
            area.AxisY.AxisLabel = "Relative intensity";
            area.AxisX.ScaleEnabled = false;
            area.AxisY.ScaleEnabled = false;

            var list = getTestMS();
            var title = new Title() { Label = "Test figure" };
            var drawing = new DrawVisual(area, title, list, true);
            drawing.MaxX *= 1.1f;
            if (drawing.MinX < 100)
                drawing.MinX = 0;

            else {
                drawing.MinX -= 50f;
            }
            drawing.Initialize();
            var dv = drawing.GetChart();
            
            drawing.SaveDrawingAsEmf(dv, @"C:\Users\tipputa\Desktop\drawingTest\image4.emf");
            //drawing.SaveChart(dv, @"C:\Users\tipputa\Desktop\drawingTest\image4.png");
        }

        [TestMethod()]
        [Ignore("Ignore view tests.")]
        public void SaveMSwithRefTest2() {
            var area = new Area() { Height = 500, Width = 500, LabelSpace = new LabelSpace() { Top = 50, Bottom = 30 } };
            area.AxisX.IsItalicLabel = true;
            area.AxisX.FontSize = 17;
            area.AxisY.FontSize = 17;
            area.AxisX.AxisLabel = "m/z";
            area.AxisY.AxisLabel = "Relative intensity";
            area.AxisY.MinorScaleEnabled = false;
            area.AxisX.MinorScaleEnabled = false;


            var list = getTestMS();
            var title = new Title() { Label = "Test figure" };
            var drawing = new DrawVisual(area, title, list, true);

            if (drawing.MinX < 100)
                drawing.MinX = 0;

            else {
                drawing.MinX -= 50f;
            }
            drawing.Initialize();
            if (drawing.SeriesList.MinX < 0) drawing.MinX = 0;
            var dv = drawing.GetChart();

            drawing.SaveDrawingAsEmf(dv, @"C:\Users\tipputa\Desktop\drawingTest\image3.emf");
            //drawing.SaveChart(dv, @"C:\Users\tipputa\Desktop\drawingTest\image3.png");
        }

        public SeriesList getTestMS() {
            var slist = new SeriesList();
            var s = new Series() { ChartType = ChartType.MSwithRef, MarkerType = MarkerType.None, Pen = new System.Windows.Media.Pen(Brushes.Black, 1.5) };
            var s2 = new Series() { ChartType = ChartType.MSwithRef, MarkerType = MarkerType.None, Pen = new System.Windows.Media.Pen(Brushes.Red, 1.5), Brush = Brushes.Red };

            var xTest = new float[] { 50, 200, 300, 500, 600 };
            var yTest = new float[] { 100, 50, 30, 10, 100 };
            for (var i = 0; i < xTest.Length; i++) {
                s.AddPoint(xTest[i], yTest[i], xTest[i].ToString("0.000"));
                s2.AddPoint(xTest[i] + 20, yTest[i], (xTest[i] + 20).ToString("0.000"));
            }
            s.IsLabelVisible = true;
            s2.IsLabelVisible = true;
            slist.Series.Add(s);
            slist.Series.Add(s2);
            return slist;
        }



        public SeriesList getTestValues() {
            var slist = new SeriesList();
            var s = new Series() { ChartType = ChartType.Point, MarkerType = MarkerType.Circle, MarkerSize = new System.Windows.Size(10, 10) };
            var s2 = new Series() { ChartType = ChartType.Point, MarkerType = MarkerType.Cross, MarkerSize = new System.Windows.Size(10, 10) };
            var s3 = new Series() { ChartType = ChartType.Line, MarkerType = MarkerType.Square, MarkerSize = new System.Windows.Size(10, 10) };

            var xTest = new float[] { 1, 2, 3, 4, 5 };
            var yTest = new float[] { 30, 40, 50, 10, 7 };
            for (var i = 0; i < xTest.Length; i++) {
                s.AddPoint(xTest[i], yTest[i], i.ToString());
                s2.AddPoint(xTest[i], yTest[i] + 1);
                s3.AddPoint(xTest[i], yTest[i] - 1f);
            }

            s.IsLabelVisible = true;
            slist.Series.Add(s);
            slist.Series.Add(s2);
            slist.Series.Add(s3);
            Debug.WriteLine(slist.MinX + "-" + slist.MaxX + ", " + slist.MinY + "-" + slist.MaxY);
            return slist;
        }
    }
}
 