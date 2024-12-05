using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcImMsApi.Parser;
using System;

namespace CompMs.App.Msdial.Model.Lcimms
{
    public sealed class LcimmsMspAnnotatorSettingModel : BindableBase, IMetabolomicsAnnotatorSettingModel
    {
        private readonly ILoadAnnotatorVisitor _annotatorVisitor;
        private readonly Func<IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>, IAnnotationQueryFactoryGenerationVisitor> _createFuctory;

        public LcimmsMspAnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase? searchParameter, ILoadAnnotatorVisitor annotatorVisitor, Func<IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>, IAnnotationQueryFactoryGenerationVisitor> createFuctory) {
            DataBaseSettingModel = dataBaseSettingModel;
            AnnotatorID = annotatorID;
            SearchParameter = searchParameter ?? new MsRefSearchParameterBase();
            _annotatorVisitor = annotatorVisitor;
            _createFuctory = createFuctory;
        }

        public DataBaseSettingModel DataBaseSettingModel { get; }

        public string AnnotatorID {
            get => annotatorID;
            set => SetProperty(ref annotatorID, value);
        }
        private string annotatorID = string.Empty;
        
        public MsRefSearchParameterBase SearchParameter { get; }

        public SourceType AnnotationSource { get; } = SourceType.MspDB; 

        public IAnnotationQueryFactory<MsScanMatchResult> CreateAnnotationQueryFactory(int priority, MoleculeDataBase db, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            return new StandardRestorationKey(AnnotatorID, priority, SearchParameter, AnnotationSource).Accept(_createFuctory(refer), _annotatorVisitor, db);
        }

        public IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> CreateRestorationKey(int priority) {
            return new StandardRestorationKey(AnnotatorID, priority, SearchParameter, AnnotationSource);
        }
    }

    public sealed class LcimmsTextDBAnnotatorSettingModel : BindableBase, IMetabolomicsAnnotatorSettingModel
    {
        private readonly ILoadAnnotatorVisitor _annotatorVisitor;
        private readonly Func<IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>, IAnnotationQueryFactoryGenerationVisitor> _createFuctory;
        public LcimmsTextDBAnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase? searchParameter, ILoadAnnotatorVisitor annotatorVisitor, Func<IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>, IAnnotationQueryFactoryGenerationVisitor> createFuctory) {
            DataBaseSettingModel = dataBaseSettingModel;
            AnnotatorID = annotatorID;
            SearchParameter = searchParameter ?? new MsRefSearchParameterBase();
            _annotatorVisitor = annotatorVisitor;
            _createFuctory = createFuctory;
        }

        public DataBaseSettingModel DataBaseSettingModel { get; }

        public string AnnotatorID {
            get => annotatorID;
            set => SetProperty(ref annotatorID, value);
        }
        private string annotatorID = string.Empty;

        public SourceType AnnotationSource { get; } = SourceType.TextDB;

        public MsRefSearchParameterBase SearchParameter { get; } = new MsRefSearchParameterBase();

        public IAnnotationQueryFactory<MsScanMatchResult> CreateAnnotationQueryFactory(int priority, MoleculeDataBase db, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            return new StandardRestorationKey(AnnotatorID, priority, SearchParameter, AnnotationSource).Accept(_createFuctory(refer), _annotatorVisitor, db);
        }

        public IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> CreateRestorationKey(int priority) {
            return new StandardRestorationKey(AnnotatorID, priority, SearchParameter, AnnotationSource);
        }
    }

    public sealed class LcimmsAnnotatorSettingFactory : IAnnotatorSettingModelFactory
    {
        private readonly ParameterBase _parameter;
        private readonly ILoadAnnotatorVisitor _annotatorVisitor;

        public LcimmsAnnotatorSettingFactory(ParameterBase parameter) {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _annotatorVisitor = new LcimmsLoadAnnotatorVisitor(parameter);
        }

        private IAnnotationQueryFactoryGenerationVisitor CreateFactory(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            return new LcimmsAnnotationQueryFactoryGenerationVisitor(_parameter.PeakPickBaseParam, _parameter.RefSpecMatchBaseParam, _parameter.ProteomicsParam, refer);
        }
        public IAnnotatorSettingModel Create(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase? searchParameter = null) {
            switch (dataBaseSettingModel.DBSource) {
                case DataBaseSource.Msp:
                case DataBaseSource.Lbm:
                    return new LcimmsMspAnnotatorSettingModel(dataBaseSettingModel, annotatorID, searchParameter, _annotatorVisitor, CreateFactory);
                case DataBaseSource.Text:
                    return new LcimmsTextDBAnnotatorSettingModel(dataBaseSettingModel, annotatorID, searchParameter, _annotatorVisitor, CreateFactory);
                default:
                    throw new NotSupportedException(nameof(dataBaseSettingModel.DBSource));
            }
        }
    }
}
