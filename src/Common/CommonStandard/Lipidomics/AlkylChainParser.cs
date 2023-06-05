using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class AlkylChainParser
    {
        private static readonly string CarbonPattern = @"(?<carbon>\d+)";
        private static readonly string DoubleBondPattern = @"(?<db>\d+)(\(((?<dbpos>\d+[EZ]?),?)+\))?";
        private static readonly string OxidizedPattern = @"[;\(]((?<ox>O(?<oxnum>\d+)?)|((?<oxpos>\d+)OH,?)+\)?)";
        //private static readonly string OxidizedPattern = @";(\(?((?<oxpos>\d+)OH,?)+\)?|(?<ox>(?<oxnum>\d+)?O))";

        public static readonly string Pattern = $"(?<plasm>[OP])-{CarbonPattern}:{DoubleBondPattern}({OxidizedPattern})?";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        public IChain Parse(string chainStr) {
            var match = pattern.Match(chainStr);
            if (match.Success) {
                var groups = match.Groups;
                var numCarbon = int.Parse(groups["carbon"].Value);
                var ox = ParseOxidized(groups);
                if (groups["plasm"].Success) {
                    switch (groups["plasm"].Value) {
                        case "P":
                            return new AlkylChain(numCarbon, ParseDoubleBond(groups, true), ox);
                        case "O":
                            return new AlkylChain(numCarbon, ParseDoubleBond(groups, false), ox);
                    }
                }
            }
            return null;
        }

        private DoubleBond ParseDoubleBond(GroupCollection groups, bool isPlasma) {
            var dbnum = int.Parse(groups["db"].Value) + (isPlasma ? 1 : 0);
            if (groups["dbpos"].Success) {
                if (isPlasma) {
                    return new DoubleBond(dbnum, groups["dbpos"].Captures.Cast<Capture>().Select(c => ParseDoubleBondInfo(c.Value)).Prepend(DoubleBondInfo.Create(1)).ToArray());
                }
                else {
                    return new DoubleBond(dbnum, groups["dbpos"].Captures.Cast<Capture>().Select(c => ParseDoubleBondInfo(c.Value)).ToArray());
                }
            }
            else {
                if (isPlasma) {
                    return new DoubleBond(dbnum, DoubleBondInfo.Create(1));
                }
                else {
                    return new DoubleBond(dbnum);
                }
            }
        }

        private DoubleBondInfo ParseDoubleBondInfo(string bond) {
            switch (bond[bond.Length - 1]){
                case 'E':
                    return DoubleBondInfo.E(int.Parse(bond.TrimEnd('E')));
                case 'Z':
                    return DoubleBondInfo.Z(int.Parse(bond.TrimEnd('Z')));
                default:
                    return DoubleBondInfo.Create(int.Parse(bond));
            }
        }

        private Oxidized ParseOxidized(GroupCollection groups) {
            if (groups["oxpos"].Success) {
                var oxpos = groups["oxpos"].Captures.Cast<Capture>().Select(c => int.Parse(c.Value)).ToArray();
                return new Oxidized(oxpos.Length, oxpos);
            }
            if (groups["ox"].Success) {
                if (groups["oxnum"].Success) {
                    return new Oxidized(int.Parse(groups["oxnum"].Value));
                }
                return new Oxidized(1);
            }
            return new Oxidized(0);
        }
    }
}
