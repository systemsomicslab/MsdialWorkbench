using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class CoqLipidParser : ILipidParser
    {
        public string Target { get; } = "CoQ";

        public static readonly string Pattern = $"^CoQ\\s*(?<length>\\d*)$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 9,
            MassDiffDictionary.HydrogenMass * 10,
            MassDiffDictionary.OxygenMass * 4,
        }.Sum();

        private static readonly double Chain = new[]
{
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 8,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var length = int.Parse(match.Groups["length"].Value);
                var chain = new TotalChain(0, 0, 0, 0, 0, 0);
                return new Lipid(LbmClass.CoQ, Skelton + Chain * length, chain);
            }
            return null;
        }
    }
}