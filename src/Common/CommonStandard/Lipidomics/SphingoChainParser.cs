using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class SphingoChainParser
    {
        private static readonly string CarbonPattern = @"(?<carbon>\d+)";
        private static readonly string DoubleBondPattern = @"(?<db>\d+)(\(((?<dbpos>\d+[EZ]?),?)+\))?";
        private static readonly string OxidizedPattern = @"[;\(]((?<ox>O(?<oxnum>\d+)?)|((?<oxpos>\d+)OH,?)+\)?)";

        //private static readonly string OxidizedPattern = @";(((?<oxpos>\d+)OH,?)+|(?<ox>(?<oxnum>\d+)?O))";

        public static readonly string Pattern = $"{CarbonPattern}:{DoubleBondPattern}{OxidizedPattern}";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        public IChain Parse(string chainStr) {
            var match = pattern.Match(chainStr);
            if (match.Success) {
                var groups = match.Groups;
                var numCarbon = int.Parse(groups["carbon"].Value);
                var db = ParseDoubleBond(groups);
                var ox = ParseOxidized(groups);
                return new SphingoChain(numCarbon, db, ox);
            }
            return null;
        }

        private DoubleBond ParseDoubleBond(GroupCollection groups) {
            var dbnum = int.Parse(groups["db"].Value);
            if (groups["dbpos"].Success) {
                return new DoubleBond(dbnum, groups["dbpos"].Captures.Cast<Capture>().Select(c => ParseDoubleBondInfo(c.Value)).ToArray());
            }
            if (groups["oxpos"].Success)
            {
                var oxposnum = groups["oxpos"].Captures.Cast<Capture>().Select(c => int.Parse(c.Value)).ToArray().Length;
                if (oxposnum > 2)
                {
                    return new DoubleBond(dbnum);
                }
            }
            else if (groups["oxnum"].Success && int.Parse(groups["oxnum"].Value) > 2)
            {
                return new DoubleBond(dbnum);
            }
            else if (dbnum >= 1) // sphingo doublebond >=1 one of them position "4"
            {
                return new DoubleBond(dbnum, DoubleBondInfo.Create(4));
            }
            return new DoubleBond(dbnum);
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
                var ox = !groups["oxnum"].Success ? 1 : int.Parse(groups["oxnum"].Value);
                switch (ox) //TBC
                {
                    case 1:
                        return new Oxidized(ox, 1);
                    case 2:
                        return new Oxidized(ox, 1, 3);
                    case 3:
                        return new Oxidized(ox, 1, 3, 4);
                }
                return new Oxidized(int.Parse(groups["oxnum"].Value), 1, 3);
            }
            return new Oxidized(0);
        }
    }
}
