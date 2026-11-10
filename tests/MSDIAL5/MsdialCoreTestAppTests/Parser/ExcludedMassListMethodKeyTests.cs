using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Enum;
using CompMs.Common.Query;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MsdialCoreTestAppTests.Parser;

/// <summary>
/// The exclusion mass list's method-file key: read into the parameter the GUI's list lives in by the four modes whose
/// peak spotting consults it, in the spelling a person types and in the one the GUI's parameter text writes, refused
/// whole when one entry cannot be read, unknown to DIMS, and written blank by those four modes' templates.
/// </summary>
[TestClass]
public sealed class ExcludedMassListMethodKeyTests
{
    private static readonly (string Mode, Func<string, ParameterBase> Read)[] ModesThatUseTheList = {
        ("gcms", path => ConfigParser.ReadForGcms(path)),
        ("lcms", path => ConfigParser.ReadForLcmsParameter(path)),
        ("imms", path => ConfigParser.ReadForImmsParameter(path)),
        ("lcimms", path => ConfigParser.ReadForLcImMsParameter(path)),
    };

    [TestMethod]
    public void EveryModeWhosePeakSpottingUsesTheListReadsIt()
    {
        using var directory = new TemporaryDirectory();
        foreach (var (mode, read) in ModesThatUseTheList) {
            var methodFile = directory.CreateFile($"{mode}.txt", "Exclude mass list: 149:0.5; 207:0.5; 281:0.5");

            var parameter = read(methodFile);

            CollectionAssert.AreEqual(new[] { (149d, 0.5d), (207d, 0.5d), (281d, 0.5d) }, Pairs(parameter.ExcludedMassList), mode);
            CollectionAssert.AreEqual(new[] { "Exclude mass list" }, RecordedKeys(methodFile, "applied"), mode);
            // The list the GUI's peak spotting consults is the one set, with its strict tolerance.
            Assert.IsTrue(parameter.PeakPickBaseParam.ShouldExclude(149.4), mode);
            Assert.IsFalse(parameter.PeakPickBaseParam.ShouldExclude(149.5), mode);
            Assert.IsFalse(parameter.PeakPickBaseParam.ShouldExclude(150.0), mode);
        }
    }

    [TestMethod]
    [DataRow("Exclude mass list: 149:0.5; 207:0.5; 281:0.5")]
    [DataRow("Exclude mass list: 149 0.5;207 0.5;281 0.5")]
    [DataRow("Exclude mass list: 149:0.5;207:0.5;281:0.5;")]
    [DataRow("Exclude mass list: 149\t0.5; 207\t0.5; 281\t0.5")]
    [DataRow("Excluded mass list: 149:0.5 ; 207:0.5 ; 281:0.5")]
    [DataRow("exclusion mass list = \"149:0.5; 207:0.5; 281:0.5\"")]
    public void ThePairsAreReadInEverySpellingOfTheList(string line)
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", line);

        var parameter = ConfigParser.ReadForGcms(methodFile);

