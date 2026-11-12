using CompMs.Common.Parser;

namespace CompMs.Common.Lipidomics.SourceGenerator.Tests;

// Seeds built from the MSP fixtures (species-level names, no adduct) must be found again by each
// record's own precursor m/z, as the adduct of the record: checks the adduct expansion over the
// generated models and the precursor m/z calculation.
[TestClass]
public class LipidSeedLibraryTests
{
    private const double MzTolerance = 0.01;

    [TestMethod]
    [DeploymentItem("LipidSpectrumGeneratorTest_GP.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_GL.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_Cer.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_ST.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_Others.msp")]
    public void SeedsAreFoundByTheirRecordsPrecursorMz()
    {
        var records = new[] { "GP", "GL", "Cer", "ST", "Others" }
            .SelectMany(f => MspFileParser.MspFileReader($"LipidSpectrumGeneratorTest_{f}.msp"))
            .Select(r => (Reference: r, Lipid: FacadeLipidParser.Default.Parse(r.Name)))
            .Where(p => p.Lipid is not null && p.Reference.AdductType.FormatCheck)
            // A few fixtures' names parse to another class than their COMPOUNDCLASS (e.g. Cer_AP entries
            // reading as Cer_AH); their adducts belong to the COMPOUNDCLASS rule sets.
            .Where(p => LipidSpectrumModelSelector.TryResolveLbmClass(p.Reference.CompoundClass, out var compoundClass) && compoundClass == p.Lipid.LipidClass)
            .ToList();
        var library = new LipidSeedLibrary(records.Select(p => new LipidSeed(p.Reference.Name, p.Lipid)), CidLipidSpectrumModels.All);

        var checkedCount = 0;
        var failures = new List<string>();
        foreach (var (reference, lipid) in records)
        {
            if (library.Entries.All(e => e.Seed.Lipid != lipid))
            {
                continue; // no generated rule set for this class
            }
            var hits = library.Search(reference.PrecursorMz, MzTolerance, null, 0, reference.AdductType.IonMode);
            if (!hits.Any(h => h.Seed.Lipid == lipid && h.Adduct.AdductIonName == reference.AdductType.AdductIonName))
            {
                failures.Add($"{reference.Name} {reference.AdductType.AdductIonName} {reference.PrecursorMz}: found {string.Join(", ", hits.Where(h => h.Seed.Lipid == lipid).Select(h => $"{h.Adduct.AdductIonName}@{h.PrecursorMz:F4}"))}");
            }
            checkedCount++;
        }

        Console.WriteLine($"Checked {checkedCount} records; {failures.Count} not found.");
        Assert.AreNotEqual(0, checkedCount);
        if (failures.Count > 0)
        {
            Assert.Fail(string.Join(Environment.NewLine, failures));
        }
    }
}
