using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class HexCerLipidParser : ILipidParser
    {
        public string Target { get; } = "HexCer";

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 10,
            MassDiffDictionary.OxygenMass * 5,
        }.Sum();

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildCeramideParser(2);
        public static readonly string Pattern = $"^HexCer\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);
        private static readonly string OxidizedPattern = @"[;\(]((?<ox>O(?<oxnum>\d+)?)|((?<oxpos>\d+)OH,?)+\)?)";

        //public static readonly string CeramideClassPattern = @"\d+:(?<d>\d+).*?\(?((?<sp>\d+)OH,?)+\)?/\d+:\d+.*?(;?(?<h>\(?((?<ab>\d+)OH,?)+\)?|((?<oxnum>\d+)?O)))?";
        public static readonly string CeramideSphPattern = @"\d+:(?<d>\d+).*?([;\(]((?<sphoh>(O(?<sphohnum>\d+)?)|((?<sp>\d+)OH,?)+\))?))";
        public static readonly string CeramideAcylPattern = @"\d+:(?<acyld>\d+).*?(?<h>[;\(]((?<ox>O(?<oxnum>\d+)?)|((?<ab>\d+)OH,?)+\)?)?)?";



        public static readonly string CeramideClassPattern = $@"{CeramideSphPattern}/{CeramideAcylPattern}";

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
                    if (classGroup["sphoh"].Success)
                    {
                        if (classGroup["sp"].Success)
                        {
                            switch (classGroup["sp"].Value)
                            {
                                case "3":
                                    classString = classString + "S";
                                    break;
                                case "4":
                                    classString = classString + "P";
                                    break;
                            }
                        }
                        else if (classGroup["sphohnum"].Success)
                        {
                            switch (classGroup["sphohnum"].Value)
                            {
                                case "2":
                                    classString = classString + "S";
                                    break;
                                case "3":
                                    classString = classString + "P";
                                    break;
                            }
                        }
                        else
                        {
                            classString = classString + "S";
                        }

                    }
                }
                var lipidClass = new LbmClass();
                switch (classString)
                {
                    case "AS":
                    case "BS":
                    case "HS":
                        lipidClass = LbmClass.HexCer_HS;
                        break;
                    case "HDS":
                    case "BDS":
                    case "ADS":
                        lipidClass = LbmClass.HexCer_HDS;
                        break;
                    case "NDS":
                        lipidClass = LbmClass.HexCer_NDS;
                        break;
                    case "NS":
                        lipidClass = LbmClass.HexCer_NS;
                        break;
                    case "AP":
                    case "ADP":
                        lipidClass = LbmClass.HexCer_AP;
                        break;
                }
                return new Lipid(lipidClass, chains.Mass + Skelton, chains);
            }
            return null;
        }
    }
}
