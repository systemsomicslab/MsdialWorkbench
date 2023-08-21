using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.DataStructure;
using CompMs.Common.Lipidomics;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using System;

namespace CompMs.MsdialDimsCore.Parser
{
    public sealed class DimsLoadAnnotatorVisitor : ILoadAnnotatorVisitor
    {
        public DimsLoadAnnotatorVisitor(ParameterBase parameter) {
            Parameter = parameter;
        }

        public ParameterBase Parameter { get; }

        public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit((StandardRestorationKey key, MoleculeDataBase database) item) {
            if (item.key.SourceType.HasFlag(SourceType.MspDB)) {
                return new DimsMspAnnotator(item.database, item.key.Parameter, Parameter.TargetOmics, item.key.Key, item.key.Priority);
            }
            if (item.key.SourceType.HasFlag(SourceType.TextDB)) {
                return new DimsTextDBAnnotator(item.database, item.key.Parameter, item.key.Key, item.key.Priority);
            }
            throw new NotSupportedException(item.key.SourceType.ToString());
        }

        public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit((MspDbRestorationKey key, MoleculeDataBase database) item) {
            return new DimsMspAnnotator(item.database, Parameter.MspSearchParam, Parameter.TargetOmics, item.key.Key, item.key.Priority);
        }

        public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit((TextDbRestorationKey key, MoleculeDataBase database) item) {
            return new DimsTextDBAnnotator(item.database, Parameter.TextDbSearchParam, item.key.Key, item.key.Priority);
        }

        public ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> Visit((ShotgunProteomicsRestorationKey key, ShotgunProteomicsDB database) item) {
            throw new NotSupportedException("Currently ShotgunProteomicsDB is not supported.");
        }

        public ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> Visit((EadLipidDatabaseRestorationKey key, EadLipidDatabase database) item) {
            return new EadLipidAnnotator(item.database, item.key.Key, item.key.Priority, item.key.MsRefSearchParameter);
        }

        ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, ILipid), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> IVisitor<ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, ILipid), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>, (EadLipidDatabaseRestorationKey key, EadLipidDatabase database)>.Visit((EadLipidDatabaseRestorationKey key, EadLipidDatabase database) item) {
            return new EadLipidAnnotator(item.database, item.key.Key, item.key.Priority, item.key.MsRefSearchParameter);
        }
    }

    public sealed class DimsAnnotationQueryFactoryGenerationVisitor : IAnnotationQueryFactoryGenerationVisitor {
        private readonly PeakPickBaseParameter _peakPickParameter;
        private readonly RefSpecMatchBaseParameter _searchParameter;
        private readonly ProteomicsParameter _proteomicsParameter;
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> _refer;
        private readonly LipidStructureMapper _lipidStructureMapper;

        public DimsAnnotationQueryFactoryGenerationVisitor(PeakPickBaseParameter peakPickParameter, RefSpecMatchBaseParameter searchParameter, ProteomicsParameter proteomicsParameter, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            _peakPickParameter = peakPickParameter ?? throw new ArgumentNullException(nameof(peakPickParameter));
            _searchParameter = searchParameter ?? throw new ArgumentNullException(nameof(searchParameter));
            _proteomicsParameter = proteomicsParameter ?? throw new ArgumentNullException(nameof(proteomicsParameter));
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
            _lipidStructureMapper = new LipidStructureMapper();
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit((StandardRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder) item) {
            if (item.key.SourceType.HasFlag(SourceType.MspDB)) {
                return new AnnotationQueryWithoutIsotopeFactory(item.finder, item.key.Parameter);
            }
            else if (item.key.SourceType.HasFlag(SourceType.TextDB)) {
                return new AnnotationQueryWithoutIsotopeFactory(item.finder, item.key.Parameter);
            }
            throw new NotSupportedException(item.key.SourceType.ToString());
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit((MspDbRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder) item) {
            return new AnnotationQueryWithoutIsotopeFactory(item.finder, _searchParameter.MspSearchParam);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit((TextDbRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder) item) {
            return new AnnotationQueryWithoutIsotopeFactory(item.finder, _searchParameter.TextDbSearchParam);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit((ShotgunProteomicsRestorationKey key, IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> finder) item) {
            return new PepAnnotationQueryFactory(item.finder, _peakPickParameter, item.key.MsRefSearchParameter, _proteomicsParameter);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit((EadLipidDatabaseRestorationKey key, IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MsScanMatchResult> finder) item) {
            return new AnnotationQueryWithReferenceFactory(_refer, item.finder, _peakPickParameter, item.key.MsRefSearchParameter, ignoreIsotopicPeak: false);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit((EadLipidDatabaseRestorationKey key, IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, ILipid), MsScanMatchResult> finder) item) {
            return new GeneratedLipidAnnotationQueryFactory(_refer, item.finder, _peakPickParameter, item.key.MsRefSearchParameter, _lipidStructureMapper.Map, ignoreIsotopicPeak: false);
        }
    }
}
