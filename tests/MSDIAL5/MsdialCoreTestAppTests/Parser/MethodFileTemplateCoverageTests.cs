using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MsdialCoreTestAppTests.Parser;

/// <summary>
/// EVERY KEY THE READER ACCEPTS IS OFFERED BY A TEMPLATE, OR LEFT OUT FOR A STATED REASON.
/// </summary>
/// <remarks>
/// MethodFileTemplateTests hold the template to the reader in one direction: what the template
/// writes, the reader claims. This holds the other direction. A key added to ConfigParser without a
/// template line used to go unnoticed: the EAD keys ("Collision type", the generated lipid library)
/// were readable but missing from `template lcms`, and an EAD run started from the template kept the
/// CID cut-offs for the LBM library until a run on real data showed it.
///
/// The keys are the case labels of ConfigParser.cs, read from the source, so a new label is seen
/// without anyone listing it. A label that is not offered needs an entry in <see cref="NotOffered"/>
/// saying why; an entry whose label is offered, or no longer read, fails too, so the list stays true.
/// </remarks>
[TestClass]
public sealed class MethodFileTemplateCoverageTests
{
    private const string Alias = "another spelling of a key the template writes";
    private const string Shorthand = "sets two keys the template writes at once";
    private const string SquaredScale = "the squared form of a cut-off the template writes as its square root";
    private const string NotAKey = "a value of the Library kind column of an annotator settings table, not a method-file key";
    private const string MolecularNetworking = "a key of the msn command's parameter file, which has no template";
    private const string NotWorkingInConsole = "a feature that does not work from the Console yet (see MethodFileTemplate's remarks)";
    private const string LabInternal = "a lab-internal switch";
    private const string MobilityOnly = "CCS of the generated lipid library, which only LC-MS and DIMS build and neither has ion mobility";
    private const string MassAccuracyPreview = "a diagnostic run that measures the m/z error, writes its report and stops before alignment, or a setting of that run; not part of a processing method";
    // Not offered and no reason recorded yet. Move a key out of here by offering it in the template
    // or by giving it one of the reasons above.
    private const string Undecided = "not offered; no reason recorded yet";

    private static readonly Dictionary<string, string> NotOffered = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["excluded mass list"] = Alias,
        ["exclusion mass list"] = Alias,
        ["alignment light"] = Alias,
        ["console alignment light mode"] = Alias,
        ["lbm annotation priority"] = Alias,
        ["annotation candidates"] = Alias,
        ["export detailed alignment provenance"] = Alias,
        ["generated lipid annotation"] = Alias,
        ["generated lipid annotation priority"] = Alias,
        ["msp annotation settings file path"] = Alias,
        ["msp search settings file path"] = Alias,
        ["text library annotator settings file path"] = Alias,
        ["text db annotator settings file path"] = Alias,
        ["text annotation settings file path"] = Alias,
        ["ri index file pathes"] = Alias,
        ["ri index file paths"] = Alias,
        ["ri dictionary file paths"] = Alias,
        ["ri compound"] = Alias,
        ["retention index alignment tolerance"] = Alias,
        ["accumulated rt ragne"] = Alias,
        ["mass accuracy"] = Alias,
        ["ri tolerance for identification"] = Alias,
        ["retention index tolerance for identification"] = Alias,
        ["weighted dot product cutoff"] = Alias,
        ["simple dot product cutoff"] = Alias,
        ["reverse dot product cutoff"] = Alias,
        ["matched peaks percentage cutoff"] = Alias,
        ["minimum spectrum match"] = Alias,
        ["execute annotation process only for alignment file for msp-based annotation"] = Alias,
        ["execute annotation process only for alignment file for lbm-based annotation"] = Alias,

        ["sample max / blank average"] = Shorthand,
        ["sample average / blank average"] = Shorthand,

