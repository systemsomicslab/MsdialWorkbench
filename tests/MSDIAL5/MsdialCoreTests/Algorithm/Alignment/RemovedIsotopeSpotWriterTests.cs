using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.MsdialCore.Algorithm.Alignment.Tests;

[TestClass]
public class RemovedIsotopeSpotWriterTests
{
    [TestMethod]
    public void WriteListsSpotWithParentTest() {
        var spot = new AlignmentSpotProperty {
            MassCenter = 278.117, TimesCenter = new ChromXs(7.991), Name = "Unknown",
            HeightAverage = 2500f, FillParcentage = 1f, RepresentativeFileID = 3,
            PeakCharacter = new IonFeatureCharacter { IsotopeWeightNumber = 2, IsotopeParentPeakID = 41 },
        };
        var representative = new AlignmentChromPeakFeature { FileID = 3, FileName = "spiked-1", PeakID = 179, PeakHeightTop = 3000 };
        var parent = new AlignmentChromPeakFeature { FileID = 3, PeakID = 41, Mass = 276.1019, ChromXsTop = new ChromXs(7.990), PeakHeightTop = 300 };

        var spots = RemovedIsotopeSpot.Collect(new[] { spot }, _ => representative, new[] { representative, parent });
        var lines = WriteLines(spots);

        Assert.AreEqual(2, lines.Length);
        StringAssert.StartsWith(lines[0], "Average Rt(min)\tAverage Mz\tMetabolite name\t");
        Assert.AreEqual("7.991\t278.11700\tUnknown\t2500\t1.00\tM + 2\tspiked-1\t179\t3000\t41\t276.10190\t7.990\t300\t10.00", lines[1]);
    }

    [TestMethod]
    public void WriteLeavesParentBlankWhenNotFoundTest() {
        var spot = new AlignmentSpotProperty {
            MassCenter = 500.0, TimesCenter = new ChromXs(3.0), Name = string.Empty, RepresentativeFileID = 0,
            PeakCharacter = new IonFeatureCharacter { IsotopeWeightNumber = 1, IsotopeParentPeakID = 7 },
        };

        var spots = RemovedIsotopeSpot.Collect(new[] { spot }, _ => null, new AlignmentChromPeakFeature[0]);
        var cells = WriteLines(spots)[1].Split('\t');

        Assert.AreEqual(14, cells.Length);
        Assert.AreEqual("Unknown", cells[2]);
        Assert.AreEqual("M + 1", cells[5]);
        Assert.AreEqual("7", cells[9]);
        Assert.AreEqual("", cells[10]);
        Assert.AreEqual("", cells[13]);
    }

    [TestMethod]
    public void WriteHeaderOnlyWhenNothingRemovedTest() {
        var lines = WriteLines(new List<RemovedIsotopeSpot>());

        Assert.AreEqual(1, lines.Length);
    }

    private static string[] WriteLines(IReadOnlyList<RemovedIsotopeSpot> spots) {
        using var stream = new MemoryStream();
        RemovedIsotopeSpotWriter.Write(stream, spots);
        return Encoding.UTF8.GetString(stream.ToArray()).TrimEnd('\n').Split('\n');
    }
}
