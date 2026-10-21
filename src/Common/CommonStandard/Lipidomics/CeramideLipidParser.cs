using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class CeramideLipidParser : ILipidParser
    {
        public string Target { get; } = "Cer";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildCeramideParser(2);
        public static readonly string Pattern = $"^Cer\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        //public static readonly string CeramideClassPattern = @"\d+:(?<d>\d+).*?\(?((?<sp>\d+)OH,?)+\)?/\d+:\d+.*?(;?(?<h>\(?((?<ab>\d+)OH,?)+\)?|(O(?<oxnum>\d+)?)))?";
        public static readonly string CeramideClassPattern = @"\d+:(?<d>\d+).*?\)?;?\(?((?<oxSph>O(?<oxnumSph>\d+)?)|((?<sp>\d+)OH,?)+\)?)/\d+:\d+.*?(;?(?<h>\(?((?<ab>\d+)OH,?)+\)?|(O(?<oxnum>\d+)?)))?";

        private static readonly Regex ceramideClassPattern = new Regex(CeramideClassPattern, RegexOptions.Compiled);

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                var classPattern = ceramideClassPattern.Match(chains.ToString());
                var classString = "";
                if (classPattern.Success)
                {
                    var classGroup = classPattern.Groups;
                    if (classGroup["h"].Success)
                    {
                        if (classGroup["ab"].Success)
                        {
                            if (classGroup["h"].Value.Contains("2OH,3OH"))
                            {
                                classString = classString + "AB";
                            }
                            else
                            {
                                switch (classGroup["ab"].Value)
                                {
                                    case "2":
                                        classString = classString + "A";
                                        break;
                                    case "3":
                                        classString = classString + "B";
                                        break;
                                    default:
                                        classString = classString + "H";
                                        break;
                                }
                            }
                        }
                        else
                        {
                            classString = classString + "H";
                        }
                    }
                    else
                    {
                        classString = classString + "N";
                    }

                    if (classGroup["d"].Value == "0")
                    {
                        classString = classString + "D";
                    }

                    if (classGroup["sp"].Success)
                    {
                        switch (classGroup["sp"].Value)
                        {
                            case "3":
                                classString = classString + "S";
                                break;
                            case "4":
                                if (classGroup["d"].Value == "0")
                                {
                                    classString = classString + "P";
                                }
                                else if (classGroup["d"].Value == "1")
                                {
                                    classString = classString + "H";
                                }
                                break;
                        }
                    }
                    else
                    {
                        switch (classGroup["oxnumSph"].Value)
                        {
                            case "2":
                                classString = classString + "S";
                                break;
                            case "3":
                                if (classGroup["d"].Value == "0")
                                {
                                    classString = classString + "P";
                                }
                                else if (classGroup["d"].Value == "1")
                                {
                                    classString = classString + "H";
                                }
                                break;
                        }
                    }
                }
                var lipidClass = new LbmClass();
                switch (classString)
                {
                    case "ADS":
                        lipidClass = LbmClass.Cer_ADS;
                        break;
                    case "AS":
                        lipidClass = LbmClass.Cer_AS;
                        break;
                    case "BDS":
                        lipidClass = LbmClass.Cer_BDS;
                        break;
                    case "BS":
                        lipidClass = LbmClass.Cer_BS;
                        break;
                    case "NDS":
                        lipidClass = LbmClass.Cer_NDS;
                        break;
                    case "NS":
                        lipidClass = LbmClass.Cer_NS;
                        break;
                    case "AP":
                    case "ADP":
                        lipidClass = LbmClass.Cer_AP;
                        break;
                    case "ABP":
                    case "ABDP":
                        lipidClass = LbmClass.Cer_ABP;
                        break;
                    case "NP":
                    case "NDP":
                        lipidClass = LbmClass.Cer_NP;
                        break;
                    case "HDS":
                        lipidClass = LbmClass.Cer_HDS;
                        break;
                    case "HS":
                        lipidClass = LbmClass.Cer_HS;
                        break;
                    case "AH":
                        lipidClass = LbmClass.Cer_AH;
                        break;
                    case "NH":
                        lipidClass = LbmClass.Cer_NH;
                        break;
                }
                return new Lipid(lipidClass, chains.Mass, chains);
            }
            return null;
        }
    }

    public class SpbLipidParser : ILipidParser
    {
        public string Target { get; } = "SPB";

        private static readonly SpbChainParser chainsParser = new SpbChainParser();
        public static readonly string Pattern = $"^SPB\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                var chainOxNum = 0;
                if (group["oxpos"].Success)
                {
                    var oxposnum = group["oxpos"].Captures.Cast<Capture>().Select(c => int.Parse(c.Value)).ToArray().Length;
                    if (oxposnum > 2)
                    {
                        chainOxNum = oxposnum;
                    }
                }
                else if (group["oxnum"].Success)
                {
                    chainOxNum = int.Parse(group["oxnum"].Value);
                }
                switch (chainOxNum)
                {
                    case 2:
                        if (group["db"].Value == "0")
                        {
                            return new Lipid(LbmClass.DHSph, chains.Mass + MassDiffDictionary.HydrogenMass, chains);
                        }
                        else
                        {
                            return new Lipid(LbmClass.Sph, chains.Mass + MassDiffDictionary.HydrogenMass, chains);
                        }
                    case 3:
                        return new Lipid(LbmClass.PhytoSph, chains.Mass + MassDiffDictionary.HydrogenMass, chains);
                }

            }
            return null;
        }
    }
    public class SpbChainParser : TotalChainParser
    {
        public int ChainCount { get; }
        public int Capacity { get; }
        public string Pattern { get; }
        private readonly Regex Expression;

        private static readonly int chainCount = 1;
        private static readonly int capacity = 1;
        private static readonly bool atLeastSpeciesLevel = true;

        public SpbChainParser()
            : base(chainCount: chainCount, capacity: capacity, hasSphingosine: true, hasEther: false, atLeastSpeciesLevel: atLeastSpeciesLevel)
        {
            ChainCount = chainCount;
            Capacity = capacity;
            var submolecularLevelPattern = $"(?<TotalChain>(?<Chain>{SphingoChainParser.Pattern}))";
            var molecularSpeciesLevelPattern =
                $"(?<MolecularSpeciesLevel>(?<Chain>{SphingoChainParser.Pattern}))";
            var positionLevelPattern =
                $@"(?<PositionLevel>(?<Chain>{SphingoChainParser.Pattern}))";
            var patterns = new[] { positionLevelPattern, molecularSpeciesLevelPattern, };
            var totalPattern = string.Join("|", atLeastSpeciesLevel ? patterns : patterns.Append(submolecularLevelPattern));
            var totalExpression = new Regex(totalPattern, RegexOptions.Compiled);
            Pattern = totalPattern;
            Expression = totalExpression;
        }
        public new ITotalChain Parse(string lipidStr)
        {
            var match = Expression.Match(lipidStr);
            if (match.Success)
            {
                var groups = match.Groups;
                var chains = ParsePositionLevelChains(groups);
                return new MolecularSpeciesLevelChains(
                            chains.GetDeterminedChains()
                                .Concat(Enumerable.Range(0, Capacity - chains.ChainCount).Select(_ => new AcylChain(0, DoubleBond.CreateFromPosition(), Oxidized.CreateFromPosition())))
                                .ToArray()
                        );
            }
            return null;
        }

    }
}
