using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace CompMs.Common.Parser.Tests;

[TestClass]
public class CsvWriterTests
{
    [TestMethod]
    public void WriteRow_CustomDelimiter_WritesCorrectOutput() {
        using var memoryStream = new MemoryStream();
        using (var writer = new CsvWriter(memoryStream, leaveOpen: true)
        {
            Delimiter = '\t'
        }) {
            writer.WriteRow("A", "B", "C");
            writer.WriteRow("D", "E", "F");
        }

        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        string result = reader.ReadToEnd();

        Assert.AreEqual("A\tB\tC\r\nD\tE\tF\r\n", result);
    }

    [TestMethod]
    public void WriteRow_DefaultDelimiter_WritesCorrectOutput() {
        using var memoryStream = new MemoryStream();
        using (var writer = new CsvWriter(memoryStream, leaveOpen: true)) {
            writer.WriteRow("A", "B", "C");
            writer.WriteRow("D", "E", "F");
        }

        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        string result = reader.ReadToEnd();

        Assert.AreEqual("A,B,C\r\nD,E,F\r\n", result);
    }

    [TestMethod]
    public void CutByExcelLimit_TruncatesLongValues() {
        using var memoryStream = new MemoryStream();
        using (var writer = new CsvWriter(memoryStream, leaveOpen: true)
        {
            CutByExcelLimit = true
        }) {
            string longValue = new string('X', 40000);
            writer.WriteRow(longValue);
        }

        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        string result = reader.ReadToEnd();

        Assert.AreEqual(new string('X', 32767) + "\r\n", result);
    }

    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void WriteRow_AfterDispose_ThrowsException() {
        var memoryStream = new MemoryStream();
        var writer = new CsvWriter(memoryStream, leaveOpen: true);

        writer.Dispose();
        writer.WriteRow("A", "B", "C");
    }

    [TestMethod]
    public void Dispose_ClosesStreamProperly() {
        var memoryStream = new MemoryStream();
        using (var writer = new CsvWriter(memoryStream, leaveOpen: false)) {
            writer.WriteRow("Test");
        }

        Assert.ThrowsException<ObjectDisposedException>(() => {
            memoryStream.WriteByte(0);
        });
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void WriteRow_NullValues_WritesCorrectOutput(bool cutByExcelLimit) {
        using var memoryStream = new MemoryStream();
        using (var writer = new CsvWriter(memoryStream, leaveOpen: true)
        {
            CutByExcelLimit = cutByExcelLimit
        }) {
            writer.WriteRow("A", null, "C");
            writer.WriteRow(null, "E", null);
        }

        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        string result = reader.ReadToEnd();

        Assert.AreEqual("A,,C\r\n,E,\r\n", result);
    }
}
