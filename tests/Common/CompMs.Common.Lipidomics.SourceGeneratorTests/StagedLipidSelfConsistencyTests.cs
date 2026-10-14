using CompMs.Common.Components;
using CompMs.Common.Parser;

namespace CompMs.Common.Lipidomics.SourceGenerator.Tests;

// The staged evaluator judged against the very XML it reads: a spectrum generated from a rule set has
// to reach that rule set's own LSILevel, and adding one of its prohibited ions has to reject it.
// Spectra and expectations both come from the XML; where they disagree the XML is inconsistent with
// itself, which is reported (not fixed) via StagedLipidSelfConsistencyKnownDifferences.tsv.
[TestClass]
public class StagedLipidSelfConsistencyTests
{
    private const double Ms2Tolerance = 0.025;
    private const double XmlToNormalizedScale = 10d;
    private const double ProhibitedIonMargin = 1.5;
    private const string KnownDifferencesFile = "StagedLipidSelfConsistencyKnownDifferences.tsv";

    [TestMethod]
    [DeploymentItem(KnownDifferencesFile)]
    [DeploymentItem("LipidSpectrumGeneratorTest_GP.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_GL.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_Cer.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_ST.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_Others.msp")]
    public void GeneratedSpectraReachTheirOwnLevel()
    {
        var evaluator = new StagedLipidSpectrumEvaluator(Ms2Tolerance);
        var outcomes = new Dictionary<(string LipidClass, string Adduct, string Check), (int Total, int Failed, string? Example)>();
        void Record(string lipidClass, string adduct, string check, bool passed, string example)
        {
            var key = (lipidClass, adduct, check);
            var (total, failed, first) = outcomes.TryGetValue(key, out var o) ? o : (0, 0, null);
            outcomes[key] = (total + 1, passed ? failed : failed + 1, passed ? first : first ?? example);
        }

        foreach (var reference in new[] { "GP", "GL", "Cer", "ST", "Others" }.SelectMany(f => MspFileParser.MspFileReader($"LipidSpectrumGeneratorTest_{f}.msp")))
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

            var hasChains = lipid.Chains.GetDeterminedChains().Length > 0 && lipid.AnnotationLevel >= 2;
            var spectrum = fragments.Where(f => f.Rule.IsVisible).Select(f => new SpectrumPeak(f.Mz, f.Intensity, f.Rule.Comment)).ToList();
            var result = evaluator.Evaluate(spectrum, ruleSet.Resolution, fragments, hasChains);
            var expected = ExpectedLevel(ruleSet.Resolution, hasChains, fragments);
            Record(model.LipidClassName, ruleSet.AdductIonName, "level", result.Level == expected, $"{reference.Name}: {result.Level} ({result.StopReason}), expected {expected}");

            foreach (var prohibited in fragments.Where(f => f.Rule.Kind == LipidFragmentKind.ProhibitedIon))
            {
                // Place the ion relative to the spectrum's maximum so it lands at 1.5x its normalized threshold.
                var max = spectrum.Max(p => p.Intensity);
                var intensity = Math.Min(100d, prohibited.Intensity / XmlToNormalizedScale * ProhibitedIonMargin) / 100d * max;
                var rejected = evaluator.Evaluate(spectrum.Append(new SpectrumPeak(prohibited.Mz, intensity)).ToList(), ruleSet.Resolution, fragments, hasChains);
                Record(model.LipidClassName, ruleSet.AdductIonName, $"prohibited {prohibited.Rule.Comment}", rejected.StopReason == StagedLipidStopReason.ProhibitedIonObserved, $"{reference.Name}: {rejected.Level} ({rejected.StopReason})");
            }
        }

        Assert.AreNotEqual(0, outcomes.Count);
        var known = File.ReadLines(KnownDifferencesFile)
            .Where(line => line.Length > 0 && !line.StartsWith('#') && !line.StartsWith("LipidClass\t"))
            .Select(line => line.Split('\t'))
            .ToDictionary(cols => (cols[0], cols[1], cols[2]), cols => cols.Length > 3 ? cols[3] : string.Empty);
        var unexpected = outcomes.Where(kvp => kvp.Value.Failed > 0 && !known.ContainsKey(kvp.Key))
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => $"{kvp.Key.LipidClass}\t{kvp.Key.Adduct}\t{kvp.Key.Check}\t{kvp.Value.Failed}/{kvp.Value.Total}\t{kvp.Value.Example}")
            .ToList();
        var stale = known.Keys.Where(key => !outcomes.TryGetValue(key, out var o) || o.Failed == 0)
            .Select(key => $"{key.Item1}\t{key.Item2}\t{key.Item3}\tno longer differs")
            .ToList();

        Console.WriteLine($"Checked {outcomes.Count} (class, adduct, check) combinations over {outcomes.Values.Sum(o => o.Total)} spectra.");
        foreach (var line in unexpected)
        {
            Console.WriteLine(line);
        }
        if (unexpected.Count > 0 || stale.Count > 0)
        {
            Assert.Fail(string.Join(Environment.NewLine, unexpected.Concat(stale)));
        }
    }

    private static StagedLipidLevel ExpectedLevel(LipidSpectrumResolution resolution, bool hasChains, IReadOnlyList<LipidFragment> fragments)
    {
        if (resolution != LipidSpectrumResolution.MolecularSpeciesLevel || !hasChains)
        {
            return StagedLipidLevel.Class;
        }
        return fragments.Any(f => f.Rule.IsPositionDiagnostic) ? StagedLipidLevel.Position : StagedLipidLevel.MolecularSpecies;
    }
}
