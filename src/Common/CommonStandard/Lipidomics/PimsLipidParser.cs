using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class PimsLipidParser : ILipidParser
    {
        public string Target { get; } = "Ac";

        public static readonly string ClassPattern = $"^Ac(?<chaincount>\\d)PIM(?<suger>\\d)\\s*({AcylChainParser.Pattern}_?)+$";
        private static readonly Regex classPattern = new Regex(ClassPattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 9,
            MassDiffDictionary.HydrogenMass * 19,
            MassDiffDictionary.OxygenMass * 11,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double Suger = new[]
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 11,
            MassDiffDictionary.OxygenMass * 5,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var classMatch = classPattern.Match(lipidStr);
            if (classMatch.Success)
            {
                var group = classMatch.Groups;
                var suger = int.Parse(classMatch.Groups["suger"].Value);
                var chaincount = int.Parse(classMatch.Groups["chaincount"].Value);
                TotalChainParser chainsParser = TotalChainParser.BuildParser(chaincount);
                string Pattern = $"^Ac(?<chaincount>\\d)PIM(?<suger>\\d)\\s*(?<sn>{chainsParser.Pattern})$";
                Regex pattern = new Regex(Pattern, RegexOptions.Compiled);
                var match = pattern.Match(lipidStr);
                if (match.Success)
                {
                    var chaingroup = match.Groups;
                    var chains = chainsParser.Parse(chaingroup["sn"].Value);
                    var mass = Skelton + chains.Mass + Suger * suger - MassDiffDictionary.HydrogenMass * (suger + chaincount);
                    if (chaincount == 2 && suger == 1)
                    {
                        return new Lipid(LbmClass.Ac2PIM1, mass, chains);
                    }
                    else if (chaincount == 2 && suger == 2)
                    {
                        return new Lipid(LbmClass.Ac2PIM2, mass, chains);
                    }
                    else if (chaincount == 3 && suger == 2)
                    {
                        return new Lipid(LbmClass.Ac3PIM2, mass, chains);
                    }
                    else if (chaincount == 4 && suger == 2)
                    {
                        return new Lipid(LbmClass.Ac4PIM2, mass, chains);
                    }
                }
                return null;

            }
            return null;

        }
    }
}
