using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Parameter;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IPepAnnotationQuery : IAnnotationQuery<MsScanMatchResult> {
        MsRefSearchParameterBase MsRefSearchParameter { get; }
        ProteomicsParameter ProteomicsParameter { get; }
    }
}
