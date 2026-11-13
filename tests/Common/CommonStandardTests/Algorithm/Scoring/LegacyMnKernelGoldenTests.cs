using CompMs.Common.Algorithm.Function;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CompMs.Common.Algorithm.Scoring.Tests;

/// <summary>
/// Golden regression for the molecular-networking kernels as they were before they were made
/// non-mutating: ModDot, Bonanza, Cosine and All, on 200 synthetic spectra preprocessed exactly as
/// the console <c>msn</c> command preprocesses them, plus the edges of the GUI network builder for
/// Bonanza and ModDot.
/// </summary>
/// <remarks>
/// The golden file was generated from unmodified master. Every later change to these kernels must
/// reproduce it bit for bit (<c>==</c> on the IEEE bits, not a tolerance).
///
/// To regenerate it (only ever from code whose legacy values are known to be right), set
/// MSDIAL_WRITE_LEGACY_MN_GOLDEN to the path of the JSON file to write and run this class.
///
/// Whole numbers are written as JSON integers and every other double as the 16 hex digits of its
/// IEEE bits, so the comparison does not depend on how a runtime formats or parses decimal text.
/// </remarks>
[TestClass]
[DeploymentItem(@"Resources\SimilarityGolden\legacy-mn-golden.json", @"Resources\SimilarityGolden\")]
public class LegacyMnKernelGoldenTests
{
    private const int Seed = 20261113;
    private const int SpectrumCount = 200;
    // The MN defaults of MolecularSpectrumNetworkingBaseParameter.
    private const double MassTolerance = 0.025;
    private const double RelativeAbundanceCutOff = 1;
    private const double AbsoluteAbundanceCutOff = 0;

    private const string GoldenPath = @"Resources\SimilarityGolden\legacy-mn-golden.json";
    private const string WriteVariable = "MSDIAL_WRITE_LEGACY_MN_GOLDEN";

    private static readonly double[] NeutralLosses = {
        18.01056, 17.02655, 43.98983, 46.00548, 162.05282, 176.03209, 79.95682, 141.01909, 183.06604, 28.0313, 14.01565, 15.99491,
    };

    // Offsets that put a peak exactly on, just inside and just outside the 0.025 Da tolerance and the
    // 0.05 Da above-precursor margin of the MN preprocessing.
    private static readonly double[] Jitters = {
        0d, 0d, 0d, 0.004, -0.004, 0.012, -0.012, 0.0249, -0.0249, 0.025, -0.025, 0.0251, -0.0251, 0.05, -0.05,
    };

    private sealed class TestSpot : IMoleculeProperty, IChromatogramPeak
    {
        public int ID { get; set; }
        public ChromXs ChromXs { get; set; } = new ChromXs(1d);
        public double Mass { get; set; }
        public double Intensity { get; set; }
        public string Name { get; set; } = "Unknown";
        public Formula Formula { get; set; }
        public string Ontology { get; set; } = string.Empty;
        public string SMILES { get; set; } = string.Empty;
        public string InChIKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// The raw synthetic spectra: shared fragments with jitter around the tolerance, neutral losses
    /// from the precursor, precursor peaks, peaks above the precursor, tied intensities, intensities
    /// under the 1% cutoff, equal precursors, precursor pairs one neutral loss apart, unsorted peak
    /// lists, and a few spectra that the preprocessing empties.
    /// </summary>
    internal static List<MSScanProperty> CreateRawSpectra() {
        var random = new Random(Seed);
        var fragments = Enumerable.Range(0, 60).Select(_ => Round5(50d + random.NextDouble() * 650d)).ToArray();

        // 25 parent compounds; most spectra are members of a family (the parent's fragments with
        // perturbed intensities, some fragments shifted by the precursor difference as an analog
        // would be), so that pairs with several matched peaks and high scores are common.
        var parents = new List<(double precursor, List<(double mz, double intensity)> peaks)>();
        for (int f = 0; f < 25; f++) {
            var precursor = Round5(200d + random.NextDouble() * 650d);
            var peaks = new List<(double, double)>();
            var count = 4 + random.Next(16);
            for (int p = 0; p < count; p++) {
                var mz = random.Next(3) == 0
                    ? fragments[random.Next(fragments.Length)]
                    : Round5(40d + random.NextDouble() * (precursor - 40d));
                peaks.Add((mz, Math.Round(10d + random.NextDouble() * 9990d, 1)));
            }
            peaks.Add((precursor, Math.Round(100d + random.NextDouble() * 5000d, 1)));
            parents.Add((precursor, peaks));
        }

        var scans = new List<MSScanProperty>(SpectrumCount);
        for (int k = 0; k < SpectrumCount; k++) {
            var family = random.Next(5) == 0 ? -1 : random.Next(parents.Count);
            double precursor;
            var precursorKind = random.Next(10);
            if (family >= 0) {
                var parent = parents[family];
                precursor = precursorKind < 5
                    ? parent.precursor
                    : precursorKind < 9
                        ? Round5(parent.precursor + NeutralLosses[random.Next(NeutralLosses.Length)] * (random.Next(2) == 0 ? 1 : -1))
                        : Round5(parent.precursor + Jitters[random.Next(Jitters.Length)]);
            }
            else if (k > 0 && precursorKind == 0) {
                precursor = scans[random.Next(k)].PrecursorMz;
            }
            else {
                precursor = Round5(150d + random.NextDouble() * 750d);
            }

            var scan = new MSScanProperty(k, precursor, new RetentionTime(1d), IonMode.Positive);
            var emptiedByPreprocessing = random.Next(40) == 0;
            if (emptiedByPreprocessing) {
                var count = 1 + random.Next(5);
                for (int p = 0; p < count; p++) {
                    scan.AddPeak(precursor + 1d + random.NextDouble() * 50d, Math.Round(1d + random.NextDouble() * 9999d, 1));
                }
                scans.Add(scan);
                continue;
            }

            if (family >= 0) {
                var parent = parents[family];
                var shift = precursor - parent.precursor;
                foreach (var (mz, intensity) in parent.peaks) {
                    if (random.Next(5) == 0) continue;
                    var moved = random.Next(4) == 0 ? mz + shift : mz;
                    var jitter = random.Next(3) == 0 ? Jitters[random.Next(Jitters.Length)] : 0d;
                    var scale = random.Next(6) == 0 ? 1d : 0.3 + random.NextDouble() * 1.4;
                    scan.AddPeak(moved + jitter, Math.Round(intensity * scale, 1));
                }
            }

            var extra = family >= 0 ? random.Next(8) : 1 + random.Next(30);
            for (int p = 0; p < extra; p++) {
                double mz;
                var peakKind = random.Next(10);
                var jitter = Jitters[random.Next(Jitters.Length)];
                if (peakKind < 4) {
                    mz = fragments[random.Next(fragments.Length)] + jitter;
                }
                else if (peakKind < 6) {
                    mz = precursor - NeutralLosses[random.Next(NeutralLosses.Length)] + jitter;
                }
                else if (peakKind < 7) {
                    mz = precursor + jitter;
                }
                else if (peakKind < 8) {
                    mz = precursor + random.NextDouble() * 0.1;
                }
                else {
                    mz = 30d + random.NextDouble() * (precursor - 30d);
                }
                if (mz <= 1d) {
                    mz = 1d + random.NextDouble();
                }

                double intensity;
                var intensityKind = random.Next(10);
                if (intensityKind == 0) {
                    intensity = 1000d;
                }
                else if (intensityKind < 3) {
                    intensity = Math.Round(0.5 + random.NextDouble() * 20d, 2);
                }
                else {
                    intensity = Math.Round(1d + random.NextDouble() * 9999d, 1);
                }
                scan.AddPeak(mz, intensity);
            }
            if (scan.Spectrum.Count == 0) {
                scan.AddPeak(precursor, 1000d);
            }
            // Leave about a third of the spectra unsorted; the rest are in m/z order as MSP files usually are.
            if (random.Next(3) != 0) {
                scan.Spectrum = scan.Spectrum.OrderBy(peak => peak.Mass).ToList();
            }
            scans.Add(scan);
        }
        return scans;
    }

    private static double Round5(double value) => Math.Round(value, 5);

    /// <summary>The spectra as <c>msn</c> scores them: the same GetProcessedSpectrum call, with the MN defaults.</summary>
    internal static List<MSScanProperty> CreateProcessedSpectra() {
        var scans = CreateRawSpectra();
        foreach (var scan in scans) {
            scan.Spectrum = MsScanMatching.GetProcessedSpectrum(scan.Spectrum, scan.PrecursorMz,
                absoluteAbundanceCutOff: AbsoluteAbundanceCutOff, relativeAbundanceCutOff: RelativeAbundanceCutOff);
        }
        return scans;
    }

    private static MolecularNetworkingQuery CreateGuiQuery(MsmsSimilarityCalc calc) {
        // MolecularSpectrumNetworkingBaseParameter's defaults, as CytoscapejsModel converts them.
        return new MolecularNetworkingQuery
        {
            MsmsSimilarityCalc = calc,
            MassTolerance = MassTolerance,
            AbsoluteAbundanceCutOff = AbsoluteAbundanceCutOff,
            RelativeAbundanceCutOff = RelativeAbundanceCutOff,
            SpectrumSimilarityCutOff = 50d,
            MinimumPeakMatch = 2d,
            MaxEdgeNumberPerNode = 6d,
            MaxPrecursorDifference = 400d,
            MaxPrecursorDifferenceAsPercent = 50d,
        };
    }

    private static List<double[]> GuiEdges(MsmsSimilarityCalc calc) {
        var scans = CreateRawSpectra();
        var spots = scans.Select(scan => new TestSpot { ID = scan.ScanID, Mass = scan.PrecursorMz, Intensity = 1000d + scan.ScanID, }).ToList();
        var instance = new MoleculerNetworkingBase().GetMolecularNetworkInstance(spots, scans.Cast<IMSScanProperty>().ToList(), CreateGuiQuery(calc), report: null);
        return instance.Root.edges
            .Select(edge => new[] { (double)edge.data.source, edge.data.target, edge.data.score, edge.data.matchpeakcount, })
            .ToList();
    }

    private static string Bits(double value) {
        var bits = BitConverter.DoubleToInt64Bits(value);
        return bits == 0L ? "0" : bits.ToString("x16", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// A value as stored in the golden file: a whole number (a matched-peak count, 0, 1, the -1
    /// sentinel) as a JSON integer, anything else as a JSON string of its IEEE bits.
    /// </summary>
    private static JToken Encode(double value) {
        var bits = BitConverter.DoubleToInt64Bits(value);
        if (bits == 0L) return new JValue(0L);
        if (bits != long.MinValue && Math.Abs(value) <= 1e15 && value == Math.Floor(value)) return new JValue((long)value);
        return new JValue(bits.ToString("x16", CultureInfo.InvariantCulture));
    }

    /// <summary>The comparison key of a stored value: "2" for an integer, "x3ff0..." for bits.</summary>
    private static string Key(JToken token) {
        return token.Type == JTokenType.Integer
            ? ((long)token).ToString(CultureInfo.InvariantCulture)
            : "x" + (string)token;
    }

    private static double Decode(JToken token) {
        return token.Type == JTokenType.Integer
            ? (long)token
            : BitConverter.Int64BitsToDouble(long.Parse((string)token, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    private static string Fingerprint(IEnumerable<MSScanProperty> scans) {
        var builder = new StringBuilder();
        foreach (var scan in scans) {
            builder.Append(Bits(scan.PrecursorMz)).Append(':');
            foreach (var peak in scan.Spectrum) {
                builder.Append(Bits(peak.Mass)).Append(',').Append(Bits(peak.Intensity)).Append(';');
            }
            builder.Append('\n');
        }
        using (var sha = SHA256.Create()) {
            return string.Concat(sha.ComputeHash(Encoding.ASCII.GetBytes(builder.ToString())).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }

    /// <summary>
    /// One row per pair i &lt; j: i, j, then ModDot[2], Bonanza[2], Cosine[2] and All[4]. A pair whose
    /// ten values are all +0 is not stored; the test checks those pairs against +0.
    /// </summary>
    private static List<JArray> PairRows(List<MSScanProperty> scans, Func<IMSScanProperty, IMSScanProperty, double[]>[] kernels, bool omitAllZero) {
        var rows = new List<JArray>();
        for (int i = 0; i < scans.Count; i++) {
            for (int j = i + 1; j < scans.Count; j++) {
                var values = kernels.SelectMany(kernel => kernel(scans[i], scans[j])).ToArray();
                if (omitAllZero && values.All(v => BitConverter.DoubleToInt64Bits(v) == 0L)) continue;
                var row = new JArray(i, j);
                foreach (var value in values) row.Add(Encode(value));
                rows.Add(row);
            }
        }
        return rows;
    }

    private static readonly string[] KernelNames = { "ModDot", "Bonanza", "Cosine", "All", };

    private static Func<IMSScanProperty, IMSScanProperty, double[]>[] LegacyKernels() {
        return new Func<IMSScanProperty, IMSScanProperty, double[]>[] {
            (a, b) => MsScanMatching.GetModifiedDotProductScore(a, b, MassTolerance),
            (a, b) => MsScanMatching.GetBonanzaScore(a, b, MassTolerance),
            (a, b) => MsScanMatching.GetCosineScore(a, b, MassTolerance),
            (a, b) => MsScanMatching.GetBonanzaModifiedDotCosineScores(a, b, MassTolerance),
        };
    }

    private static JObject CreateGolden() {
        var raw = CreateRawSpectra();
        var processed = CreateProcessedSpectra();
        return new JObject
        {
            ["format"] = "msdial-legacy-mn-golden",
            ["format_version"] = 1,
            ["seed"] = Seed,
            ["spectrum_count"] = SpectrumCount,
            ["mass_tolerance"] = MassTolerance,
            ["relative_abundance_cutoff"] = RelativeAbundanceCutOff,
            ["absolute_abundance_cutoff"] = AbsoluteAbundanceCutOff,
            ["raw_sha256"] = Fingerprint(raw),
            ["processed_sha256"] = Fingerprint(processed),
            ["columns"] = new JArray("i", "j",
                "ModDot.score", "ModDot.matched", "Bonanza.score", "Bonanza.matched", "Cosine.score", "Cosine.matched",
                "All.bonanza", "All.matched", "All.moddot", "All.cosine"),
            ["omitted_all_zero_pairs"] = SpectrumCount * (SpectrumCount - 1) / 2 - PairRows(processed, LegacyKernels(), omitAllZero: true).Count,
            ["pairs"] = new JArray(PairRows(processed, LegacyKernels(), omitAllZero: true)),
            ["gui_edges"] = new JObject
            {
                ["Bonanza"] = new JArray(GuiEdges(MsmsSimilarityCalc.Bonanza).Select(e => new JArray(e.Select(Encode)))),
                ["ModDot"] = new JArray(GuiEdges(MsmsSimilarityCalc.ModDot).Select(e => new JArray(e.Select(Encode)))),
            },
        };
    }

    private static JObject LoadGolden() {
        var target = Environment.GetEnvironmentVariable(WriteVariable);
        if (!string.IsNullOrEmpty(target)) {
            var golden = CreateGolden();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(target)));
            // One pair per line keeps the file diffable without inflating it.
            using (var writer = new StreamWriter(target, false, new UTF8Encoding(false)) { NewLine = "\n" }) {
                var json = new JsonTextWriter(writer) { Formatting = Formatting.None };
                writer.Write("{\n");
                var properties = golden.Properties().ToList();
                for (int p = 0; p < properties.Count; p++) {
                    var property = properties[p];
                    writer.Write(JsonConvert.ToString(property.Name));
                    writer.Write(":");
                    if (property.Value is JArray array && property.Name == "pairs") {
                        writer.Write("[\n");
                        for (int r = 0; r < array.Count; r++) {
                            writer.Write(array[r].ToString(Formatting.None));
                            writer.Write(r + 1 < array.Count ? ",\n" : "\n");
                        }
                        writer.Write("]");
                    }
                    else {
                        writer.Write(property.Value.ToString(Formatting.None));
                    }
                    writer.Write(p + 1 < properties.Count ? ",\n" : "\n");
                }
                writer.Write("}\n");
            }
            Assert.Inconclusive($"The golden file was written to {target}; unset {WriteVariable} to test against it.");
        }
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GoldenPath);
        Assert.IsTrue(File.Exists(path), $"Golden file not found: {path}");
        return JObject.Parse(File.ReadAllText(path));
    }

    [TestMethod]
    public void TheSyntheticInputIsTheOneTheGoldenFileWasMadeFrom() {
        var golden = LoadGolden();
        Assert.AreEqual((string)golden["raw_sha256"], Fingerprint(CreateRawSpectra()), "the generator changed; the golden values no longer describe its spectra");
        Assert.AreEqual((string)golden["processed_sha256"], Fingerprint(CreateProcessedSpectra()), "the MN preprocessing changed");
    }

    [TestMethod]
    public void LegacyKernelsReproduceMasterValuesBitForBitOnEveryPair() {
        var golden = LoadGolden();
        var columns = golden["columns"].Select(c => (string)c).ToArray();
        var expected = golden["pairs"].ToDictionary(row => ((int)row[0], (int)row[1]), row => row.Skip(2).Select(Key).ToArray());
        Assert.AreEqual(SpectrumCount * (SpectrumCount - 1) / 2, expected.Count + (int)golden["omitted_all_zero_pairs"]);
        Assert.IsTrue(expected.Count > 1000, "the synthetic set must produce matches for this check to mean anything");

        var zero = Enumerable.Repeat("0", columns.Length - 2).ToArray();
        var actual = PairRows(CreateProcessedSpectra(), LegacyKernels(), omitAllZero: false);
        Assert.AreEqual(SpectrumCount * (SpectrumCount - 1) / 2, actual.Count);
        var mismatches = new List<string>();
        foreach (var row in actual) {
            var pair = ((int)row[0], (int)row[1]);
            var want = expected.TryGetValue(pair, out var stored) ? stored : zero;
            var got = row.Skip(2).Select(Key).ToArray();
            for (int c = 0; c < want.Length; c++) {
                if (want[c] != got[c]) {
                    mismatches.Add($"pair {pair} {columns[c + 2]}: expected {want[c]}, actual {got[c]} ({Decode(row[c + 2]):R})");
                }
            }
        }
        Assert.AreEqual(0, mismatches.Count, string.Join(Environment.NewLine, mismatches.Take(20)));
    }

    [TestMethod]
    public void GuiBonanzaNetworkKeepsItsEdgesAndScores() {
        AssertGuiEdges(LoadGolden(), "Bonanza", MsmsSimilarityCalc.Bonanza);
    }

    [TestMethod]
    public void GuiModDotNetworkKeepsItsEdgesAndScores() {
        AssertGuiEdges(LoadGolden(), "ModDot", (MsmsSimilarityCalc)0);
    }

    private static void AssertGuiEdges(JObject golden, string name, MsmsSimilarityCalc calc) {
        var expected = golden["gui_edges"][name].Select(e => string.Join(",", e.Select(Key))).ToList();
        var actual = GuiEdges(calc).Select(e => string.Join(",", e.Select(v => Key(Encode(v))))).ToList();
        Assert.IsTrue(expected.Count >= 20, "the synthetic set must produce edges for this check to mean anything");
        CollectionAssert.AreEqual(expected, actual);
    }
}
