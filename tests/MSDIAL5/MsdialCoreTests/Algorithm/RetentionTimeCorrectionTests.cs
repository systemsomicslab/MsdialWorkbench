using CompMs.Common.Components;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Tests;

/// <summary>
/// Unit tests for the RetentionTimeCorrection class.
/// </summary>
[TestClass()]
public class RetentionTimeCorrectionTests
{
    /// <summary>
    /// This test method is used to fix the IndexOutOfRange exception that occurs when the number of CommonStdData and StandardPair does not match in GetRetentionTimeCorrectionBean_SampleMinusAverage.
    /// </summary>
    [TestMethod()]
    public void GetRetentionTimeCorrectionBean_SampleMinusAverage_PartialStandardUsed() {
        var references = new List<MoleculeMsReference> { new(), new(), new(), };
        var stds = new List<StandardPair>
            {
                new() { SamplePeakAreaBean = new() { ChromXs = new ChromXs(new RetentionTime(2d)) }, Reference = references[0] },
                new() { SamplePeakAreaBean = new() { ChromXs = new ChromXs(new RetentionTime(1d)) }, Reference = references[1] },
                new() { SamplePeakAreaBean = new() { ChromXs = new ChromXs(new RetentionTime(3d)) }, Reference = references[2] },
            };
        var commons = new List<CommonStdData> { new(references[1]) { AverageRetentionTime = 1.5f }, };
        var parameter = new RetentionTimeCorrectionParam { InterpolationMethod = (InterpolationMethod)(-1) /*fake interpolation method */ };

        _ = RetentionTimeCorrection.GetRetentionTimeCorrectionBean_SampleMinusAverage(parameter, stds, [], commons);
    }
}
