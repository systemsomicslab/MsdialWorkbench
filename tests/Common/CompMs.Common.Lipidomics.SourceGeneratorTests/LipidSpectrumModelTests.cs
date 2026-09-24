using CompMs.Common.Parser;
using System.Globalization;
using System.Xml.Linq;

namespace CompMs.Common.Lipidomics.SourceGenerator.Tests;

// Checks the rule metadata the generator emits next to Generate() against the XML it was generated
// from, reading the XML at test time so nothing here has to change when the XML does.
[TestClass]
public class LipidSpectrumModelTests
{
    private const string ModelXml = "MsdialLipidCidSpectrumModel.xml";

    private static readonly Dictionary<string, LipidFragmentKind> IonLists = new()
    {
        ["Precursors"] = LipidFragmentKind.Precursor,
        ["ChainIons"] = LipidFragmentKind.ChainIon,
        ["ClassIons"] = LipidFragmentKind.ClassIon,
        ["InvisibleIons"] = LipidFragmentKind.InvisibleIon,
        ["ProhibitedIons"] = LipidFragmentKind.ProhibitedIon,
    };

    [TestMethod]
    [DeploymentItem(ModelXml)]
    public void EveryXmlEntryHasAMatchingRuleSet()
    {
        var failures = new List<string>();
        var entries = XDocument.Load(ModelXml).Descendants("LipidMS").Where(e => e.Element("LipidClass") is not null).ToList();
        Assert.AreNotEqual(0, entries.Count);

        foreach (var entry in entries)
        {
            var lipidClass = entry.Element("LipidClass")!.Value;
            var adduct = entry.Element("Adduct")?.Value ?? string.Empty;
            var label = $"{lipidClass} {adduct}";

            var ruleSet = CidLipidSpectrumModels.Find(lipidClass)?.FindRuleSet(adduct);
            if (ruleSet is null)
            {
                failures.Add($"{label}: no rule set");
                continue;
            }
            if (ruleSet.Resolution != ExpectedResolution(entry.Element("LSILevel")?.Value))
            {
                failures.Add($"{label}: resolution {ruleSet.Resolution}, XML LSILevel '{entry.Element("LSILevel")?.Value}'");
            }

            // Rules follow the XML order; only invisible/prohibited ions may be missing (the generator
            // warns about those it cannot turn into code).
            var rules = ruleSet.Rules;
            var next = 0;
            foreach (var (kind, ion) in XmlIons(entry))
            {
                if (next < rules.Count && Matches(rules[next], kind, ion))
                {
                    next++;
                }
                else if (kind is LipidFragmentKind.InvisibleIon or LipidFragmentKind.ProhibitedIon)
                {
                    Console.WriteLine($"{label}: skipped {kind} {ion}");
                }
                else
                {
                    failures.Add($"{label}: no rule for {kind} {ion}");
                }
            }
            if (next != rules.Count)
            {
                failures.Add($"{label}: {rules.Count - next} rule(s) not found in the XML");
            }
        }

        if (failures.Count > 0)
        {
            Assert.Fail(string.Join(Environment.NewLine, failures));
        }
    }

