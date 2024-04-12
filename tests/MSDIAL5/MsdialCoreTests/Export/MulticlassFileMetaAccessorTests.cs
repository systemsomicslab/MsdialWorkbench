using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.MsdialCore.Export.Tests;

[TestClass]
public class MulticlassFileMetaAccessorTests
{
    [TestMethod]
    public void Constructor_WithClasses_SetsAdditionalClasses()
    {
        // Arrange
        string[] expectedClasses = ["Class1", "Class2"];

        // Act
        var accessor = new MulticlassFileMetaAccessor(expectedClasses);

        // Assert
        CollectionAssert.AreEqual(expectedClasses, accessor.AdditionalClasses);
    }

    [TestMethod]
    public void Constructor_WithNumClasses_GeneratesCorrectClassNames()
    {
        // Arrange
        int numClasses = 2;
        string[] expectedClasses = ["Parameter1", "Parameter2"];

        // Act
        var accessor = new MulticlassFileMetaAccessor(numClasses);

        // Assert
        CollectionAssert.AreEqual(expectedClasses, accessor.AdditionalClasses);
    }

    [TestMethod]
    public void GetHeaders_IncludesAllFieldsCorrectly()
    {
        // Arrange
        var accessor = new MulticlassFileMetaAccessor(["Class1", "Class2"]);
        string[] expectedHeaders = ["Class", "Class1", "Class2", "File type", "Injection order", "Batch ID"];

        // Act
        var headers = accessor.GetHeaders();

        // Assert
        CollectionAssert.AreEqual(expectedHeaders, headers.ToArray());
    }

    [TestMethod]
    public void GetContent_CreatesCorrectContentArray()
    {
        // Arrange
        var accessor = new MulticlassFileMetaAccessor(["Parameter1", "Parameter2"]);
        var file = new AnalysisFileBean
        {
            AnalysisFileClass = "A_B",
            AnalysisFileType = AnalysisFileType.Sample,
            AnalysisFileAnalyticalOrder = 1,
            AnalysisBatch = 1
        };
        string[] expectedContent = ["A_B", "A", "B", "Sample", "1", "1"];

        // Act
        var content = accessor.GetContent(file);

        // Assert
        CollectionAssert.AreEqual(expectedContent, content);
    }

    [TestMethod]
    public void GetContents_WithMultipleFiles_ReturnsCorrectArrays()
    {
        // Arrange
        var accessor = new MulticlassFileMetaAccessor(["Parameter1", "Parameter2"]);
        var files = new[]
        {
            new AnalysisFileBean { AnalysisFileClass = "A_B", AnalysisFileType = AnalysisFileType.Blank, AnalysisFileAnalyticalOrder = 1, AnalysisBatch = 1},
            new AnalysisFileBean { AnalysisFileClass = "C_D", AnalysisFileType = AnalysisFileType.Sample, AnalysisFileAnalyticalOrder = 2, AnalysisBatch = 2}
        };
        var expected = new string[][]
        {
            ["A_B", "A", "B", "Blank", "1", "1"],
            ["C_D", "C", "D", "Sample", "2", "2"]
        };

        // Act
        var contents = accessor.GetContents(files);

        // Assert
        for (int i = 0; i < contents.Length; i++)
        {
            CollectionAssert.AreEqual(expected[i], contents[i]);
        }
    }
}
