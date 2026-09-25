using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Parser;

namespace CompMs.Common.Lipidomics.SourceGenerator.Tests;

// Feeds spectra built from the generated rules to the existing CID characterization
// (MsScanMatching.GetRefinedLipidAnnotationLevel -> LipidMsmsCharacterization.JudgeIfXxx), to find
// where the XML markup and the hand-written rules disagree. Every spectrum comes from the rules, so
// nothing here changes when the XML does; only LipidSpectrumJudgeIfKnownDifferences.tsv has to be kept up to date.
[TestClass]
public class LipidSpectrumJudgeIfCompatibilityTests
{
    // The tolerance MsRefSearchParameterBase uses by default.
    private const double Ms2Tolerance = 0.025;

    // JudgeIf compares against a spectrum normalized to a maximum of 100; the XML is on a 0-999 scale.
    private const double XmlToNormalizedScale = 10d;

    // An injected prohibited ion is placed this far above its (normalized) threshold.
    private const double ProhibitedIonMargin = 1.5;

    private const string ClassCheck = "class";
    private const string ChainCheck = "chain";
    private const string LevelCheck = "level";
    private const string ProhibitedCheck = "prohibited";

    // (XML class, adduct, check) -> why the difference is accepted. A listed difference that no longer
    // occurs fails the test too, so the list can't go stale.
    private const string KnownDifferencesFile = "LipidSpectrumJudgeIfKnownDifferences.tsv";

