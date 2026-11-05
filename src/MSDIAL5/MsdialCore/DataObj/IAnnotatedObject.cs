using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;

namespace CompMs.MsdialCore.DataObj
{
    public interface IAnnotatedObject
    {
        MsScanMatchResultContainer MatchResults { get; }
    }

    public static class AnnotatedObjectExtensions
    {
        public static string GetFormula(this IAnnotatedObject self, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            return self.MatchResults.RepresentativeFormula(refer);
        }

        public static string GetOntology(this IAnnotatedObject self, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            return self.MatchResults.RepresentativeOntology(refer);
        }

        public static string GetSMILES(this IAnnotatedObject self, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            return self.MatchResults.RepresentativeSMILES(refer);
        }

        public static string GetInChIKey(this IAnnotatedObject self, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            return self.MatchResults.RepresentativeInChIKey(refer);
        }

        public static string GetProtein(this IAnnotatedObject self, IMatchResultRefer<PeptideMsReference, MsScanMatchResult> refer) {
            return self.MatchResults.RepresentativeProtein(refer);
        }

        public static T? Refer<T>(this IAnnotatedObject? self, IMatchResultRefer<T?, MsScanMatchResult?> refer) {
            return refer.Refer(self?.MatchResults.Representative);
        }

        /// <summary>
        /// <paramref name="name"/> as every export must show it: see
        /// <see cref="CompMs.Common.Utility.AnnotationName.Canonical(string, bool)"/>.
        /// </summary>
        /// <remarks>
        /// Takes the feature rather than the flag so that a caller cannot pair one feature's name
        /// with another's chain verdict, or forget the verdict and silently publish an sn-chain
        /// composition that nothing measured. The exporters that have a match result in hand
        /// already -- the metadata accessors, mzTab-M -- pass it directly instead, so that the name
        /// agrees with the rest of the row they are building.
        /// </remarks>
        public static string CanonicalName(this IAnnotatedObject? self, string name) {
            return CompMs.Common.Utility.AnnotationName.Canonical(
                name, self?.MatchResults?.Representative?.IsLipidChainsMatch ?? false);
        }
    }
}