        CollectionAssert.AreEqual(new[] { (149d, 0.5d), (207d, 0.5d), (281d, 0.5d) }, Pairs(parameter.ExcludedMassList));
        Assert.AreEqual(1, RecordedKeys(methodFile, "applied").Count);
    }

    /// <summary>
    /// A list the GUI exported as parameter text is a method-file line the Console applies.
    /// </summary>
    [TestMethod]
    public void TheLineTheGuisParameterTextWritesIsReadBackAsTheSameList()
    {
        var original = new MsdialLcmsParameter();
        original.ExcludedMassList = new List<MzSearchQuery> {
            new MzSearchQuery { Mass = 149.0233, MassTolerance = 0.01 },
            new MzSearchQuery { Mass = 391.2843, MassTolerance = 0.005 },
        };
        string line;
        var culture = Thread.CurrentThread.CurrentCulture;
        try {
            // ParametersAsText writes numbers in the current culture; the method file format is invariant.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            line = original.ParametersAsText().Single(text => text.StartsWith("Exclude mass list:", StringComparison.Ordinal));
        }
        finally {
            Thread.CurrentThread.CurrentCulture = culture;
        }
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", line);

        var parameter = ConfigParser.ReadForLcmsParameter(methodFile);

        CollectionAssert.AreEqual(Pairs(original.ExcludedMassList), Pairs(parameter.ExcludedMassList));
    }

    /// <summary>
    /// The GUI's tolerance has no upper bound, and two numbers are always an m/z and then its tolerance, whatever their
    /// size: the author decided "149 207" is the mass 149 with a tolerance of 207, not two masses.
    /// </summary>
    [TestMethod]
    [DataRow("149:1", 149d, 1d)]
    [DataRow("149:2.5", 149d, 2.5d)]
    [DataRow("149 207", 149d, 207d)]
    [DataRow("207 149", 207d, 149d)]
    public void AToleranceOfOneOrMoreIsReadAsTheWindowOfTheMassBeforeIt(string value, double mass, double tolerance)
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", $"Exclude mass list: {value}");

        var parameter = ConfigParser.ReadForGcms(methodFile);

        CollectionAssert.AreEqual(new[] { (mass, tolerance) }, Pairs(parameter.ExcludedMassList), value);
        CollectionAssert.AreEqual(new[] { "Exclude mass list" }, RecordedKeys(methodFile, "applied"), value);
        Assert.IsTrue(parameter.PeakPickBaseParam.ShouldExclude(mass + tolerance * 0.99), value);
        Assert.IsFalse(parameter.PeakPickBaseParam.ShouldExclude(mass + tolerance), value);
    }

    /// <summary>
    /// Dropping one unreadable entry would quietly keep the peaks it was meant to remove.
    /// </summary>
    [TestMethod]
    [DataRow("149")]
    [DataRow("149:0.5; 207")]
    [DataRow("149:-0.5")]
    [DataRow("-149:0.5")]
    [DataRow("abc:0.5")]
    [DataRow("149:0.5:1")]
    [DataRow("149:Infinity")]
    [DataRow("149:NaN")]
    [DataRow(";")]
    // Masses typed without tolerances, where an entry does not hold exactly two numbers.
    [DataRow("149; 207")]
    [DataRow("149 207 281")]
    // A comma is not a separator: "149,5" read as two numbers would be the mass 149 with a tolerance of 5.
    [DataRow("149, 207")]
    [DataRow("149,5")]
    [DataRow("149:0.5, 207:0.5")]
    [DataRow("149 0,5")]
    [DataRow("149,5 0,5")]
    // The GUI refuses a typed tolerance below 0.0001, and so does a method file, even a zero the GUI stored and exported.
    [DataRow("149:0")]
    [DataRow("149:0.00009")]
    [DataRow("0 0.5;149 0.5;207 0")]
    public void AListWithAnEntryThatCannotBeReadIsRefusedWhole(string value)
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", $"Exclude mass list: {value}");

        var parameter = ConfigParser.ReadForGcms(methodFile);

        Assert.AreEqual(0, parameter.ExcludedMassList.Count, value);
        CollectionAssert.AreEqual(new[] { $"Exclude mass list: {value}" }, RecordedKeys(methodFile, "unusable"), value);
        Assert.AreEqual(0, RecordedKeys(methodFile, "applied").Count, value);
    }

    /// <summary>
    /// The GUI takes a mass of zero and a tolerance of 0.0001, so a list holding either is read.
    /// </summary>
    [TestMethod]
    public void AMassOfZeroAndTheSmallestToleranceTheGuiTakesAreRead()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", "Exclude mass list: 0 0.5;149 0.0001");

        var parameter = ConfigParser.ReadForGcms(methodFile);

        CollectionAssert.AreEqual(new[] { (0d, 0.5d), (149d, 0.0001d) }, Pairs(parameter.ExcludedMassList));
        Assert.IsTrue(parameter.PeakPickBaseParam.ShouldExclude(149.00005));
        Assert.IsFalse(parameter.PeakPickBaseParam.ShouldExclude(149.0002));
    }

    [TestMethod]
    public void ABlankListKeepsEveryPeak()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", "Exclude mass list:");

        var parameter = ConfigParser.ReadForGcms(methodFile);

        Assert.AreEqual(0, parameter.ExcludedMassList.Count);
        CollectionAssert.AreEqual(new[] { "Exclude mass list" }, RecordedKeys(methodFile, "blank"));
    }

    [TestMethod]
    public void TheLastLineOfAListWrittenTwiceIsTheOneUsed()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Exclude mass list: 149:0.5
            Exclude mass list: 207:0.5; 281:0.5
            """);

        var parameter = ConfigParser.ReadForGcms(methodFile);

        CollectionAssert.AreEqual(new[] { (207d, 0.5d), (281d, 0.5d) }, Pairs(parameter.ExcludedMassList));
    }

    /// <summary>
    /// DIMS processing never reads the list, so a DIMS method file's key had no effect and is said to have none.
    /// </summary>
    [TestMethod]
    public void DimsReportsTheKeyAsUnrecognised()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", "Exclude mass list: 149:0.5");

        var parameter = ConfigParser.ReadForDimsParameter(methodFile);

        Assert.AreEqual(0, parameter.ExcludedMassList.Count);
        CollectionAssert.AreEqual(new[] { "Exclude mass list" }, RecordedKeys(methodFile, "unrecognised"));
    }

    [TestMethod]
    [DataRow(MethodFileMode.Gcms, true)]
    [DataRow(MethodFileMode.Lcms, true)]
    [DataRow(MethodFileMode.Imms, true)]
    [DataRow(MethodFileMode.Lcimms, true)]
    [DataRow(MethodFileMode.Dims, false)]
    public void TheTemplatesOfTheModesThatUseTheListWriteItBlank(MethodFileMode mode, bool written)
    {
        var template = MethodFileTemplate.Create(mode, IonMode.Positive);

        Assert.AreEqual(written, template.Contains("\nExclude mass list:\n"), mode.ToString());
        if (written) {
            StringAssert.Contains(template, "149:0.5; 207:0.5; 281:0.5");
        }
    }

    [TestMethod]
    public void TheGcmsTemplateSaysTheIonsAreLeftOutOfTheDeconvolutedSpectra()
    {
        StringAssert.Contains(MethodFileTemplate.Create(MethodFileMode.Gcms), "left out of every deconvoluted spectrum");
        Assert.IsFalse(MethodFileTemplate.Create(MethodFileMode.Lcms).Contains("deconvoluted spectrum"));
    }

    [TestMethod]
    public void AGcmsTemplateWithTheListFilledInReadsBackWithIt()
    {
        var template = MethodFileTemplate.Create(MethodFileMode.Gcms)
            .Replace("\nExclude mass list:\n", "\nExclude mass list: 149:0.5; 207:0.5; 281:0.5\n");
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", template);

        var parameter = (MsdialGcmsParameter)ConfigParser.ReadForGcms(methodFile);

        CollectionAssert.AreEqual(new[] { (149d, 0.5d), (207d, 0.5d), (281d, 0.5d) }, Pairs(parameter.ExcludedMassList));
        Assert.AreEqual(0, RecordedKeys(methodFile, "unrecognised").Count);
        Assert.AreEqual(0, RecordedKeys(methodFile, "unusable").Count);
    }

    private static (double, double)[] Pairs(IEnumerable<MzSearchQuery> queries)
    {
        return queries.Select(query => (query.Mass, query.MassTolerance)).ToArray();
    }

    private static List<string> RecordedKeys(string methodFile, string list)
    {
        var record = Path.Combine(
            Path.GetDirectoryName(methodFile)!,
            Path.GetFileNameWithoutExtension(methodFile) + ".keys.json");
        return JObject.Parse(File.ReadAllText(record))[list]!.Select(item => (string)item!).ToList();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "MsdialCoreTestAppTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateFile(string name, string content = "")
        {
            var path = System.IO.Path.Combine(Path, name);
            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return path;
        }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
