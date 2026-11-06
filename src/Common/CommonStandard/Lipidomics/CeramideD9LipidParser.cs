using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class CeramideD9LipidParser : ILipidParser
    {
        public string Target { get; } = "Cer_d9";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildCeramideParser(2);
        public static readonly string Pattern = $"^Cer_d9\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        public static readonly string CeramideClassPattern = @"\d+:(?<d>\d+).*?\)?;?\(?((?<oxSph>O(?<oxnumSph>\d+)?)|((?<sp>\d+)OH,?)+\)?)/\d+:\d+.*?(;?(?<h>\(?((?<ab>\d+)OH,?)+\)?|(O(?<oxnum>\d+)?)))?";

        private static readonly Regex ceramideClassPattern = new Regex(CeramideClassPattern, RegexOptions.Compiled);

        private static readonly double massBalanceD9 = new[]
{
            MassDiffDictionary.Hydrogen2Mass * 9,
            - MassDiffDictionary.HydrogenMass * 9,
        }.Sum();

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
                        lipidClass = LbmClass.Cer_AH_d9;
                        break;
                    case "NH":
                        lipidClass = LbmClass.Cer_NH_d9;
                        break;
                }
                return new Lipid(lipidClass, chains.Mass + massBalanceD9, chains);
            }
            return null;
        }
    }
}
