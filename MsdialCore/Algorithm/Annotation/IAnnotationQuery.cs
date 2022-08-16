using CompMs.Common.DataObj.Property;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface ICallableAnnotationQuery<out TResult>
    {
        IMSIonProperty Property { get; }
        IMSScanProperty Scan { get; }
        IMSScanProperty NormalizedScan { get; }
        IReadOnlyList<IsotopicPeak> Isotopes { get; }
        IonFeatureCharacter IonFeature { get; }
        MsRefSearchParameterBase Parameter { get; }

        IEnumerable<TResult> FindCandidates();
    }

    public interface IAnnotationQuery
    {
        IMSIonProperty Property { get; }
        IMSScanProperty Scan { get; }
        IMSScanProperty NormalizedScan { get; }
        IReadOnlyList<IsotopicPeak> Isotopes { get; }
        IonFeatureCharacter IonFeature { get; }
        MsRefSearchParameterBase Parameter { get; }
    }
}
