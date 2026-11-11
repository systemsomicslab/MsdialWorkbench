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
using System.Buffers;
using System;

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

        /// <summary>
        /// Represents the molecular property of the annotated MSDec result.
        /// If no annotation is provided, this property will be <c>null</c>.
        /// </summary>
        [IgnoreMember]
        public IMoleculeProperty? Molecule { get; }

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

        internal class AnnotatedMSDecResultFormatter : IMessagePackFormatter<AnnotatedMSDecResult?>
        {
            public AnnotatedMSDecResult Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
                if (reader.TryReadNil()) {
                    return null;
                }
                var count = reader.ReadArrayHeader();
                if (count != 4) {
                    throw new MessagePackSerializationException($"Unexpected array length for {nameof(AnnotatedMSDecResult)}: {count}.");
                }
                var raw = reader.ReadBytes().GetValueOrDefault();
                var rawBytes = raw.IsEmpty ? [] : raw.ToArray();
                using var memory = new MemoryStream(rawBytes, 0, rawBytes.Length, writable: false);
                var mSDecResult = MsdecResultsReader.ReadMSDecResultVer1(memory, isAnnotationInfoIncluded: false);
                var matchResults = options.Resolver.GetFormatterWithVerify<MsScanMatchResultContainer>().Deserialize(ref reader, options);
                var molecule = options.Resolver.GetFormatterWithVerify<IMoleculeProperty?>().Deserialize(ref reader, options);
                var quantMass = reader.ReadDouble();
                return new AnnotatedMSDecResult(mSDecResult, matchResults, molecule, quantMass);
            }

            public void Serialize(ref MessagePackWriter writer, AnnotatedMSDecResult value, MessagePackSerializerOptions options) {
                if (value is null) {
                    writer.WriteNil();
                    return;
                }
                writer.WriteArrayHeader(4);
                using var memory = new MemoryStream();
                MsdecResultsWriter.MSDecWriterVer1(memory, value.MSDecResult);
                var buffer = memory.ToArray();
                writer.Write(buffer);
                options.Resolver.GetFormatterWithVerify<MsScanMatchResultContainer>().Serialize(ref writer, value.MatchResults, options);
                options.Resolver.GetFormatterWithVerify<IMoleculeProperty?>().Serialize(ref writer, value.Molecule, options);
                writer.Write(value.QuantMass);
            }
        }
    }
}
