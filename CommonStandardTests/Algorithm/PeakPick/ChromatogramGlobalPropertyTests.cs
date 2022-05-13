using CompMs.Common.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Algorithm.PeakPick.Tests
{
    [TestClass()]
    public class ChromatogramGlobalPropertyTests
    {
        private static List<ChromatogramPeak> BuildChromatogramSample() {
            return new List<ChromatogramPeak>
            {
                new ChromatogramPeak( 0, 100d,   100d, new RetentionTime( 0d)),
                new ChromatogramPeak( 1, 100d, 10000d, new RetentionTime( 1d)),
                new ChromatogramPeak( 2, 100d,  1000d, new RetentionTime( 2d)),
                new ChromatogramPeak( 3, 100d,  1000d, new RetentionTime( 3d)),
                new ChromatogramPeak( 4, 100d,   100d, new RetentionTime( 4d)),
                new ChromatogramPeak( 5, 100d,  1000d, new RetentionTime( 5d)),
                new ChromatogramPeak( 6, 100d, 10000d, new RetentionTime( 6d)),
                new ChromatogramPeak( 7, 100d,  1000d, new RetentionTime( 7d)),
                new ChromatogramPeak( 8, 100d,   100d, new RetentionTime( 8d)),
                new ChromatogramPeak( 9, 100d,  1000d, new RetentionTime( 9d)),
                new ChromatogramPeak(10, 100d, 10000d, new RetentionTime(10d)),
                new ChromatogramPeak(11, 100d, 10000d, new RetentionTime(11d)),
                new ChromatogramPeak(12, 100d,  1000d, new RetentionTime(12d)),
                new ChromatogramPeak(13, 100d,   100d, new RetentionTime(13d)),
                new ChromatogramPeak(14, 100d,   100d, new RetentionTime(14d)),
                new ChromatogramPeak(15, 100d,  1000d, new RetentionTime(15d)),
            };
        }
    }
}
