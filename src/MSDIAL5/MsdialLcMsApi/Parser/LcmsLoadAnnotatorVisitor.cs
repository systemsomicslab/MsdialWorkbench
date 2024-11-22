using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using MessagePack;
using System;

namespace CompMs.MsdialLcMsApi.Parser
{
    [MessagePackObject]
    public sealed class LcmsLoadAnnotatorVisitor : ILoadAnnotatorVisitor
    {
        public LcmsLoadAnnotatorVisitor(ParameterBase parameter) {
            Parameter = parameter;
        }

        [Key(nameof(Parameter))]
        public ParameterBase Parameter { get; }

        public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(StandardRestorationKey key, MoleculeDataBase database) {
            if (key.SourceType.HasFlag(SourceType.MspDB)) {
                return new LcmsMspAnnotator(database, key.Parameter, Parameter.TargetOmics, Parameter.CollistionType, key.Key, key.Priority);
            }
            else if (key.SourceType.HasFlag(SourceType.TextDB)) {
                return new LcmsTextDBAnnotator(database, key.Parameter, key.Key, key.Priority);
            }
            throw new NotSupportedException(key.SourceType.ToString());
        }

        public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(MspDbRestorationKey key, MoleculeDataBase database) {
            return new LcmsMspAnnotator(database, Parameter.MspSearchParam, Parameter.TargetOmics, Parameter.CollistionType, key.Key, key.Priority);
        }

        public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(TextDbRestorationKey key, MoleculeDataBase database) {
            return new LcmsTextDBAnnotator(database, Parameter.TextDbSearchParam, key.Key, key.Priority);
        }

        public ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> Visit(ShotgunProteomicsRestorationKey key, ShotgunProteomicsDB database) {
            return new LcmsFastaAnnotator(database, key.MsRefSearchParameter, key.ProteomicsParameter, key.Key, key.SourceType, key.Priority);
        }

        public ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> Visit(EadLipidDatabaseRestorationKey key, EadLipidDatabase database) {
            return new EadLipidAnnotator(database, key.Key, key.Priority, key.MsRefSearchParameter);
        }
    }

    public sealed class LcmsAnnotationQueryFactoryGenerationVisitor : IAnnotationQueryFactoryGenerationVisitor {
        private readonly PeakPickBaseParameter _peakPickParameter;
        private readonly RefSpecMatchBaseParameter _searchParameter;
        private readonly ProteomicsParameter _proteomicsParameter;
        private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> _refer;

        public LcmsAnnotationQueryFactoryGenerationVisitor(PeakPickBaseParameter peakPickParameter, RefSpecMatchBaseParameter searchParameter, ProteomicsParameter proteomicsParameter, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            _peakPickParameter = peakPickParameter ?? throw new ArgumentNullException(nameof(peakPickParameter));
            _searchParameter = searchParameter ?? throw new ArgumentNullException(nameof(searchParameter));
            _proteomicsParameter = proteomicsParameter ?? throw new ArgumentNullException(nameof(proteomicsParameter));
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit(StandardRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder) {
            if (key.SourceType.HasFlag(SourceType.MspDB)) {
                return new AnnotationQueryFactory(finder, _peakPickParameter, key.Parameter, ignoreIsotopicPeak: true);
            }
            else if (key.SourceType.HasFlag(SourceType.TextDB)) {
                return new AnnotationQueryFactory(finder, _peakPickParameter, key.Parameter, ignoreIsotopicPeak: false);
            }
            throw new NotSupportedException(key.SourceType.ToString());
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit(MspDbRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder) {
            return new AnnotationQueryFactory(finder, _peakPickParameter, _searchParameter.MspSearchParam, ignoreIsotopicPeak: true);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit(TextDbRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder) {
            return new AnnotationQueryFactory(finder, _peakPickParameter, _searchParameter.TextDbSearchParam, ignoreIsotopicPeak: false);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit(ShotgunProteomicsRestorationKey key, IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> finder) {
            return new PepAnnotationQueryFactory(finder, _peakPickParameter, key.MsRefSearchParameter, _proteomicsParameter);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit(EadLipidDatabaseRestorationKey key, IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MsScanMatchResult> finder) {
            return new AnnotationQueryWithReferenceFactory(_refer, finder, _peakPickParameter, key.MsRefSearchParameter, ignoreIsotopicPeak: false);
        }
    }
}
