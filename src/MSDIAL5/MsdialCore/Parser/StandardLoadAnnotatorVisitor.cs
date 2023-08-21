using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.DataStructure;
using CompMs.Common.Lipidomics;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;

namespace CompMs.MsdialCore.Parser
{
    public class StandardLoadAnnotatorVisitor : ILoadAnnotatorVisitor
    {
        public StandardLoadAnnotatorVisitor(ParameterBase parameter) {
            Parameter = parameter;
        }

        public ParameterBase Parameter { get; }

        public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit((StandardRestorationKey key, MoleculeDataBase database) item) {
            return new MassAnnotator(item.database, item.key.Parameter, Parameter.TargetOmics, item.key.SourceType, item.key.Key, item.key.Priority);
        }

        public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit((MspDbRestorationKey key, MoleculeDataBase database) item) {
            return new MassAnnotator(item.database, Parameter.MspSearchParam, Parameter.TargetOmics, SourceType.MspDB, item.key.Key, item.key.Priority);
        }

        public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit((TextDbRestorationKey key, MoleculeDataBase database) item) {
            return new MassAnnotator(item.database, Parameter.MspSearchParam, Parameter.TargetOmics, SourceType.TextDB, item.key.Key, item.key.Priority);
        }

        public ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> Visit((EadLipidDatabaseRestorationKey key, EadLipidDatabase database) item) {
            throw new NotImplementedException();
        }

        public ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> Visit((ShotgunProteomicsRestorationKey key, ShotgunProteomicsDB database) item) {
            throw new NotImplementedException();
        }

        ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, ILipid), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> IVisitor<ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, ILipid), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>, (EadLipidDatabaseRestorationKey key, EadLipidDatabase database)>.Visit((EadLipidDatabaseRestorationKey key, EadLipidDatabase database) item) {
            throw new NotImplementedException();
        }
    }

    public sealed class StandardAnnotationQueryFactoryGenerationVisitor : IAnnotationQueryFactoryGenerationVisitor {
        private readonly PeakPickBaseParameter _peakPickParameter;
        private readonly RefSpecMatchBaseParameter _searchParameter;

        public StandardAnnotationQueryFactoryGenerationVisitor(PeakPickBaseParameter peakPickParameter, RefSpecMatchBaseParameter searchParameter) {
            _peakPickParameter = peakPickParameter ?? throw new ArgumentNullException(nameof(peakPickParameter));
            _searchParameter = searchParameter;
        }       

        public IAnnotationQueryFactory<MsScanMatchResult> Visit((StandardRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder) item) {
            return new AnnotationQueryFactory(item.finder, _peakPickParameter, item.key.Parameter, ignoreIsotopicPeak: item.key.SourceType != SourceType.TextDB);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit((MspDbRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder) item) {
            return new AnnotationQueryFactory(item.finder, _peakPickParameter, _searchParameter.MspSearchParam, ignoreIsotopicPeak: true);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit((TextDbRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder) item) {
            return new AnnotationQueryFactory(item.finder, _peakPickParameter, _searchParameter.TextDbSearchParam, ignoreIsotopicPeak: false);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit((ShotgunProteomicsRestorationKey key, IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> finder) item) {
            throw new NotImplementedException();
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit((EadLipidDatabaseRestorationKey key, IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MsScanMatchResult> finder) item) {
            throw new NotImplementedException();
        }

        IAnnotationQueryFactory<MsScanMatchResult> IVisitor<IAnnotationQueryFactory<MsScanMatchResult>, (EadLipidDatabaseRestorationKey key, IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, ILipid), MsScanMatchResult> finder)>.Visit((EadLipidDatabaseRestorationKey key, IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, ILipid), MsScanMatchResult> finder) item) {
            throw new NotImplementedException();
        }
    }
}