        ["weighted dot product cutoff for msp-based annotation"] = SquaredScale,
        ["simple dot product cutoff for msp-based annotation"] = SquaredScale,
        ["reverse dot product cutoff for msp-based annotation"] = SquaredScale,
        ["weighted dot product cutoff for lbm-based annotation"] = SquaredScale,
        ["simple dot product cutoff for lbm-based annotation"] = SquaredScale,
        ["reverse dot product cutoff for lbm-based annotation"] = SquaredScale,

        ["predicted"] = NotAKey,
        ["insilico"] = NotAKey,
        ["computed"] = NotAKey,
        ["generated"] = NotAKey,
        ["predictedmsp"] = NotAKey,
        ["acquired"] = NotAKey,
        ["experimental"] = NotAKey,
        ["measured"] = NotAKey,
        ["msp"] = NotAKey,

        ["mnprecursorwindow"] = MolecularNetworking,
        ["mnintensitypower"] = MolecularNetworking,
        ["mnrttolerance"] = MolecularNetworking,
        ["mnioncorrelationsimilaritycutoff"] = MolecularNetworking,
        ["mnspectrumsimilaritycutoff"] = MolecularNetworking,
        ["mnrelativeabundancecutoff"] = MolecularNetworking,
        ["mnmasstolerance"] = MolecularNetworking,
        ["minimumpeakmatch"] = MolecularNetworking,
        ["maxedgenumberpernode"] = MolecularNetworking,
        ["maxprecursordifference"] = MolecularNetworking,
        ["mnabsoluteabundancecutoff"] = MolecularNetworking,
        ["mnisexportioncorrelation"] = MolecularNetworking,

        ["corrdec execute"] = NotWorkingInConsole,
        ["corrdec ms2 tolerance"] = NotWorkingInConsole,
        ["corrdec minimum ms2 peak height"] = NotWorkingInConsole,
        ["corrdec minimum number of detected samples"] = NotWorkingInConsole,
        ["corrdec exclude highly correlated spots"] = NotWorkingInConsole,
        ["corrdec minimum correlation coefficient (ms2)"] = NotWorkingInConsole,
        ["corrdec margin 1 (target precursor)"] = NotWorkingInConsole,
        ["corrdec margin 2 (coeluted precursor)"] = NotWorkingInConsole,
        ["corrdec minimum detected rate"] = NotWorkingInConsole,
        ["corrdec minimum ms2 relative intensity"] = NotWorkingInConsole,
        ["corrdec remove peaks larger than precursor"] = NotWorkingInConsole,
        ["tracking isotope label"] = NotWorkingInConsole,
        ["set fully labeled reference file"] = NotWorkingInConsole,
        ["non labeled reference id"] = NotWorkingInConsole,
        ["fully labeled reference id"] = NotWorkingInConsole,
        ["isotope tracking dictionary id"] = NotWorkingInConsole,
        ["isotope text db file path"] = NotWorkingInConsole,
        ["compounds library file path for target detection"] = NotWorkingInConsole,

        ["is private version"] = LabInternal,
        ["is private version of tada"] = LabInternal,

        ["ccs tolerance for generated lipid annotation"] = MobilityOnly,
        ["use ccs for generated lipid annotation scoring"] = MobilityOnly,
        ["use ccs for generated lipid annotation filtering"] = MobilityOnly,

        ["ms2 mass accuracy preview only"] = MassAccuracyPreview,