    private static Dictionary<(string LipidClass, string Adduct, string Check), string> ReadKnownDifferences()
    {
        var result = new Dictionary<(string, string, string), string>();
        foreach (var line in File.ReadLines(KnownDifferencesFile))
        {
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("LipidClass\t"))
            {
                continue;
            }
            var cols = line.Split('\t');
            Assert.IsTrue(cols.Length >= 4, $"{KnownDifferencesFile}: malformed line '{line}'");
            result.Add((cols[0], cols[1], cols[2]), cols[3]);
        }
        return result;
    }

    [TestMethod]
    [DeploymentItem(KnownDifferencesFile)]
    [DeploymentItem("LipidSpectrumGeneratorTest_GP.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_GL.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_Cer.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_ST.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_Others.msp")]
    public void GeneratedSpectraAreAnnotatedByJudgeIf()
    {
        var outcomes = new Dictionary<(string LipidClass, string Adduct, string Check), Outcome>();
        void Record(string lipidClass, string adduct, string check, bool passed, string example)
        {
            var key = (lipidClass, adduct, check);
            if (!outcomes.TryGetValue(key, out var outcome))
            {
                outcomes[key] = outcome = new Outcome();
            }
            outcome.Total++;
            if (!passed)
            {
                outcome.Failed++;
                outcome.Example ??= example;
            }
        }

        foreach (var reference in ReadReferences())
        {
            if (FacadeLipidParser.Default.Parse(reference.Name) is not Lipid lipid)
            {
                continue;
            }
            var model = LipidSpectrumModelSelector.Select(CidLipidSpectrumModels.All, lipid, reference.AdductType);
            var ruleSet = model?.FindRuleSet(reference.AdductType.AdductIonName);
            if (model is null || ruleSet is null || ruleSet.Resolution == LipidSpectrumResolution.None)
            {
                continue;
            }
            // JudgeIf is chosen by COMPOUNDCLASS, the model by the class the name parses to. A few
            // fixtures disagree (e.g. Cer_EOS entries whose name reads as Cer_HS); comparing those would
            // judge one class's spectrum by another class's rules.
            if (!LipidSpectrumModelSelector.TryResolveLbmClass(reference.CompoundClass, out var compoundClass) || compoundClass != lipid.LipidClass)
            {
                continue;
            }
            IReadOnlyList<LipidFragment>? fragments;
            try
            {
                fragments = model.GenerateFragments(lipid, reference.AdductType);
            }
            catch (Exception)
            {
                continue; // chain shapes Generate can't handle are covered by the generator tests
            }
            if (fragments is null)
            {
                continue;
            }

            var spectrum = Normalize(fragments.Where(f => f.Rule.IsVisible).Select(f => new SpectrumPeak(f.Mz, f.Intensity, f.Rule.Comment)));
            var result = Annotate(spectrum, reference);
            if (result.IsOthers)
            {
                continue; // JudgeIf doesn't characterize this class; nothing to compare
            }

            var lipidClass = model.LipidClassName;
            var adduct = ruleSet.AdductIonName;
            Record(lipidClass, adduct, ClassCheck, result.IsClassMatched, $"{reference.Name} -> '{result.Name}'");
            if (ruleSet.Resolution == LipidSpectrumResolution.MolecularSpeciesLevel && lipid.AnnotationLevel >= 2)
            {
                Record(lipidClass, adduct, ChainCheck, result.IsChainMatched, $"{reference.Name} -> '{result.Name}'");
            }
            if (ruleSet.Resolution == LipidSpectrumResolution.SpeciesLevel && lipid.AnnotationLevel >= 2)
            {
                // The XML says this adduct can't resolve chains; JudgeIf going further is a disagreement.
                // (A species-level name is no evidence either way: JudgeIf then "matches" the 0:0 chain.)
                Record(lipidClass, adduct, LevelCheck, !result.IsChainMatched, $"{reference.Name} -> '{result.Name}' (XML: SL)");
            }

            foreach (var prohibited in fragments.Where(f => f.Rule.Kind == LipidFragmentKind.ProhibitedIon))
            {
                var intensity = Math.Min(100d, prohibited.Intensity / XmlToNormalizedScale * ProhibitedIonMargin);
                var rejected = Annotate(Normalize(spectrum.Append(new SpectrumPeak(prohibited.Mz, intensity, prohibited.Rule.Comment))), reference);
                Record(lipidClass, adduct, $"{ProhibitedCheck} {prohibited.Rule.Comment}", !rejected.IsClassMatched, $"{reference.Name} + {prohibited.Mz:F4} @ {intensity:F1} -> '{rejected.Name}'");
            }
        }

        Assert.AreNotEqual(0, outcomes.Count);
        var knownDifferences = ReadKnownDifferences();
        var unexpected = new List<string>();
        var stale = new List<string>();
        foreach (var kvp in outcomes.OrderBy(kvp => kvp.Key.LipidClass).ThenBy(kvp => kvp.Key.Adduct).ThenBy(kvp => kvp.Key.Check))
        {
            var (key, outcome) = (kvp.Key, kvp.Value);
            var known = knownDifferences.ContainsKey(key);
            var line = $"{key.LipidClass}\t{key.Adduct}\t{key.Check}\t{outcome.Failed}/{outcome.Total}\t{outcome.Example}";
            if (outcome.Failed > 0 && !known)
            {
                unexpected.Add(line);
            }
            else if (outcome.Failed == 0 && known)
            {
                stale.Add($"{key.LipidClass}\t{key.Adduct}\t{key.Check}\tno longer differs");
            }
        }
        stale.AddRange(knownDifferences.Keys
            .Where(key => !outcomes.ContainsKey(key))
            .Select(key => $"{key.LipidClass}\t{key.Adduct}\t{key.Check}\tno longer checked"));
        Console.WriteLine($"Checked {outcomes.Count} (class, adduct, check) combinations over {outcomes.Values.Sum(o => o.Total)} spectra.");
        foreach (var group in outcomes.GroupBy(kvp => kvp.Key.Check.Split(' ')[0]))
        {
            Console.WriteLine($"  {group.Key}: {group.Count(kvp => kvp.Value.Failed > 0)} of {group.Count()} combinations differ");
        }
        Console.WriteLine($"Unexpected differences: {unexpected.Count}");
        foreach (var line in unexpected)
        {
            Console.WriteLine(line);
        }

        if (unexpected.Count > 0 || stale.Count > 0)
        {
            Assert.Fail(string.Join(Environment.NewLine, unexpected.Concat(stale)));
        }
    }

    private static IEnumerable<MoleculeMsReference> ReadReferences()
    {
        return new[] { "GP", "GL", "Cer", "ST", "Others" }
            .SelectMany(file => MspFileParser.MspFileReader($"LipidSpectrumGeneratorTest_{file}.msp"));
    }

    // The shape DataAccess.GetNormalizedMs2Spectra hands to the characterization: max 100, sorted by m/z.
    private static List<SpectrumPeak> Normalize(IEnumerable<SpectrumPeak> peaks)
    {
        var list = peaks.ToList();
        var max = list.Count == 0 ? 0d : list.Max(p => p.Intensity);
        return list
            .Select(p => new SpectrumPeak(p.Mass, max > 0 ? p.Intensity / max * 100d : 0d, p.Comment))
            .OrderBy(p => p.Mass)
            .ToList();
    }

    private static AnnotationResult Annotate(List<SpectrumPeak> spectrum, MoleculeMsReference reference)
    {
        var query = new MSScanProperty { PrecursorMz = reference.PrecursorMz, IonMode = reference.IonMode, Spectrum = spectrum };
        var name = MsScanMatching.GetRefinedLipidAnnotationLevel(query, reference, Ms2Tolerance,
            out var isClassMatched, out var isChainMatched, out _, out var isOthers);
        return new AnnotationResult(name, isClassMatched, isChainMatched, isOthers);
    }

    private sealed record AnnotationResult(string Name, bool IsClassMatched, bool IsChainMatched, bool IsOthers);

    private sealed class Outcome
    {
        public int Total { get; set; }
        public int Failed { get; set; }
        public string? Example { get; set; }
    }
}
