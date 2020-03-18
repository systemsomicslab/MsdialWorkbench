using System.Collections.Generic;
using System.Linq;

namespace NCDK.Sgroups
{
    public static class SgroupTool
    {   
        /// <summary>
        /// Key to store/fetch CTab Sgroups from Molfiles. Important! - Use at your own risk,
        /// property is transitive and may be removed in future with a more specific accessor.
        /// </summary>
        public const string CtabSgroupsPropertyKey = "cdk:CtabSgroups";

        public static IList<Sgroup> GetCtabSgroups(this IAtomContainer o)
            => o.GetProperty<IList<Sgroup>>(CtabSgroupsPropertyKey);

        public static void SetCtabSgroups(this IAtomContainer o, IList<Sgroup> chirality)
            => o.SetProperty(CtabSgroupsPropertyKey, chirality);

        private static Dictionary<string, SgroupType> StrToSgroupTypeMap { get; } = new Dictionary<string, SgroupType>
        {
            [""] = 0,
            ["SUP"] = SgroupType.CtabAbbreviation,
            ["MUL"] = SgroupType.CtabMultipleGroup,
            ["SRU"] = SgroupType.CtabStructureRepeatUnit,
            ["MON"] = SgroupType.CtabMonomer,
            ["MOD"] = SgroupType.CtabModified,
            ["COP"] = SgroupType.CtabCopolymer,
            ["MER"] = SgroupType.CtabMer,
            ["CRO"] = SgroupType.CtabCrossLink,
            ["GRA"] = SgroupType.CtabMultipleGroup,
            ["ANY"] = SgroupType.CtabAnyPolymer,
            ["COM"] = SgroupType.CtabComponent,
            ["MIX"] = SgroupType.CtabMixture,
            ["FOR"] = SgroupType.CtabFormulation,
            ["DAT"] = SgroupType.CtabData,
            ["GEN"] = SgroupType.CtabGeneric,
            ["N/A"] = SgroupType.ExtMulticenter,
        };

        public static string Key(this SgroupType value)
            => StrToSgroupTypeMap.Keys.ElementAt((int)value);

        public static SgroupType ToSgroupType(string str)
        {
            if (!StrToSgroupTypeMap.TryGetValue(str, out SgroupType o))
                return SgroupType.CtabGeneric;
            return o;
        }

        public static IReadOnlyList<SgroupKey> SgroupKeyValues { get; } = System.Enum.GetValues(typeof(SgroupKey)).Cast<SgroupKey>().ToArray();
    }
}
