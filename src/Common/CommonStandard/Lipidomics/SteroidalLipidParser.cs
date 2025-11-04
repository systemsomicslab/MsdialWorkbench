using Accord.Math;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class DCAELipidParser : ILipidParser
    {
        public string Target { get; } = "SE";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^SE 24:1;O4\\/\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 39,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.DCAE, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class GDCAELipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^BA 24:1;O4;G\\/\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 42,
            MassDiffDictionary.OxygenMass * 5,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.GDCAE, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class GLCAELipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^BA 24:1;O3;G\\/\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 42,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.GLCAE, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class TDCAELipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^BA 24:1;O4;T\\/\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 44,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.TDCAE, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class TLCAELipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^BA 24:1;O3;T\\/\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 44,
            MassDiffDictionary.OxygenMass * 5,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.TLCAE, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class KLCAELipidParser : ILipidParser
    {
        public string Target { get; } = "SE";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^SE 24:2;O4\\/\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 37,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.KLCAE, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class KDCAELipidParser : ILipidParser
    {
        public string Target { get; } = "SE";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^SE 24:2;O5\\/\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 37,
            MassDiffDictionary.OxygenMass * 5,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.KDCAE, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class LCAELipidParser : ILipidParser
    {
        public string Target { get; } = "SE";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^SE 24:1;O3\\/\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 39,
            MassDiffDictionary.OxygenMass * 3,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.LCAE, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class BRSELipidParser : ILipidParser
    {
        public string Target { get; } = "SE";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^SE 28:2\\/\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 28,
            MassDiffDictionary.HydrogenMass * 45,
            MassDiffDictionary.OxygenMass * 1,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.BRSE, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class CASELipidParser : ILipidParser
    {
        public string Target { get; } = "SE";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^SE 28:1\\/\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 28,
            MassDiffDictionary.HydrogenMass * 47,
            MassDiffDictionary.OxygenMass * 1,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.CASE, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class SISELipidParser : ILipidParser
    {
        public string Target { get; } = "SE";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^SE 29:1\\/\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 29,
            MassDiffDictionary.HydrogenMass * 49,
            MassDiffDictionary.OxygenMass * 1,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.SISE, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class STSELipidParser : ILipidParser
    {
        public string Target { get; } = "SE";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^SE 29:2\\/\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 29,
            MassDiffDictionary.HydrogenMass * 47,
            MassDiffDictionary.OxygenMass * 1,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.STSE, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class CholestanLipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 27:0;O$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 27,
            MassDiffDictionary.HydrogenMass * 48,
            MassDiffDictionary.OxygenMass * 1,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class EGSELipidParser : ILipidParser
    {
        public string Target { get; } = "SE";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^SE 28:3\\/\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 28,
            MassDiffDictionary.HydrogenMass * 43,
            MassDiffDictionary.OxygenMass * 1,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.EGSE, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class DEGSELipidParser : ILipidParser
    {
        public string Target { get; } = "SE";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^SE 28:4\\/\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 28,
            MassDiffDictionary.HydrogenMass * 41,
            MassDiffDictionary.OxygenMass * 1,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.DEGSE, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class DSMSELipidParser : ILipidParser
    {
        public string Target { get; } = "SE";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^SE 27:2\\/\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 27,
            MassDiffDictionary.HydrogenMass * 43,
            MassDiffDictionary.OxygenMass * 1,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.DSMSE, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class AHexCSLipidParser : ILipidParser
    {
        public string Target { get; } = "ASG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^ASG 27:1;O;Hex;FA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 33,
            MassDiffDictionary.HydrogenMass * 55,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.AHexCS, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class AHexBRSLipidParser : ILipidParser
    {
        public string Target { get; } = "ASG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^ASG 28:2;O;Hex;FA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 34,
            MassDiffDictionary.HydrogenMass * 55,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.AHexBRS, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class AHexCASLipidParser : ILipidParser
    {
        public string Target { get; } = "ASG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^ASG 28:1;O;Hex;FA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 34,
            MassDiffDictionary.HydrogenMass * 57,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.AHexCAS, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class AHexSISLipidParser : ILipidParser
    {
        public string Target { get; } = "ASG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^ASG 29:1;O;Hex;FA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 35,
            MassDiffDictionary.HydrogenMass * 59,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.AHexSIS, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class AHexSTSLipidParser : ILipidParser
    {
        public string Target { get; } = "ASG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^ASG 29:2;O;Hex;FA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 35,
            MassDiffDictionary.HydrogenMass * 57,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.AHexSTS, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class CSLPHexLipidParser : ILipidParser
    {
        public string Target { get; } = "SG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^SG 27:1;O;Hex;LPA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 36,
            MassDiffDictionary.HydrogenMass * 62,
            MassDiffDictionary.OxygenMass * 11,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 1,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.CSLPHex, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class BRSLPHexLipidParser : ILipidParser
    {
        public string Target { get; } = "SG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^SG 28:2;O;Hex;LPA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 37,
            MassDiffDictionary.HydrogenMass * 62,
            MassDiffDictionary.OxygenMass * 11,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 1,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.BRSLPHex, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class CASLPHexLipidParser : ILipidParser
    {
        public string Target { get; } = "SG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^SG 28:1;O;Hex;LPA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 37,
            MassDiffDictionary.HydrogenMass * 64,
            MassDiffDictionary.OxygenMass * 11,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 1,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.CASLPHex, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class SISLPHexLipidParser : ILipidParser
    {
        public string Target { get; } = "SG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^SG 29:1;O;Hex;LPA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 38,
            MassDiffDictionary.HydrogenMass * 66,
            MassDiffDictionary.OxygenMass * 11,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 1,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.SISLPHex, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class STSLPHexLipidParser : ILipidParser
    {
        public string Target { get; } = "SG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^SG 29:2;O;Hex;LPA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 38,
            MassDiffDictionary.HydrogenMass * 64,
            MassDiffDictionary.OxygenMass * 11,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 1,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.STSLPHex, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class CSPHexLipidParser : ILipidParser
    {
        public string Target { get; } = "SG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(2);
        public static readonly string Pattern = $"^SG 27:1;O;Hex;PA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 36,
            MassDiffDictionary.HydrogenMass * 61,
            MassDiffDictionary.OxygenMass * 11,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 1,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.CSPHex, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class BRSPHexLipidParser : ILipidParser
    {
        public string Target { get; } = "SG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(2);
        public static readonly string Pattern = $"^SG 28:2;O;Hex;PA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 37,
            MassDiffDictionary.HydrogenMass * 61,
            MassDiffDictionary.OxygenMass * 11,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 1,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.BRSPHex, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class CASPHexLipidParser : ILipidParser
    {
        public string Target { get; } = "SG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(2);
        public static readonly string Pattern = $"^SG 28:1;O;Hex;PA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 37,
            MassDiffDictionary.HydrogenMass * 63,
            MassDiffDictionary.OxygenMass * 11,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 1,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.CASPHex, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class SISPHexLipidParser : ILipidParser
    {
        public string Target { get; } = "SG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(2);
        public static readonly string Pattern = $"^SG 29:1;O;Hex;PA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 38,
            MassDiffDictionary.HydrogenMass * 65,
            MassDiffDictionary.OxygenMass * 11,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 1,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.SISPHex, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class STSPHexLipidParser : ILipidParser
    {
        public string Target { get; } = "SG";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(2);
        public static readonly string Pattern = $"^SG 29:2;O;Hex;PA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 38,
            MassDiffDictionary.HydrogenMass * 63,
            MassDiffDictionary.OxygenMass * 11,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 1,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.STSPHex, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }

    public class CALipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:1;O5$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 40,
            MassDiffDictionary.OxygenMass * 5,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class DCALipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:1;O4$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 40,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class HDCALipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:1;O4$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 40,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class UDCALipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:1;O4$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 40,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();
        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class CDCALipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:1;O4$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 40,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class GCALipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O5$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 43,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class GDCALipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O4;G$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 43,
            MassDiffDictionary.OxygenMass * 5,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class GCDCALipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O3;G$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 43,
            MassDiffDictionary.OxygenMass * 5,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class GUDCALipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O3;G$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 43,
            MassDiffDictionary.OxygenMass * 5,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class GLCALipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O3;G$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 43,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class TCALipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O5;T$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 45,
            MassDiffDictionary.OxygenMass * 7,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class TUDCALipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O3;T$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 45,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class TCDCALipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O3;T$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 45,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class TDCALipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O4;T$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 45,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class TLCALipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O3;T$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 45,
            MassDiffDictionary.OxygenMass * 5,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class MCALipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:1;O5$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 40,
            MassDiffDictionary.OxygenMass * 5,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class TMCALipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O4;T$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 45,
            MassDiffDictionary.OxygenMass * 7,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class LCALipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:1;O3$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 40,
            MassDiffDictionary.OxygenMass * 3,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class GHCALipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O4;G$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 43,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class THCALipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 27:1;O5$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 45,
            MassDiffDictionary.OxygenMass * 7,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class ILCALipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:1;O3$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 40,
            MassDiffDictionary.OxygenMass * 3,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class KLCA_7LipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:2;O4$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 38,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class LCAHexLipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:1;O3;Hex$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 30,
            MassDiffDictionary.HydrogenMass * 50,
            MassDiffDictionary.OxygenMass * 8,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class KLCA_7_HexLipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:2;O3;Hex$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 30,
            MassDiffDictionary.HydrogenMass * 48,
            MassDiffDictionary.OxygenMass * 9,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class DCAHexLipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:1;O4;Hex$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 30,
            MassDiffDictionary.HydrogenMass * 50,
            MassDiffDictionary.OxygenMass * 9,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class CAHexLipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:1;O5;Hex$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 30,
            MassDiffDictionary.HydrogenMass * 50,
            MassDiffDictionary.OxygenMass * 10,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class GLCAHexLipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O3;G;Hex$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 32,
            MassDiffDictionary.HydrogenMass * 53,
            MassDiffDictionary.OxygenMass * 9,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class GDCAHexLipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O4;G;Hex$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 32,
            MassDiffDictionary.HydrogenMass * 53,
            MassDiffDictionary.OxygenMass * 10,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class GCAHexLipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O5;G;Hex$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 32,
            MassDiffDictionary.HydrogenMass * 53,
            MassDiffDictionary.OxygenMass * 11,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class TLCAHexLipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O3;T;Hex$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 32,
            MassDiffDictionary.HydrogenMass * 55,
            MassDiffDictionary.OxygenMass * 10,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class TDCAHexLipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O4;T;Hex$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 32,
            MassDiffDictionary.HydrogenMass * 55,
            MassDiffDictionary.OxygenMass * 11,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class TCAHexLipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O5;T;Hex$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 32,
            MassDiffDictionary.HydrogenMass * 55,
            MassDiffDictionary.OxygenMass * 12,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class LCASulfateLipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:1;O3;S$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 40,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class KLCA_7_SulfateLipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:2;O3;S$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 38,
            MassDiffDictionary.OxygenMass * 7,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class DCASulfateLipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:1;O4;S$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 40,
            MassDiffDictionary.OxygenMass * 7,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class CASulfateLipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 24:1;O5;S$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 24,
            MassDiffDictionary.HydrogenMass * 40,
            MassDiffDictionary.OxygenMass * 8,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class GLCASulfateLipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O3;G;S$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 43,
            MassDiffDictionary.OxygenMass * 7,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class GDCASulfateLipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O4;G;S$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 43,
            MassDiffDictionary.OxygenMass * 8,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class GCASulfateLipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O5;G;S$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 43,
            MassDiffDictionary.OxygenMass * 9,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class TLCASulfateLipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O3;T;S$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 45,
            MassDiffDictionary.OxygenMass * 8,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 2,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class TDCASulfateLipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O4;T;S$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 45,
            MassDiffDictionary.OxygenMass * 9,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 2,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class TCASulfateLipidParser : ILipidParser
    {
        public string Target { get; } = "BA";

        public static readonly string Pattern = $"^BA 24:1;O5;T;S$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 26,
            MassDiffDictionary.HydrogenMass * 45,
            MassDiffDictionary.OxygenMass * 10,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 2,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class CholesterolLipidParser : ILipidParser
    {
        public string Target { get; } = "ST";

        public static readonly string Pattern = $"^ST 27:1;O$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 27,
            MassDiffDictionary.HydrogenMass * 46,
            MassDiffDictionary.OxygenMass * 1,
            MassDiffDictionary.NitrogenMass * 0,
            MassDiffDictionary.PhosphorusMass * 0,
            MassDiffDictionary.SulfurMass * 0,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.ST, Skelton, chain);
            }
            return null;
        }
    }

    public class SHexLipidParser : ILipidParser
    {
        public string Target { get; } = "SG";
        double Skelton;

        public ILipid Parse(string lipidStr)
        {
            switch (lipidStr)
            {
                case "SG 27:1;O;Hex"://Cholesterol_Hex
                    Skelton = new[]
                    {
                        MassDiffDictionary.CarbonMass * 33,
                        MassDiffDictionary.HydrogenMass * 56,
                        MassDiffDictionary.OxygenMass * 6,
                        MassDiffDictionary.NitrogenMass * 0,
                        MassDiffDictionary.PhosphorusMass * 0,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                case "SG 28:2;O;Hex"://BRS_Hex
                    Skelton = new[]
                {
                        MassDiffDictionary.CarbonMass * 34,
                        MassDiffDictionary.HydrogenMass * 56,
                        MassDiffDictionary.OxygenMass * 6,
                        MassDiffDictionary.NitrogenMass * 0,
                        MassDiffDictionary.PhosphorusMass * 0,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                case "SG 28:1;O;Hex"://CAS_Hex
                    Skelton = new[]
                {
                        MassDiffDictionary.CarbonMass * 34,
                        MassDiffDictionary.HydrogenMass * 58,
                        MassDiffDictionary.OxygenMass * 6,
                        MassDiffDictionary.NitrogenMass * 0,
                        MassDiffDictionary.PhosphorusMass * 0,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                case "SG 29:1;O;Hex"://SIS_Hex
                    Skelton = new[]
                {
                        MassDiffDictionary.CarbonMass * 35,
                        MassDiffDictionary.HydrogenMass * 60,
                        MassDiffDictionary.OxygenMass * 6,
                        MassDiffDictionary.NitrogenMass * 0,
                        MassDiffDictionary.PhosphorusMass * 0,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                case "SG 29:2;O;Hex"://STS_Hex
                    Skelton = new[]
                {
                        MassDiffDictionary.CarbonMass * 35,
                        MassDiffDictionary.HydrogenMass * 58,
                        MassDiffDictionary.OxygenMass * 6,
                        MassDiffDictionary.NitrogenMass * 0,
                        MassDiffDictionary.PhosphorusMass * 0,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                case "SG 27:0;O;Hex"://Cholestan_Hex
                    Skelton = new[]
                    {
                        MassDiffDictionary.CarbonMass * 33,
                        MassDiffDictionary.HydrogenMass * 58,
                        MassDiffDictionary.OxygenMass * 6,
                        MassDiffDictionary.NitrogenMass * 0,
                        MassDiffDictionary.PhosphorusMass * 0,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                default:
                    return null;
            }
            var chain = new TotalChain(0, 0, 0, 0, 0, 0);
            return new Lipid(LbmClass.SHex, Skelton, chain);
        }
    }
    public class SPELipidParser : ILipidParser
    {
        public string Target { get; } = "ST";
        double Skelton;

        public ILipid Parse(string lipidStr)
        {
            switch (lipidStr)
            {
                case "ST 27:1;O;PE"://Cholesterol_PE
                    Skelton = new[]
                    {
                        MassDiffDictionary.CarbonMass * 29,
                        MassDiffDictionary.HydrogenMass * 52,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.NitrogenMass * 1,
                        MassDiffDictionary.PhosphorusMass * 1,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                    case "ST 28:2;O;PE"://BRS_PE
                        Skelton = new[]
                        {
                            MassDiffDictionary.CarbonMass * 30,
                            MassDiffDictionary.HydrogenMass * 52,
                            MassDiffDictionary.OxygenMass * 4,
                            MassDiffDictionary.NitrogenMass * 1,
                            MassDiffDictionary.PhosphorusMass * 1,
                            MassDiffDictionary.SulfurMass * 0,
                        }.Sum();
                    break;
                    case "ST 28:1;O;PE"://CAS_PE
                        Skelton = new[]
                        {
                            MassDiffDictionary.CarbonMass * 30,
                            MassDiffDictionary.HydrogenMass * 54,
                            MassDiffDictionary.OxygenMass * 4,
                            MassDiffDictionary.NitrogenMass * 1,
                            MassDiffDictionary.PhosphorusMass * 1,
                            MassDiffDictionary.SulfurMass * 0,
                        }.Sum();
                    break;
                    case "ST 29:1;O;PE"://SIS_PE
                        Skelton = new[]
                        {
                            MassDiffDictionary.CarbonMass * 31,
                            MassDiffDictionary.HydrogenMass * 56,
                            MassDiffDictionary.OxygenMass * 4,
                            MassDiffDictionary.NitrogenMass * 1,
                            MassDiffDictionary.PhosphorusMass * 1,
                            MassDiffDictionary.SulfurMass * 0,
                        }.Sum();
                    break;
                    case "ST 29:2;O;PE"://STS_PE
                        Skelton = new[]
                        {
                            MassDiffDictionary.CarbonMass * 31,
                            MassDiffDictionary.HydrogenMass * 54,
                            MassDiffDictionary.OxygenMass * 4,
                            MassDiffDictionary.NitrogenMass * 1,
                            MassDiffDictionary.PhosphorusMass * 1,
                            MassDiffDictionary.SulfurMass * 0,
                        }.Sum();
                    break;
                    case "ST 27:0;O;PE"://Cholestan_PE
                        Skelton = new[]
                        {
                            MassDiffDictionary.CarbonMass * 29,
                            MassDiffDictionary.HydrogenMass * 54,
                            MassDiffDictionary.OxygenMass * 4,
                            MassDiffDictionary.NitrogenMass * 1,
                            MassDiffDictionary.PhosphorusMass * 1,
                            MassDiffDictionary.SulfurMass * 0,
                        }.Sum();
                    break;
                default:
                    return null;
            }
            var chain = new TotalChain(0, 0, 0, 0, 0, 0);
            return new Lipid(LbmClass.SPE, Skelton, chain);
        }
    }

    public class SPEHexLipidParser : ILipidParser
    {
        public string Target { get; } = "SG";
        double Skelton;

        public ILipid Parse(string lipidStr)
        {
            switch (lipidStr)
            {
                case "SG 27:1;O;Hex;PE"://Cholesterol_PE_Hex
                    Skelton = new[]
                    {
                        MassDiffDictionary.CarbonMass * 35,
                        MassDiffDictionary.HydrogenMass * 62,
                        MassDiffDictionary.OxygenMass * 9,
                        MassDiffDictionary.NitrogenMass * 1,
                        MassDiffDictionary.PhosphorusMass * 1,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                case "SG 28:2;O;Hex;PE"://BRS_PE_Hex
                    Skelton = new[]
                {
                        MassDiffDictionary.CarbonMass * 36,
                        MassDiffDictionary.HydrogenMass * 62,
                        MassDiffDictionary.OxygenMass * 9,
                        MassDiffDictionary.NitrogenMass * 1,
                        MassDiffDictionary.PhosphorusMass * 1,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                case "SG 28:1;O;Hex;PE"://CAS_PE_Hex
                    Skelton = new[]
                {
                        MassDiffDictionary.CarbonMass * 36,
                        MassDiffDictionary.HydrogenMass * 64,
                        MassDiffDictionary.OxygenMass * 9,
                        MassDiffDictionary.NitrogenMass * 1,
                        MassDiffDictionary.PhosphorusMass * 1,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                case "SG 29:1;O;Hex;PE"://SIS_PE_Hex
                    Skelton = new[]
                {
                        MassDiffDictionary.CarbonMass * 37,
                        MassDiffDictionary.HydrogenMass * 66,
                        MassDiffDictionary.OxygenMass * 9,
                        MassDiffDictionary.NitrogenMass * 1,
                        MassDiffDictionary.PhosphorusMass * 1,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                case "SG 29:2;O;Hex;PE"://STS_PE_Hex
                    Skelton = new[]
                {
                        MassDiffDictionary.CarbonMass * 37,
                        MassDiffDictionary.HydrogenMass * 64,
                        MassDiffDictionary.OxygenMass * 9,
                        MassDiffDictionary.NitrogenMass * 1,
                        MassDiffDictionary.PhosphorusMass * 1,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                case "SG 27:0;O;Hex;PE"://Cholestan_PE_Hex
                    Skelton = new[]
                {
                        MassDiffDictionary.CarbonMass * 35,
                        MassDiffDictionary.HydrogenMass * 64,
                        MassDiffDictionary.OxygenMass * 9,
                        MassDiffDictionary.NitrogenMass * 1,
                        MassDiffDictionary.PhosphorusMass * 1,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                default:
                    return null;
            }
            var chain = new TotalChain(0, 0, 0, 0, 0, 0);
            return new Lipid(LbmClass.SPEHex, Skelton, chain);
        }
    }

    public class SPGHexLipidParser : ILipidParser
    {
        public string Target { get; } = "SG";
        double Skelton;

        public ILipid Parse(string lipidStr)
        {
            switch (lipidStr)
            {
                case "SG 27:1;O;Hex;PG"://Cholesterol_PG_Hex
                    Skelton = new[]
                    {
                        MassDiffDictionary.CarbonMass * 36,
                        MassDiffDictionary.HydrogenMass * 63,
                        MassDiffDictionary.OxygenMass * 11,
                        MassDiffDictionary.NitrogenMass * 0,
                        MassDiffDictionary.PhosphorusMass * 1,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                case "SG 28:2;O;Hex;PG"://BRS_PG_Hex
                    Skelton = new[]
                {
                        MassDiffDictionary.CarbonMass * 37,
                        MassDiffDictionary.HydrogenMass * 63,
                        MassDiffDictionary.OxygenMass * 11,
                        MassDiffDictionary.NitrogenMass * 0,
                        MassDiffDictionary.PhosphorusMass * 1,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                case "SG 28:1;O;Hex;PG"://CAS_PG_Hex
                    Skelton = new[]
                {
                        MassDiffDictionary.CarbonMass * 37,
                        MassDiffDictionary.HydrogenMass * 65,
                        MassDiffDictionary.OxygenMass * 11,
                        MassDiffDictionary.NitrogenMass * 0,
                        MassDiffDictionary.PhosphorusMass * 1,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                case "SG 29:1;O;Hex;PG"://SIS_PG_Hex
                    Skelton = new[]
                {
                        MassDiffDictionary.CarbonMass * 38,
                        MassDiffDictionary.HydrogenMass * 67,
                        MassDiffDictionary.OxygenMass * 11,
                        MassDiffDictionary.NitrogenMass * 0,
                        MassDiffDictionary.PhosphorusMass * 1,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                case "SG 29:2;O;Hex;PG"://STS_PG_Hex
                    Skelton = new[]
                {
                        MassDiffDictionary.CarbonMass * 38,
                        MassDiffDictionary.HydrogenMass * 65,
                        MassDiffDictionary.OxygenMass * 11,
                        MassDiffDictionary.NitrogenMass * 0,
                        MassDiffDictionary.PhosphorusMass * 1,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                case "SG 27:0;O;Hex;PG"://Cholestan_PG_Hex
                    Skelton = new[]
                {
                        MassDiffDictionary.CarbonMass * 36,
                        MassDiffDictionary.HydrogenMass * 65,
                        MassDiffDictionary.OxygenMass * 11,
                        MassDiffDictionary.NitrogenMass * 0,
                        MassDiffDictionary.PhosphorusMass * 1,
                        MassDiffDictionary.SulfurMass * 0,
                    }.Sum();
                    break;
                default:
                    return null;
            }
            var chain = new TotalChain(0, 0, 0, 0, 0, 0);
            return new Lipid(LbmClass.SPGHex, Skelton, chain);
        }
    }

    public class SSulfateLipidParser : ILipidParser
    {
        public string Target { get; } = "ST";
        double Skelton;

        public ILipid Parse(string lipidStr)
        {
            switch (lipidStr)
            {
                case "ST 27:1;O;S"://Cholesterol_Sulfate
                    Skelton = new[]
                        {
                            MassDiffDictionary.CarbonMass * 27,
                            MassDiffDictionary.HydrogenMass * 46,
                            MassDiffDictionary.OxygenMass * 4,
                            MassDiffDictionary.NitrogenMass * 0,
                            MassDiffDictionary.PhosphorusMass * 0,
                            MassDiffDictionary.SulfurMass * 1,
                        }.Sum();
                    break;
                case "ST 28:2;O;S"://BRS_Sulfate
                    Skelton = new[]
                    {
                            MassDiffDictionary.CarbonMass * 28,
                            MassDiffDictionary.HydrogenMass * 46,
                            MassDiffDictionary.OxygenMass * 4,
                            MassDiffDictionary.NitrogenMass * 0,
                            MassDiffDictionary.PhosphorusMass * 0,
                            MassDiffDictionary.SulfurMass * 1,
                        }.Sum();
                    break;
                case "ST 28:1;O;S"://CAS_Sulfate
                    Skelton = new[]
                {
                            MassDiffDictionary.CarbonMass * 28,
                            MassDiffDictionary.HydrogenMass * 48,
                            MassDiffDictionary.OxygenMass * 4,
                            MassDiffDictionary.NitrogenMass * 0,
                            MassDiffDictionary.PhosphorusMass * 0,
                            MassDiffDictionary.SulfurMass * 1,
                        }.Sum();
                    break;
                case "ST 29:1;O;S"://SIS_Sulfate
                    Skelton = new[]
                {
                            MassDiffDictionary.CarbonMass * 29,
                            MassDiffDictionary.HydrogenMass * 50,
                            MassDiffDictionary.OxygenMass * 4,
                            MassDiffDictionary.NitrogenMass * 0,
                            MassDiffDictionary.PhosphorusMass * 0,
                            MassDiffDictionary.SulfurMass * 1,
                        }.Sum();
                    break;
                case "ST 29:2;O;S"://STS_Sulfate
                    Skelton = new[]
                {
                            MassDiffDictionary.CarbonMass * 29,
                            MassDiffDictionary.HydrogenMass * 48,
                            MassDiffDictionary.OxygenMass * 4,
                            MassDiffDictionary.NitrogenMass * 0,
                            MassDiffDictionary.PhosphorusMass * 0,
                            MassDiffDictionary.SulfurMass * 1,
                        }.Sum();
                    break;
                case "ST 27:0;O;S"://Cholestan_Sulfate
                    Skelton = new[]
                {
                            MassDiffDictionary.CarbonMass * 27,
                            MassDiffDictionary.HydrogenMass * 48,
                            MassDiffDictionary.OxygenMass * 4,
                            MassDiffDictionary.NitrogenMass * 0,
                            MassDiffDictionary.PhosphorusMass * 0,
                            MassDiffDictionary.SulfurMass * 1,
                        }.Sum();
                    break;
                default:
                    return null;
            }
            var chain = new TotalChain(0, 0, 0, 0, 0, 0);
            return new Lipid(LbmClass.SSulfate, Skelton, chain);
        }
    }
}
