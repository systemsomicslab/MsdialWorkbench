using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using MessagePack;
using MessagePack.Formatters;
using System.IO;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    [MessagePackFormatter(typeof(AnnotatedMSDecResultFormatter))]
    public sealed class AnnotatedMSDecResult {
        public AnnotatedMSDecResult(MSDecResult mSDecResult, MsScanMatchResultContainer matchResults, MoleculeMsReference reference) {
            MSDecResult = mSDecResult;
            MatchResults = matchResults;
            Molecule = reference;
            QuantMass = reference.QuantMass != 0 ? reference.QuantMass : mSDecResult.ModelPeakMz;
        }

        public AnnotatedMSDecResult(MSDecResult mSDecResult, MsScanMatchResultContainer matchResults) {
            MSDecResult = mSDecResult;
            MatchResults = matchResults;
            QuantMass = mSDecResult.ModelPeakMz;
            Molecule = null;
        }

        public AnnotatedMSDecResult(MSDecResult mSDecResult, MsScanMatchResultContainer matchResults, IMoleculeProperty molecule, double quantMass) {
            MSDecResult = mSDecResult;
            MatchResults = matchResults;
            Molecule = molecule;
            QuantMass = quantMass;
        }

        [IgnoreMember]
        public MSDecResult MSDecResult { get; }
        [IgnoreMember]
        public MsScanMatchResultContainer MatchResults { get; }
        [IgnoreMember]
        public IMoleculeProperty Molecule { get; }
        [IgnoreMember]
        public double QuantMass { get; }

        [IgnoreMember]
        public bool IsUnknown => Molecule is null;
        public bool IsReferenceMatched(IMatchResultEvaluator<MsScanMatchResult> evaluator) => MatchResults.IsReferenceMatched(evaluator);

        public void Save(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(this, stream);
        }

        public static AnnotatedMSDecResult Load(Stream stream) {
            return MessagePackDefaultHandler.LoadFromStream<AnnotatedMSDecResult>(stream);
        }

        internal class AnnotatedMSDecResultFormatter : IMessagePackFormatter<AnnotatedMSDecResult>
        {
            public AnnotatedMSDecResult Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize) {
                var currentOffset = offset;
                var contentSize = MessagePackBinary.ReadArrayHeader(bytes, currentOffset, out int readTmp);
                currentOffset += readTmp;
                var raw = formatterResolver.GetFormatterWithVerify<byte[]>().Deserialize(bytes, currentOffset, formatterResolver, out readTmp);
                currentOffset += readTmp;
                var memory = new MemoryStream(raw, 0, raw.Length, writable: false);
                var mSDecResult = MsdecResultsReader.ReadMSDecResultVer1(memory, isAnnotationInfoIncluded: false);
                var matchResults = formatterResolver.GetFormatterWithVerify<MsScanMatchResultContainer>().Deserialize(bytes, currentOffset, formatterResolver, out readTmp);
                currentOffset += readTmp;
                var molecule = MoleculePropertyExtension.Formatter.Deserialize(bytes, currentOffset, formatterResolver, out readTmp);
                currentOffset += readTmp;
                var quantMass = formatterResolver.GetFormatterWithVerify<double>().Deserialize(bytes, currentOffset, formatterResolver, out readTmp);
                currentOffset += readTmp;
                readSize = currentOffset - offset;
                return new AnnotatedMSDecResult(mSDecResult, matchResults, molecule, quantMass);
            }

            public int Serialize(ref byte[] bytes, int offset, AnnotatedMSDecResult value, IFormatterResolver formatterResolver) {
                var currentOffset = offset;
                currentOffset += MessagePackBinary.WriteArrayHeader(ref bytes, currentOffset, 4);
                var memory = new MemoryStream();
                MsdecResultsWriter.MSDecWriterVer1(memory, value.MSDecResult);
                memory.Close();
                var buffer = memory.ToArray();
                currentOffset += formatterResolver.GetFormatterWithVerify<byte[]>().Serialize(ref bytes, currentOffset, buffer, formatterResolver);
                currentOffset += formatterResolver.GetFormatterWithVerify<MsScanMatchResultContainer>().Serialize(ref bytes, currentOffset, value.MatchResults, formatterResolver);
                currentOffset += MoleculePropertyExtension.Formatter.Serialize(ref bytes, currentOffset, value.Molecule, formatterResolver);
                currentOffset += formatterResolver.GetFormatterWithVerify<double>().Serialize(ref bytes, currentOffset, value.QuantMass, formatterResolver);
                return currentOffset - offset;
            }
        }
    }
}
