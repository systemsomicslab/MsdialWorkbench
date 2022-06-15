using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Normalize
{
    public interface INormalizationTarget
    {
        int Id { get; }
        int InternalStandardId { get; set; }
        IonAbundanceUnit IonAbundanceUnit { get; set; }
        IReadOnlyList<INormalizationTarget> Children { get; }
        IReadOnlyList<INormalizableValue> Values { get; }
        bool IsReferenceMatched(IMatchResultEvaluator<MsScanMatchResult> evaluator);
        MoleculeMsReference RetriveReference(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer);
    }
}
