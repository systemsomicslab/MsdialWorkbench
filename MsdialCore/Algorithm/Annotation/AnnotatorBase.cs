using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Parser;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public abstract class AnnotatorBase<T, U> : IAnnotator<T, U>
        where T: IMSProperty
        where U: IMSScanProperty
    {
        public AnnotatorBase(IEnumerable<MoleculeMsReference> db, string sourceKey) {
            this.db = db.ToList();
            Key = sourceKey;
        }


        protected readonly List<MoleculeMsReference> db;
        public string Key { get; }

        public IReferRestorationKey Save(Stream stream) {
            Common.MessagePack.LargeListMessagePack.Serialize(stream, db);
            return new MspDbRestorationKey(Key);
        }

        public abstract MsScanMatchResult Annotate(T property, U scan, IReadOnlyList<IsotopicPeak> isotopes, MsRefSearchParameterBase parameter = null);
        public abstract List<MsScanMatchResult> FindCandidates(T property, U scan, IReadOnlyList<IsotopicPeak> isotopes, MsRefSearchParameterBase parameter = null);

        public abstract MsScanMatchResult CalculateScore(T property, U scan, IReadOnlyList<IsotopicPeak> isotopes, MoleculeMsReference reference, MsRefSearchParameterBase parameter = null);
        public abstract List<MoleculeMsReference> Search(T property, MsRefSearchParameterBase parameter = null);
        public abstract void Validate(MsScanMatchResult result, T property, U scan, IReadOnlyList<IsotopicPeak> isotopes, MoleculeMsReference reference, MsRefSearchParameterBase parameter = null);
        public abstract MoleculeMsReference Refer(MsScanMatchResult result);
    }
}