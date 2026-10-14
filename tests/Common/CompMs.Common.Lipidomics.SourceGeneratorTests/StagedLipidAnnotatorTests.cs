using CompMs.Common.Components;
using CompMs.Common.Parser;

namespace CompMs.Common.Lipidomics.SourceGenerator.Tests;

// End to end over the MSP fixtures: reduce each lipid to its species-level seed (as the seed library
// will hold it), annotate the spectrum generated for the real lipid, and check the annotator finds the
// real lipid back at the level the XML allows. Spectra and expectations both come from the XML.
[TestClass]
public class StagedLipidAnnotatorTests
{
    private const double Ms2Tolerance = 0.025;

    [TestMethod]
    [DeploymentItem(KnownMissesFile)]
    [DeploymentItem("LipidSpectrumGeneratorTest_GP.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_GL.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_Cer.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_ST.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_Others.msp")]
    public void AnnotatorFindsTheGeneratingLipidFromItsSeed()
    {
        var annotator = new StagedLipidAnnotator(
            CidLipidSpectrumModels.All,
            StagedLipidAnnotator.CreateDefaultGenerator(),
            new StagedLipidSpectrumEvaluator(Ms2Tolerance));
        var outcomes = new Dictionary<(string LipidClass, string Adduct), (int Total, int Failed, string? Example)>();

        foreach (var reference in new[] { "GP", "GL", "Cer", "ST", "Others" }.SelectMany(f => MspFileParser.MspFileReader($"LipidSpectrumGeneratorTest_{f}.msp")))
        {
            if (FacadeLipidParser.Default.Parse(reference.Name) is not Lipid lipid || lipid.AnnotationLevel < 2)
            {
                continue;
            }
            var model = LipidSpectrumModelSelector.Select(CidLipidSpectrumModels.All, lipid, reference.AdductType);
            var ruleSet = model?.FindRuleSet(reference.AdductType.AdductIonName);
            if (model is null || ruleSet is null || ruleSet.Resolution == LipidSpectrumResolution.None)
            {
                continue;
            }
            List<SpectrumPeak>? spectrum;
            try
            {
                spectrum = model.Generate(lipid, reference.AdductType);
            }
            catch (Exception)
            {
                continue; // chain shapes Generate can't handle are covered by the generator tests
            }
            if (spectrum is null)
            {
                continue;
            }

            var seed = ToSeed(lipid);
            var result = annotator.Annotate(seed, reference.AdductType, spectrum);
            var (passed, actual) = ruleSet.Resolution == LipidSpectrumResolution.MolecularSpeciesLevel
                ? (result.Level >= StagedLipidLevel.MolecularSpecies && SameChains(result.Lipid, lipid), $"{result.Lipid?.Name ?? "null"} ({result.Level}, {result.StopReason})")
                : (result.Level == StagedLipidLevel.Class && result.Lipid == seed, $"{result.Lipid?.Name ?? "null"} ({result.Level}, {result.StopReason})");

            var key = (model.LipidClassName, ruleSet.AdductIonName);
            var (total, failed, example) = outcomes.TryGetValue(key, out var o) ? o : (0, 0, null);
            outcomes[key] = (total + 1, passed ? failed : failed + 1, passed ? example : example ?? $"{reference.Name} (seed {seed.Name}) -> {actual}");
        }

        var known = File.ReadLines(KnownMissesFile)
            .Where(line => line.Length > 0 && !line.StartsWith('#') && !line.StartsWith("LipidClass\t"))
            .Select(line => line.Split('\t'))
            .ToDictionary(cols => (cols[0], cols[1]), cols => cols.Length > 2 ? cols[2] : string.Empty);
        var misses = outcomes.Where(kvp => kvp.Value.Failed > 0).OrderBy(kvp => kvp.Key).ToList();
        var unexpected = misses.Where(kvp => !known.ContainsKey(kvp.Key))
            .Select(kvp => $"{kvp.Key.LipidClass}\t{kvp.Key.Adduct}\t{kvp.Value.Failed}/{kvp.Value.Total}\t{kvp.Value.Example}")
            .ToList();
        var stale = known.Keys.Where(key => !outcomes.TryGetValue(key, out var o) || o.Failed == 0)
            .Select(key => $"{key.Item1}\t{key.Item2}\tno longer misses")
            .ToList();

        Console.WriteLine($"Annotated {outcomes.Values.Sum(o => o.Total)} spectra over {outcomes.Count} (class, adduct) rule sets; {misses.Count} rule sets with misses ({unexpected.Count} unexpected).");
        foreach (var line in unexpected)
        {
            Console.WriteLine(line);
        }
        Assert.AreNotEqual(0, outcomes.Count);
        if (unexpected.Count > 0 || stale.Count > 0)
        {
            Assert.Fail(string.Join(Environment.NewLine, unexpected.Concat(stale)));
        }
    }

    private const string KnownMissesFile = "StagedLipidAnnotatorKnownMisses.tsv";

    // The species-level name ("PC 34:1"), parsed back the way a seed library entry would be.
    private static ILipid ToSeed(Lipid lipid)
    {
        var chains = lipid.Chains;
        var total = new TotalChain(chains.CarbonCount, chains.DoubleBondCount, chains.OxidizedCount, chains.AcylChainCount, chains.AlkylChainCount, chains.SphingoChainCount);
        var name = $"{LipidClassDictionary.Default.LbmItems[lipid.LipidClass].DisplayName} {total}";
        return FacadeLipidParser.Default.Parse(name) is { } parsed && parsed.LipidClass == lipid.LipidClass
            ? parsed
            : new Lipid(lipid.LipidClass, lipid.Mass, total);
    }

    // Compares chains as a multiset of (type, carbon, double bonds, oxidized); positions don't matter.
    private static bool SameChains(ILipid? actual, ILipid expected)
    {
        if (actual is null || actual.LipidClass != expected.LipidClass)
        {
            return false;
        }
        static IEnumerable<string> Keys(ILipid lipid) => lipid.Chains.GetDeterminedChains()
            .Select(c => $"{c.GetType().Name}:{c.CarbonCount}:{c.DoubleBondCount}:{c.OxidizedCount}")
            .OrderBy(s => s);
        return Keys(actual).SequenceEqual(Keys(expected));
    }
}
