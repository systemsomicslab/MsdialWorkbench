using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting.Tests
{
    /// <summary>
    /// A small synthetic LC-MS lipidomics alignment for the GUI tests of the lipid RT check: the TG grid of the core tests
    /// (C 48-56 even, DB 0-3, RT = 2 + 0.15 C - 0.25 DB, [M+NH4]+, chain-level LBM matches, dot 0.8) and two unannotated
    /// spots at 0.5 and 19.5 min, so that the run spans about 20 minutes. No library content.
    /// </summary>
    internal static class LipidRtTestSpots
    {
        private const double H = 1.007825, C12 = 12.0, O16 = 15.994915, Ammonium = 18.033823;

        public static ParameterBase Lipidomics() {
            return new ParameterBase { TargetOmics = TargetOmics.Lipidomics, IonMode = IonMode.Positive, MachineCategory = MachineCategory.LCMS };
        }

        public static List<AlignmentSpotProperty> Spots() {
            var spots = new List<AlignmentSpotProperty>();
            foreach (var (id, rt, mz, name) in Grid()) {
                var spot = new AlignmentSpotProperty
                {
                    MasterAlignmentID = id,
                    AlignmentID = id,
                    TimesCenter = new ChromXs(rt),
                    MassCenter = mz,
                    HeightAverage = 1e5f,
                    Ontology = name is null ? string.Empty : "TG",
                    Name = name ?? string.Empty,
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>(),
                    AlignmentDriftSpotFeatures = new List<AlignmentSpotProperty>(),
                };
                spot.SetAdductType(AdductIon.GetAdductIon("[M+NH4]+"));
                if (name != null) {
                    spot.MatchResults.AddResult(Lbm(name));
                }
                spots.Add(spot);
            }
            return spots;
        }

        public static AlignmentSpotProperty Find(IEnumerable<AlignmentSpotProperty> spots, int c, int db) {
            return spots.Single(s => s.Name.StartsWith($"TG {c}:{db}|", StringComparison.Ordinal));
        }

        private static IEnumerable<(int Id, double Rt, double Mz, string? Name)> Grid() {
            yield return (0, 0.5, 300.1, null);
            yield return (1, 19.5, 301.1, null);
            var id = 2;
            foreach (var c in new[] { 48, 50, 52, 54, 56 }) {
                foreach (var db in new[] { 0, 1, 2, 3 }) {
                    var mass = C12 * (c + 3) + H * (2 * c - 2 * db + 2) + O16 * 6;
                    yield return (id++, 2 + 0.15 * c - 0.25 * db, mass + Ammonium, $"TG {c}:{db}|TG 16:0_16:0_{c - 32}:{db}");
                }
            }
        }

        private static MsScanMatchResult Lbm(string name) {
            return new MsScanMatchResult
            {
                Name = name,
                IsPrecursorMzMatch = true,
                IsSpectrumMatch = true,
                IsLipidClassMatch = true,
                IsLipidChainsMatch = true,
                IsReferenceMatched = true,
                Source = SourceType.MspDB,
                AnnotatorID = "lbm",
                WeightedDotProduct = 0.8f,
                TotalScore = 0.8f,
            };
        }
    }
}