    // GenerateFragments must evaluate the visible rules exactly as Generate does.
    [TestMethod]
    [DeploymentItem("LipidSpectrumGeneratorTest_GP.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_GL.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_Cer.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_ST.msp")]
    [DeploymentItem("LipidSpectrumGeneratorTest_Others.msp")]
    public void VisibleFragmentsEqualGeneratedSpectrum()
    {
        var failures = new List<string>();
        var compared = 0;
        foreach (var file in new[] { "GP", "GL", "Cer", "ST", "Others" })
        {
            foreach (var reference in MspFileParser.MspFileReader($"LipidSpectrumGeneratorTest_{file}.msp"))
            {
                if (FacadeLipidParser.Default.Parse(reference.Name) is not Lipid lipid)
                {
                    continue;
                }
                foreach (var model in LipidSpectrumModelSelector.FindCandidates(CidLipidSpectrumModels.All, lipid, reference.AdductType))
                {
                    var label = $"{model.LipidClassName} {reference.Name} {reference.AdductType.AdductIonName}";
                    // Generate itself can't handle every chain shape the MSP names parse to; what
                    // matters here is only that both methods agree, failures included.
                    var (peaks, peaksError) = Try(() => model.Generate(lipid, reference.AdductType));
                    var (fragments, fragmentsError) = Try(() => model.GenerateFragments(lipid, reference.AdductType));
                    if (peaksError is not null || fragmentsError is not null)
                    {
                        if (peaksError?.GetType() != fragmentsError?.GetType())
                        {
                            failures.Add($"{label}: Generate threw {peaksError?.GetType().Name ?? "nothing"}, GenerateFragments threw {fragmentsError?.GetType().Name ?? "nothing"}");
                        }
                        continue;
                    }
                    if (peaks is null || fragments is null)
                    {
                        failures.Add($"{label}: Generate returned {(peaks is null ? "null" : "peaks")}, GenerateFragments returned {(fragments is null ? "null" : "fragments")}");
                        continue;
                    }

                    var visible = fragments.Where(f => f.Rule.IsVisible).ToList();
                    if (visible.Count != peaks.Count)
                    {
                        failures.Add($"{label}: {visible.Count} visible fragments, {peaks.Count} peaks");
                        continue;
                    }
                    for (var i = 0; i < peaks.Count; i++)
                    {
                        if (visible[i].Mz != peaks[i].Mass || visible[i].Intensity != peaks[i].Intensity || visible[i].Rule.Comment != peaks[i].Comment)
                        {
                            failures.Add($"{label}: #{i} fragment ({visible[i].Mz}, {visible[i].Intensity}, {visible[i].Rule.Comment}) != peak ({peaks[i].Mass}, {peaks[i].Intensity}, {peaks[i].Comment})");
                        }
                    }
                    compared++;
                }
            }
        }

        Console.WriteLine($"Compared {compared} spectra.");
        Assert.AreNotEqual(0, compared);
        if (failures.Count > 0)
        {
            Assert.Fail(string.Join(Environment.NewLine, failures));
        }
    }

    private static (T? Value, Exception? Error) Try<T>(Func<T?> func) where T : class
    {
        try
        {
            return (func(), null);
        }
        catch (Exception ex)
        {
            return (null, ex);
        }
    }

    private static LipidSpectrumResolution ExpectedResolution(string? lsiLevel)
    {
        return lsiLevel switch
        {
            "SL" => LipidSpectrumResolution.SpeciesLevel,
            "MSL" => LipidSpectrumResolution.MolecularSpeciesLevel,
            _ => LipidSpectrumResolution.None,
        };
    }

    private static IEnumerable<(LipidFragmentKind Kind, XElement Ion)> XmlIons(XElement entry)
    {
        foreach (var list in entry.Elements())
        {
            if (IonLists.TryGetValue(list.Name.LocalName, out var kind))
            {
                foreach (var ion in list.Elements())
                {
                    yield return (kind, ion);
                }
            }
        }
    }

    private static bool Matches(LipidFragmentRule rule, LipidFragmentKind kind, XElement ion)
    {
        var intensityElement = kind switch
        {
            LipidFragmentKind.InvisibleIon => "InvisibleIonIntensity",
            LipidFragmentKind.ProhibitedIon => "ProhibitedIonIntensity",
            _ => "Intensity",
        };
        return rule.Kind == kind
            && rule.Intensity == ParseDouble(ion.Element(intensityElement)?.Value ?? "0")
            && rule.IsDiagnostic == (ion.Element("IsDiagnostic")?.Value.Trim() == "1")
            && rule.IsPositionDiagnostic == (ion.Element("IsPositionDiagnostic")?.Value.Trim() == "1")
            && rule.DiagnosticIonGroup == ion.Element("DiagnosticIonGroup")?.Value
            && rule.DiagnosticIonCount == ParseNullableInt(ion.Element("DiagnosticIonCount")?.Value)
            && rule.DiagnosticIonIntensity == ParseNullableDouble(ion.Element("DiagnosticIonIntensity")?.Value);
    }

    private static double ParseDouble(string value) => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static int? ParseNullableInt(string? value) => int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static double? ParseNullableDouble(string? value) => double.TryParse(value?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
}
