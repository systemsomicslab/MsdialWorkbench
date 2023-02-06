using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
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

        public virtual ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(StandardRestorationKey key, MoleculeDataBase database) {
            return new MassAnnotator(database, key.Parameter, Parameter.TargetOmics, key.SourceType, key.Key, key.Priority);
        }

        public virtual ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(MspDbRestorationKey key, MoleculeDataBase database) {
            return new MassAnnotator(database, Parameter.MspSearchParam, Parameter.TargetOmics, SourceType.MspDB, key.Key, key.Priority);
        }

        public virtual ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(TextDbRestorationKey key, MoleculeDataBase database) {
            return new MassAnnotator(database, Parameter.MspSearchParam, Parameter.TargetOmics, SourceType.TextDB, key.Key, key.Priority);
        }

        public ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> Visit(EadLipidDatabaseRestorationKey key, EadLipidDatabase database) {
            throw new NotImplementedException();
        }

        public ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> Visit(ShotgunProteomicsRestorationKey key, ShotgunProteomicsDB database) {
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

        public IAnnotationQueryFactory<MsScanMatchResult> Visit(StandardRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder) {
            return new AnnotationQueryFactory(finder, _peakPickParameter, key.Parameter, ignoreIsotopicPeak: key.SourceType != SourceType.TextDB);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit(MspDbRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder) {
            return new AnnotationQueryFactory(finder, _peakPickParameter, _searchParameter.MspSearchParam, ignoreIsotopicPeak: true);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit(TextDbRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder) {
            return new AnnotationQueryFactory(finder, _peakPickParameter, _searchParameter.TextDbSearchParam, ignoreIsotopicPeak: false);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit(ShotgunProteomicsRestorationKey key, IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> finder) {
            throw new NotImplementedException();
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Visit(EadLipidDatabaseRestorationKey key, IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MsScanMatchResult> finder) {
            throw new NotImplementedException();
        }
    }
}
