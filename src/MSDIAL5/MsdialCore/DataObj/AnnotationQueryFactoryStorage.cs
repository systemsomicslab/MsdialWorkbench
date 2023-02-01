using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class AnnotationQueryFactoryStorage
    {
        public AnnotationQueryFactoryStorage(
            IEnumerable<IAnnotationQueryFactory<MsScanMatchResult>> moleculeQueryFactories,
            IEnumerable<IAnnotationQueryFactory<MsScanMatchResult>> peptideQueryFactories,
            IEnumerable<IAnnotationQueryFactory<MsScanMatchResult>> secondQueryFactotries) {

            _moleculeQueryFactories = moleculeQueryFactories.ToList();
            MoleculeQueryFactories = _moleculeQueryFactories.AsReadOnly();
            _peptideQueryFactories = peptideQueryFactories.ToList();
            PeptideQueryFactories = _peptideQueryFactories.AsReadOnly();
            _secondQueryFactories = secondQueryFactotries.ToList();
            SecondQueryFactories = _secondQueryFactories.AsReadOnly();
        }

        public AnnotationQueryFactoryStorage() {
            _moleculeQueryFactories = new List<IAnnotationQueryFactory<MsScanMatchResult>>();
            MoleculeQueryFactories = _moleculeQueryFactories.AsReadOnly();
            _peptideQueryFactories = new List<IAnnotationQueryFactory<MsScanMatchResult>>();
            PeptideQueryFactories = _peptideQueryFactories.AsReadOnly();
            _secondQueryFactories = new List<IAnnotationQueryFactory<MsScanMatchResult>>();
            SecondQueryFactories = _secondQueryFactories.AsReadOnly();
        }

        public ReadOnlyCollection<IAnnotationQueryFactory<MsScanMatchResult>> MoleculeQueryFactories { get; }
        private List<IAnnotationQueryFactory<MsScanMatchResult>> _moleculeQueryFactories;
        public ReadOnlyCollection<IAnnotationQueryFactory<MsScanMatchResult>> PeptideQueryFactories { get; }
        private List<IAnnotationQueryFactory<MsScanMatchResult>> _peptideQueryFactories;
        public ReadOnlyCollection<IAnnotationQueryFactory<MsScanMatchResult>> SecondQueryFactories { get; }
        private List<IAnnotationQueryFactory<MsScanMatchResult>> _secondQueryFactories;
    }
}
