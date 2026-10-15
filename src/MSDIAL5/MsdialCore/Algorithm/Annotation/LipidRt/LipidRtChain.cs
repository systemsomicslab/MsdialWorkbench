using System;
using System.Collections.Generic;
using System.Globalization;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt
{
    /// <summary>One chain of a lipid name with its role, as the plausibility profile needs it.</summary>
    /// <remarks>
    /// <see cref="DoubleBond"/> is the chain's own count as written: a plasmenyl "P-18:0" chain has 0 here. The
    /// species-level double-bond equivalent that counts P- as O- plus one double bond lives on the composition.
    /// </remarks>
    public sealed class LipidRtChain : IEquatable<LipidRtChain>
    {
        public LipidRtChain(LipidRtChainRole role, int carbon, int doubleBond, int oxygen, string ether) {
            Role = role;
            Carbon = carbon;
            DoubleBond = doubleBond;
            Oxygen = oxygen;
            Ether = ether ?? string.Empty;
        }

        public LipidRtChainRole Role { get; }
        public int Carbon { get; }
        public int DoubleBond { get; }
        public int Oxygen { get; }

        /// <summary>"O", "P" or empty.</summary>
        public string Ether { get; }

        public bool IsFatty => Role == LipidRtChainRole.Acyl || Role == LipidRtChainRole.Alkyl || Role == LipidRtChainRole.NAcyl
            || Role == LipidRtChainRole.EsterAcyl || Role == LipidRtChainRole.OAcyl;

        public bool IsUnresolved => Role == LipidRtChainRole.SpeciesCore || Role == LipidRtChainRole.SphingoidCore;

        public bool Equals(LipidRtChain other) {
            return other is not null && Role == other.Role && Carbon == other.Carbon && DoubleBond == other.DoubleBond
                && Oxygen == other.Oxygen && Ether == other.Ether;
        }

        public override bool Equals(object obj) => Equals(obj as LipidRtChain);

        public override int GetHashCode() {
            unchecked {
                var hash = (int)Role;
                hash = hash * 31 + Carbon;
                hash = hash * 31 + DoubleBond;
                hash = hash * 31 + Oxygen;
                return hash * 31 + Ether.GetHashCode();
            }
        }

        /// <summary>"acyl/18/1/0/" as the reference harness writes it.</summary>
        public override string ToString() {
            return string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}/{3}/{4}", RoleToken(Role), Carbon, DoubleBond, Oxygen, Ether);
        }

        /// <summary>Reads the "role/C/DB/O/ether|..." serialization of the reference harness.</summary>
        public static IReadOnlyList<LipidRtChain> ParseList(string text) {
            var chains = new List<LipidRtChain>();
            if (string.IsNullOrEmpty(text)) {
                return chains;
            }
            foreach (var part in text.Split('|')) {
                var fields = part.Split('/');
                if (fields.Length != 5) {
                    throw new FormatException($"Not a chain: '{part}'");
                }
                chains.Add(new LipidRtChain(
                    ParseRole(fields[0]),
                    int.Parse(fields[1], CultureInfo.InvariantCulture),
                    int.Parse(fields[2], CultureInfo.InvariantCulture),
                    int.Parse(fields[3], CultureInfo.InvariantCulture),
                    fields[4]));
            }
            return chains;
        }

        public static string RoleToken(LipidRtChainRole role) {
            switch (role) {
                case LipidRtChainRole.Acyl: return "acyl";
                case LipidRtChainRole.Alkyl: return "alkyl";
                case LipidRtChainRole.SphingoidBase: return "sph_base";
                case LipidRtChainRole.NAcyl: return "n_acyl";
                case LipidRtChainRole.EsterAcyl: return "ester_acyl";
                case LipidRtChainRole.OAcyl: return "o_acyl";
                case LipidRtChainRole.SterolCore: return "sterol_core";
                case LipidRtChainRole.SpeciesCore: return "species_core";
                case LipidRtChainRole.SphingoidCore: return "sph_core";
                default: throw new ArgumentOutOfRangeException(nameof(role));
            }
        }

        public static LipidRtChainRole ParseRole(string token) {
            switch (token) {
                case "acyl": return LipidRtChainRole.Acyl;
                case "alkyl": return LipidRtChainRole.Alkyl;
                case "sph_base": return LipidRtChainRole.SphingoidBase;
                case "n_acyl": return LipidRtChainRole.NAcyl;
                case "ester_acyl": return LipidRtChainRole.EsterAcyl;
                case "o_acyl": return LipidRtChainRole.OAcyl;
                case "sterol_core": return LipidRtChainRole.SterolCore;
                case "species_core": return LipidRtChainRole.SpeciesCore;
                case "sph_core": return LipidRtChainRole.SphingoidCore;
                default: throw new FormatException($"Unknown chain role '{token}'");
            }
        }
    }
}
