using System;
using System.Collections.Generic;
using System.Linq;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.ChartData
{
    public class MatrixChartData : DefaultChartData
    {
        public SeriesList Data { get; set; }

        public MatrixChartData(double[,] matrix, IReadOnlyList<string> xlabels, IReadOnlyList<string> ylabels)
        {
            DataMinX = LimitMinX = 0;
            DataMaxX = LimitMaxX = matrix.GetLength(1);
            DataMinY = LimitMinY = 0;
            DataMaxY = LimitMaxY = matrix.GetLength(0);
            DataMinZ = LimitMinZ = matrix.Cast<double>().Min();
            DataMaxZ = LimitMaxZ = matrix.Cast<double>().Max();
        }
    }
}
