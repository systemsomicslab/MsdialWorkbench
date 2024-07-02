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
    }
}