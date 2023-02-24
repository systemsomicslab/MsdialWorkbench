/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{

    public class PCLipidParser : ILipidParser {
        public string Target { get; } = "PC";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(2);
        public static readonly string Pattern = $"^PC\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 8,
            MassDiffDictionary.HydrogenMass * 18,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                if (chains.OxidizedCount > 0)
                {
                    return new Lipid(LbmClass.OxPC, Skelton + chains.Mass, chains);
                }
                return new Lipid(LbmClass.PC, Skelton + chains.Mass, chains);
            }
            return null;
        }
	}

    public class PELipidParser : ILipidParser {
        public string Target { get; } = "PE";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(2);
        public static readonly string Pattern = $"^PE\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 12,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                if (chains.OxidizedCount > 0)
                {
                    return new Lipid(LbmClass.OxPE, Skelton + chains.Mass, chains);
                }
                return new Lipid(LbmClass.PE, Skelton + chains.Mass, chains);
            }
            return null;
        }
	}

    public class PALipidParser : ILipidParser {
        public string Target { get; } = "PA";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(2);
        public static readonly string Pattern = $"^PA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 7,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.PA, Skelton + chains.Mass, chains);
            }
            return null;
        }
	}

    public class PGLipidParser : ILipidParser {
        public string Target { get; } = "PG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(2);
        public static readonly string Pattern = $"^PG\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 13,
            MassDiffDictionary.OxygenMass * 8,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                if (chains.OxidizedCount > 0)
                {
                    return new Lipid(LbmClass.OxPG, Skelton + chains.Mass, chains);
                }
                return new Lipid(LbmClass.PG, Skelton + chains.Mass, chains);
            }
            return null;
        }
	}

    public class PILipidParser : ILipidParser {
        public string Target { get; } = "PI";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(2);
        public static readonly string Pattern = $"^PI\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 9,
            MassDiffDictionary.HydrogenMass * 17,
            MassDiffDictionary.OxygenMass * 11,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                if (chains.OxidizedCount > 0)
                {
                    return new Lipid(LbmClass.OxPI, Skelton + chains.Mass, chains);
                }
                return new Lipid(LbmClass.PI, Skelton + chains.Mass, chains);
            }
            return null;
        }
	}

    public class PSLipidParser : ILipidParser {
        public string Target { get; } = "PS";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(2);
        public static readonly string Pattern = $"^PS\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 12,
            MassDiffDictionary.OxygenMass * 8,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                if (chains.OxidizedCount > 0)
                {
                    return new Lipid(LbmClass.OxPS, Skelton + chains.Mass, chains);
                }
                return new Lipid(LbmClass.PS, Skelton + chains.Mass, chains);
            }
            return null;
        }
	}

    public class LPCLipidParser : ILipidParser {
        public string Target { get; } = "LPC";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildSpeciesLevelParser(1, 2);
        public static readonly string Pattern = $"^LPC\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 8,
            MassDiffDictionary.HydrogenMass * 18,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.LPC, Skelton + chains.Mass, chains);
            }
            return null;
        }
	}

    public class LPELipidParser : ILipidParser {
        public string Target { get; } = "LPE";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildSpeciesLevelParser(1, 2);
        public static readonly string Pattern = $"^LPE\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 12,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.LPE, Skelton + chains.Mass, chains);
            }
            return null;
        }
	}

    public class LPGLipidParser : ILipidParser {
        public string Target { get; } = "LPG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildSpeciesLevelParser(1, 2);
        public static readonly string Pattern = $"^LPG\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 13,
            MassDiffDictionary.OxygenMass * 8,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.LPG, Skelton + chains.Mass, chains);
            }
            return null;
        }
	}
}