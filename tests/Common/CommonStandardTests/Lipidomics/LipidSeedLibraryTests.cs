using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class LipidSeedLibraryTests
    {
        private const double MzTolerance = 0.01;

        [TestMethod()]
        public void ReaderReadsColumnsByHeaderName() {
            var text = "RT\tName\tAdduct\tExtra\n" +
                "5.2\tPC 34:1\t\tignored\n" +
                "\tPE 36:2\t[M-H]-\t\n";

            var seeds = LipidSeedTsvReader.Read(new StringReader(text), out var errors);

            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
            Assert.AreEqual(2, seeds.Count);
            Assert.AreEqual("PC 34:1", seeds[0].Name);
            Assert.AreEqual(LbmClass.PC, seeds[0].Lipid.LipidClass);
            Assert.AreEqual(5.2, seeds[0].RetentionTime);
            Assert.IsNull(seeds[0].Adduct);
            Assert.IsNull(seeds[1].RetentionTime);
            Assert.AreEqual("[M-H]-", seeds[1].Adduct.AdductIonName);
        }

        [TestMethod()]
        public void ReaderReportsBadLinesAndKeepsTheRest() {
            var text = "Name\tRT\tAdduct\tPrecursorMz\n" +
                "PC 34:1\tabc\t\t\n" +       // line 2: RT not a number
                "not a lipid\t1.0\t\t\n" +   // line 3: unparsable name
                "PC 34:1\t1.0\t\t760.5\n" +  // line 4: m/z without adduct
                "PC 34:1\t1.0\t[M+H]+\t\n";  // line 5: fine

            var seeds = LipidSeedTsvReader.Read(new StringReader(text), out var errors);

            Assert.AreEqual(1, seeds.Count);
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, errors.Select(e => e.LineNumber).ToArray());
        }

        [TestMethod()]
        public void ReaderClassColumnOverridesTheClassReadFromTheName() {
            var text = "Name\tClass\n" +
                "Cer 34:1;O2\tCer_NDS\n" +
                "Cer 34:1;O2\t\n" +
                "Cer 34:1;O2\tNoSuchClass\n";

            var seeds = LipidSeedTsvReader.Read(new StringReader(text), out var errors);

            Assert.AreEqual(2, seeds.Count);
            Assert.AreEqual(LbmClass.Cer_NDS, seeds[0].Lipid.LipidClass);
            Assert.AreEqual(seeds[1].Lipid.Mass, seeds[0].Lipid.Mass);
            Assert.AreEqual(4, errors.Single().LineNumber);
        }

        [TestMethod()]
        public void ReaderRequiresTheNameColumn() {
            var seeds = LipidSeedTsvReader.Read(new StringReader("RT\n1.0\n"), out var errors);

            Assert.AreEqual(0, seeds.Count);
            Assert.AreEqual(1, errors.Single().LineNumber);
        }

        [TestMethod()]
        public void SeedWithoutAdductMatchesEveryAdductOfItsClass() {
            var pc = Seed("PC 34:1", rt: 5.0);
            var library = new LipidSeedLibrary(new[] { pc }, new[] { new FakeModel("PC", "[M+H]+", "[M+Na]+", "[M+HCOO]-") });

            var mH = AdductIon.GetAdductIon("[M+H]+").ConvertToMz(pc.Lipid.Mass);
            var mNa = AdductIon.GetAdductIon("[M+Na]+").ConvertToMz(pc.Lipid.Mass);

            Assert.AreEqual("[M+H]+", library.Search(mH, MzTolerance, 5.0, 0.5, IonMode.Positive).Single().Adduct.AdductIonName);
            Assert.AreEqual("[M+Na]+", library.Search(mNa, MzTolerance, 5.0, 0.5, IonMode.Positive).Single().Adduct.AdductIonName);
            Assert.AreEqual(3, library.Entries.Count);
        }

        [TestMethod()]
        public void SearchFiltersByIonModeAndRetentionTime() {
            var pc = Seed("PC 34:1", rt: 5.0);
            var library = new LipidSeedLibrary(new[] { pc }, new[] { new FakeModel("PC", "[M+H]+") });
            var mz = AdductIon.GetAdductIon("[M+H]+").ConvertToMz(pc.Lipid.Mass);

            Assert.AreEqual(0, library.Search(mz, MzTolerance, 5.0, 0.5, IonMode.Negative).Count);
            Assert.AreEqual(0, library.Search(mz, MzTolerance, 6.0, 0.5, IonMode.Positive).Count);
            Assert.AreEqual(1, library.Search(mz, MzTolerance, null, 0.5, IonMode.Positive).Count);
            Assert.AreEqual(0, library.Search(mz + 0.02, MzTolerance, 5.0, 0.5, IonMode.Positive).Count);
        }

        [TestMethod()]
        public void SeedWithoutRetentionTimeMatchesAnyTime() {
            var pc = Seed("PC 34:1", rt: null);
            var library = new LipidSeedLibrary(new[] { pc }, new[] { new FakeModel("PC", "[M+H]+") });
            var mz = AdductIon.GetAdductIon("[M+H]+").ConvertToMz(pc.Lipid.Mass);

            Assert.AreEqual(1, library.Search(mz, MzTolerance, 12.3, 0.1, IonMode.Positive).Count);
        }

        [TestMethod()]
        public void GivenAdductAndPrecursorMzAreUsedAsIs() {
            var lipid = FacadeLipidParser.Default.Parse("PC 34:1");
            var seed = new LipidSeed("PC 34:1", lipid, 5.0, AdductIon.GetAdductIon("[M+H]+"), precursorMz: 760.6);
            var library = new LipidSeedLibrary(new[] { seed }, new[] { new FakeModel("PC", "[M+H]+", "[M+Na]+") });

            Assert.AreEqual(760.6, library.Entries.Single().PrecursorMz);
            Assert.AreEqual(1, library.Search(760.6, MzTolerance, 5.0, 0.5, IonMode.Positive).Count);
        }

        [TestMethod()]
        public void BuiltInPresetsKeepTheirSolventAdducts() {
            var model = new FakeModel("PC", "[M+H]+", "[M+Na]+", "[M-H]-", "[M+HCOO]-", "[M+CH3COO]-", "[M+HCO3]-");
            string[] Adducts(string preset) => new LipidSeedLibrary(new[] { Seed("PC 34:1", rt: 5.0) }, new[] { model }, LipidAdductPreset.BuiltIn.Single(p => p.Name == preset).Apply(new[] { model }))
                .Entries.Select(e => e.Adduct.AdductIonName).OrderBy(a => a).ToArray();

            CollectionAssert.AreEqual(new[] { "[M+H]+", "[M+Na]+" }, Adducts(LipidAdductPreset.PositiveName));
            CollectionAssert.AreEqual(new[] { "[M+HCOO]-", "[M-H]-" }, Adducts(LipidAdductPreset.NegativeHcoohName));
            CollectionAssert.AreEqual(new[] { "[M+CH3COO]-", "[M-H]-" }, Adducts(LipidAdductPreset.NegativeCh3coohName));
            CollectionAssert.AreEqual(new[] { "[M+HCO3]-", "[M-H]-" }, Adducts(LipidAdductPreset.NegativeNh4hco3Name));
            Assert.AreEqual(6, new LipidSeedLibrary(new[] { Seed("PC 34:1", rt: 5.0) }, new[] { model }).Entries.Count, "no selection keeps every adduct");
        }

        [TestMethod()]
        public void AdductsWrittenInTheSeedsAreFilteredByThePresetToo() {
            // A library with one row per solvent adduct of the same lipid.
            var lipid = FacadeLipidParser.Default.Parse("PC 34:1");
            var seeds = new[] { "[M+HCOO]-", "[M+CH3COO]-", "[M+HCO3]-", "[M-H]-" }
                .Select(a => new LipidSeed("PC 34:1", lipid, 5.0, AdductIon.GetAdductIon(a)))
                .ToArray();
            var model = new FakeModel("PC", "[M+HCOO]-", "[M+CH3COO]-", "[M+HCO3]-", "[M-H]-");
            var selection = LipidAdductPreset.BuiltIn.Single(p => p.Name == LipidAdductPreset.NegativeHcoohName).Apply(new[] { model });

            var library = new LipidSeedLibrary(seeds, new[] { model }, selection);

            CollectionAssert.AreEquivalent(new[] { "[M+HCOO]-", "[M-H]-" }, library.Entries.Select(e => e.Adduct.AdductIonName).ToArray());
        }

        [TestMethod()]
        public void UserPresetCanSetAdductsPerClass() {
            var text = "# Name: Lab negative\nClass\tAdduct\n*\t[M-H]-\nPC\t[M+HCOO]-\n";
            var preset = LipidAdductPreset.Parse(new StringReader(text), "file name", out var errors);
            var models = new[] { new FakeModel("PC", "[M-H]-", "[M+HCOO]-"), new FakeModel("PE", "[M-H]-", "[M+HCOO]-") };

            var library = new LipidSeedLibrary(new[] { Seed("PC 34:1", rt: 5.0), Seed("PE 36:2", rt: 6.0) }, models, preset.Apply(models));

            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual("Lab negative", preset.Name);
            Assert.AreEqual(IonMode.Negative, preset.IonMode);
            CollectionAssert.AreEquivalent(
                new[] { "PC [M-H]-", "PC [M+HCOO]-", "PE [M-H]-" },
                library.Entries.Select(e => $"{e.Seed.Lipid.LipidClass} {e.Adduct.AdductIonName}").ToArray());
        }

        [TestMethod()]
        public void PresetFileReportsBadLines() {
            var text = "Class\tAdduct\nNoSuchClass\t[M-H]-\n*\tnot an adduct\njust one cell\n*\t[M-H]-\n";

            var preset = LipidAdductPreset.Parse(new StringReader(text), "fallback", out var errors);

            Assert.AreEqual("fallback", preset.Name, "the file name stands in when there is no '# Name:' line");
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, errors.Select(e => int.Parse(e.Split(' ', ':')[1])).ToArray());
            Assert.IsTrue(preset.Allows(LbmClass.PC, AdductIon.GetAdductIon("[M-H]-")));
        }

        [TestMethod()]
        public void BuiltInPresetsHaveTheirPolarity() {
            CollectionAssert.AreEqual(
                new[] { "Positive:Positive", "Negative HCOOH:Negative", "Negative CH3COOH:Negative", "Negative NH4HCO3:Negative" },
                LipidAdductPreset.BuiltIn.Select(p => $"{p.Name}:{p.IonMode}").ToArray());
            Assert.AreEqual(LipidAdductPreset.PositiveName, LipidAdductPreset.DefaultFor(IonMode.Positive).Name);
            Assert.AreEqual(LipidAdductPreset.NegativeHcoohName, LipidAdductPreset.DefaultFor(IonMode.Negative).Name);
        }

        [TestMethod()]
        public void ClassWithoutRuleSetHasNoEntries() {
            var library = new LipidSeedLibrary(new[] { Seed("PE 36:2", rt: 5.0) }, new[] { new FakeModel("PC", "[M+H]+") });

            Assert.AreEqual(0, library.Entries.Count);
        }

        [TestMethod()]
        public void SubtypeRuleSetsCountForTheirLbmClass() {
            // "EtherPE_O" / "EtherPE_P" rule sets give LbmClass.EtherPE its adducts.
            var library = new LipidSeedLibrary(new[] { Seed("PE O-36:2", rt: 5.0) }, new[] { new FakeModel("EtherPE_O", "[M+H]+"), new FakeModel("EtherPE_P", "[M+H]+", "[M-H]-") });

            CollectionAssert.AreEquivalent(new[] { "[M+H]+", "[M-H]-" }, library.Entries.Select(e => e.Adduct.AdductIonName).ToArray());
        }

        private static LipidSeed Seed(string name, double? rt) {
            return new LipidSeed(name, FacadeLipidParser.Default.Parse(name), rt);
        }

        private sealed class FakeModel : ILipidSpectrumModel
        {
            private readonly LipidSpectrumRuleSet[] _ruleSets;

            public FakeModel(string lipidClassName, params string[] adducts) {
                LipidClassName = lipidClassName;
                _ruleSets = adducts.Select(a => new LipidSpectrumRuleSet(lipidClassName, a, LipidSpectrumResolution.MolecularSpeciesLevel, new LipidFragmentRule[0])).ToArray();
            }

            public string LipidClassName { get; }
            public IReadOnlyList<LipidSpectrumRuleSet> RuleSets => _ruleSets;
            public LipidSpectrumRuleSet FindRuleSet(string adductIonName) => _ruleSets.FirstOrDefault(r => r.AdductIonName == adductIonName);
            public List<SpectrumPeak> Generate(Lipid lipid, AdductIon adduct) => new List<SpectrumPeak>();
            public IReadOnlyList<LipidFragment> GenerateFragments(Lipid lipid, AdductIon adduct) => new LipidFragment[0];
        }
    }
}
