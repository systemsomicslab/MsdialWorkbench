using CompMs.App.MsdialConsole.Process;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace MsdialCoreTestAppTests.Process;

[TestClass]
public sealed class ConsoleLineFilterTests
{
    [TestMethod]
    public void SuppressExact_HidesOnlyTheRequestedLineWithinScope() {
        var original = Console.Out;
        var output = new StringWriter();
        try {
            Console.SetOut(output);
            Console.WriteLine("Reading data...");
            using (ConsoleLineFilter.SuppressExact("Reading data...")) {
                Console.WriteLine("Reading data...");
                Console.WriteLine("Alignment progress: 50%");
                Console.WriteLine("Reading data... additional detail");
            }
            Console.WriteLine("Reading data...");
        }
        finally {
            Console.SetOut(original);
        }

        var lines = output.ToString().Split(
            new[] { Environment.NewLine },
            StringSplitOptions.RemoveEmptyEntries);
        CollectionAssert.AreEqual(
            new[] {
                "Reading data...",
                "Alignment progress: 50%",
                "Reading data... additional detail",
                "Reading data...",
            },
            lines);
    }
}
