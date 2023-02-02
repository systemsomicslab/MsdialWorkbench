using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public enum AccuracyType { IsNominal, IsAccurate }

    public enum SmoothingMethod
    {
        SimpleMovingAverage,
        LinearWeightedMovingAverage,
        SavitzkyGolayFilter,
        BinomialFilter,
        LowessFilter,
        LoessFilter
    }

    public enum ScaleMethod
    {
        None, MeanCenter, ParetoScale, AutoScale
    }

    public enum TransformMethod
    {
        None, Log10, QuadRoot
    }

    public enum DeconvolutionType { One, Both }

    public enum CoverRange { CommonRange, ExtendedRange, ExtremeRange }

}