        ["execute automatic rt correction for alignment"] = Undecided,
        ["automatic rt correction reference file id"] = Undecided,
        ["automatic rt correction rt bin width"] = Undecided,
        ["automatic rt correction match rt tolerance"] = Undecided,
        ["automatic rt correction minimum anchors"] = Undecided,
        ["automatic rt correction maximum anchors"] = Undecided,
        ["automatic rt correction minimum sample coverage"] = Undecided,
        ["automatic rt correction intensity quantile"] = Undecided,
        ["automatic rt correction maximum peak width quantile"] = Undecided,
        ["automatic rt correction minimum signal to noise"] = Undecided,
        ["automatic rt correction minimum gaussian similarity"] = Undecided,
        ["automatic rt correction minimum ideal slope"] = Undecided,
        ["automatic rt correction outlier mad threshold"] = Undecided,
        ["automatic rt correction reference centrality weight"] = Undecided,
        ["automatic rt correction interpolate blanks by analytical order"] = Undecided,
        ["automatic rt correction local support rt window"] = Undecided,
        ["automatic rt correction anchor mode"] = Undecided,
        ["automatic rt correction anchor library file path"] = Undecided,
        ["automatic rt correction anchor peak selection mode"] = Undecided,
        ["automatic rt correction anchor peak selection rt weight"] = Undecided,
        ["automatic rt correction preview only"] = Undecided,
        ["export folder path"] = Undecided,
        ["height matrix export"] = Undecided,
        ["peak area matrix export"] = Undecided,
        ["peak id matrix export"] = Undecided,
        ["retention time matrix export"] = Undecided,
        ["mass matrix export"] = Undecided,
        ["sn matrix export"] = Undecided,
        ["machine category"] = Undecided,
        ["searched lipid class"] = Undecided,
        ["relative amplitude cut off"] = Undecided,
        ["ri tolerance for lbm-based annotation"] = Undecided,
        ["only report top hit for text-based annotation"] = Undecided,
        ["together with alignment"] = Undecided,
    };

    [TestMethod]
    public void EveryReaderKeyIsOfferedOrLeftOutForAReason()
    {
        var offered = OfferedKeys();
        var missing = ReaderKeys().Where(key => !offered.Contains(key) && !NotOffered.ContainsKey(key)).ToList();

        Assert.AreEqual(0, missing.Count,
            "ConfigParser reads these keys, but no template offers them and NotOffered gives no reason. "
            + "Offer each in MethodFileTemplate, or add it to NotOffered with its reason:" + Environment.NewLine
            + string.Join(Environment.NewLine, missing));
    }

    [TestMethod]
    public void EveryLeftOutKeyIsStillReadAndStillLeftOut()
    {
        var reader = ReaderKeys();
        var offered = OfferedKeys();

        var stale = NotOffered.Keys.Where(key => !reader.Contains(key) || offered.Contains(key)).ToList();

        Assert.AreEqual(0, stale.Count,
            "NotOffered lists these keys, but ConfigParser no longer reads them or a template now offers them. Remove them:"
            + Environment.NewLine + string.Join(Environment.NewLine, stale));
    }

    private static HashSet<string> OfferedKeys()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (MethodFileMode mode in Enum.GetValues(typeof(MethodFileMode))) {
            foreach (var ionMode in new[] { IonMode.Positive, IonMode.Negative }) {
                foreach (var line in MethodFileTemplate.Create(mode, ionMode).Split('\n')) {
                    var colon = line.IndexOf(':');
                    if (line.StartsWith("#", StringComparison.Ordinal) || colon <= 0) {
                        continue;
                    }
                    keys.Add(line.Substring(0, colon).Trim().ToLowerInvariant());
                }
            }
        }
        return keys;
    }

    private static HashSet<string> ReaderKeys()
    {
        var source = File.ReadAllText(ConfigParserSource());
        return new HashSet<string>(
            Regex.Matches(source, "case \"([^\"]+)\":").Cast<Match>().Select(match => match.Groups[1].Value),
            StringComparer.Ordinal);
    }

    private static string ConfigParserSource()
    {
        var relative = Path.Combine("tests", "MSDIAL5", "MsdialCoreTestApp", "Parser", "ConfigParser.cs");
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent) {
            var candidate = Path.Combine(directory.FullName, relative);
            if (File.Exists(candidate)) {
                return candidate;
            }
        }
        throw new FileNotFoundException($"{relative} was not found above {AppContext.BaseDirectory}.");
    }
}
