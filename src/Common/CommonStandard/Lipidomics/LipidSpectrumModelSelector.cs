using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    /// <summary>
    /// Maps the XML <c>&lt;LipidClass&gt;</c> names of <see cref="ILipidSpectrumModel"/> to
    /// <see cref="LbmClass"/>, and picks the model to use for a concrete lipid.
    /// </summary>
    public static class LipidSpectrumModelSelector
    {
        // The XML occasionally splits one LbmClass's rules by chain subtype with a "_P" (plasmalogen)
        // or "_O" (alkyl ether) suffix that isn't itself an LbmClass member (e.g. "EtherLPE_P" and
        // "EtherLPE_O" both belong to LbmClass.EtherLPE). Only these two unambiguous suffixes are
        // stripped; other names without an LbmClass equivalent are left unresolved, not guessed at.
        private static readonly string[] _subtypeSuffixes = { "_P", "_O" };

        public static bool TryResolveLbmClass(string lipidClassName, out LbmClass lbmClass) {
            lbmClass = default;
            if (string.IsNullOrEmpty(lipidClassName)) {
                return false;
            }
            if (System.Enum.TryParse(lipidClassName, out lbmClass)) {
                return true;
            }
            foreach (var suffix in _subtypeSuffixes) {
                if (lipidClassName.EndsWith(suffix)
                    && System.Enum.TryParse(lipidClassName.Substring(0, lipidClassName.Length - suffix.Length), out lbmClass)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Every model whose class resolves to the lipid's class and that has a rule set for the adduct,
        /// best match first: a "_P"/"_O" subtype matching the lipid's alkyl chain, then the unsplit
        /// class, then a non-matching subtype.
        /// </summary>
        public static IReadOnlyList<ILipidSpectrumModel> FindCandidates(IEnumerable<ILipidSpectrumModel> models, ILipid lipid, AdductIon adduct) {
            var chains = lipid.Chains?.GetDeterminedChains() ?? new IChain[0];
            var hasPlasmalogen = chains.OfType<AlkylChain>().Any(c => c.IsPlasmalogen);
            var hasAlkylEther = chains.OfType<AlkylChain>().Any(c => !c.IsPlasmalogen);
            return models
                .Where(m => TryResolveLbmClass(m.LipidClassName, out var lbm) && lbm == lipid.LipidClass)
                .Where(m => m.FindRuleSet(adduct.AdductIonName) != null)
                .OrderBy(m => RankBySubtype(m.LipidClassName, hasPlasmalogen, hasAlkylEther))
                .ToList();
        }

        public static ILipidSpectrumModel? Select(IEnumerable<ILipidSpectrumModel> models, ILipid lipid, AdductIon adduct) {
            return FindCandidates(models, lipid, adduct).FirstOrDefault();
        }

        private static int RankBySubtype(string lipidClassName, bool hasPlasmalogen, bool hasAlkylEther) {
            if (lipidClassName.EndsWith("_P")) {
                return hasPlasmalogen ? 0 : 2;
            }
            if (lipidClassName.EndsWith("_O")) {
                return hasAlkylEther ? 0 : 2;
            }
            return 1;
        }
    }
}
