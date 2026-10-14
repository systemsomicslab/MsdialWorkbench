using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    /// <summary>
    /// Settings of a staged lipid annotator. Not platform specific: the platform comes in through the
    /// annotator and query factory visitors.
    /// </summary>
    public sealed class StagedLipidAnnotatorSettingModel : BindableBase, IStagedLipidAnnotatorSettingModel
    {
        private readonly ILoadAnnotatorVisitor _annotatorVisitor;
        private readonly Func<IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>, IAnnotationQueryFactoryGenerationVisitor> _createFactory;

        public StagedLipidAnnotatorSettingModel(
            DataBaseSettingModel dataBaseSettingModel,
            string annotatorID,
            MsRefSearchParameterBase? searchParameter,
            ILoadAnnotatorVisitor annotatorVisitor,
            Func<IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>, IAnnotationQueryFactoryGenerationVisitor> createFactory) {
            DataBaseSettingModel = dataBaseSettingModel ?? throw new ArgumentNullException(nameof(dataBaseSettingModel));
            AnnotatorID = annotatorID;
            SearchParameter = searchParameter ?? new MsRefSearchParameterBase();
            _annotatorVisitor = annotatorVisitor ?? throw new ArgumentNullException(nameof(annotatorVisitor));
            _createFactory = createFactory ?? throw new ArgumentNullException(nameof(createFactory));
        }

        public DataBaseSettingModel DataBaseSettingModel { get; }

        public string AnnotatorID {
            get => _annotatorID;
            set => SetProperty(ref _annotatorID, value);
        }
        private string _annotatorID = string.Empty;

        public SourceType AnnotationSource { get; } = SourceType.GeneratedLipid;

        public MsRefSearchParameterBase SearchParameter { get; }

        public IAnnotationQueryFactory<MsScanMatchResult> CreateAnnotationQueryFactory(int priority, StagedLipidDatabase db, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            return CreateRestorationKey(priority).Accept(_createFactory(refer), _annotatorVisitor, db);
        }

        public IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, StagedLipidDatabase> CreateRestorationKey(int priority) {
            return new StagedLipidRestorationKey(AnnotatorID, priority, SearchParameter);
        }
    }
}
