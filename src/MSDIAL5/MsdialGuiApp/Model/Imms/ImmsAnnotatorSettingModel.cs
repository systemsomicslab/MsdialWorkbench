using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Parser;
using System;

namespace CompMs.App.Msdial.Model.Imms
{

    public sealed class ImmsMspAnnotatorSettingModel : BindableBase, IMetabolomicsAnnotatorSettingModel
    {
        private readonly ILoadAnnotatorVisitor _annotatorVisitor;
        private readonly Func<IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>, IAnnotationQueryFactoryGenerationVisitor> _createFuctory;

        public ImmsMspAnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase? searchParameter, ILoadAnnotatorVisitor annotatorVisitor, Func<IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>, IAnnotationQueryFactoryGenerationVisitor> createFuctory) {
            DataBaseSettingModel = dataBaseSettingModel;
            AnnotatorID = annotatorID;
            _annotatorVisitor = annotatorVisitor;
            _createFuctory = createFuctory;
            if (dataBaseSettingModel.DBSource == DataBaseSource.Msp) {
                SearchParameter = searchParameter ?? new MsRefSearchParameterBase {
                    SimpleDotProductCutOff = 0.5F,
                    WeightedDotProductCutOff = 0.5F,
                    ReverseDotProductCutOff = 0.7F,
                    MatchedPeaksPercentageCutOff = 0.3F,
                    MinimumSpectrumMatch = 3,
                };
            }
            else { // meaning lbm
                SearchParameter = searchParameter ?? new MsRefSearchParameterBase {
                    SimpleDotProductCutOff = 0.1F,
                    WeightedDotProductCutOff = 0.1F,
                    ReverseDotProductCutOff = 0.1F,
                    MatchedPeaksPercentageCutOff = 0.0F,
                    MinimumSpectrumMatch = 1
                };
            }
            
        }

        public DataBaseSettingModel DataBaseSettingModel { get; }

        public string AnnotatorID {
            get => annotatorID;
            set => SetProperty(ref annotatorID, value);
        }
        private string annotatorID = string.Empty;

        public SourceType AnnotationSource { get; } = SourceType.MspDB;

        public MsRefSearchParameterBase SearchParameter { get; } = new MsRefSearchParameterBase();

        public IAnnotationQueryFactory<MsScanMatchResult> CreateAnnotationQueryFactory(int priority, MoleculeDataBase db, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            return new StandardRestorationKey(AnnotatorID, priority, SearchParameter, AnnotationSource).Accept(_createFuctory(refer), _annotatorVisitor, db);
        }

        public IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> CreateRestorationKey(int priority) {
            return new StandardRestorationKey(AnnotatorID, priority, SearchParameter, AnnotationSource);
        }
    }

    public sealed class ImmsTextDBAnnotatorSettingModel : BindableBase, IMetabolomicsAnnotatorSettingModel
    {
        private readonly ILoadAnnotatorVisitor _annotatorVisitor;
        private readonly Func<IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>, IAnnotationQueryFactoryGenerationVisitor> _createFuctory;

        public ImmsTextDBAnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase? searchParameter, ILoadAnnotatorVisitor annotatorVisitor, Func<IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>, IAnnotationQueryFactoryGenerationVisitor> createFuctory) {
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

    public sealed class ImmsEadLipidAnnotatorSettingModel : BindableBase, IEadLipidAnnotatorSettingModel
    {
        private readonly ILoadAnnotatorVisitor _annotatorVisitor;
        private readonly Func<IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>, IAnnotationQueryFactoryGenerationVisitor> _createFuctory;

        public ImmsEadLipidAnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase? searchParameter, ILoadAnnotatorVisitor annotatorVisitor, Func<IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>, IAnnotationQueryFactoryGenerationVisitor> createFuctory) {
            DataBaseSettingModel = dataBaseSettingModel;
            AnnotatorID = annotatorID;
            SearchParameter = searchParameter ?? new MsRefSearchParameterBase {
                SimpleDotProductCutOff = 0.1F,
                WeightedDotProductCutOff = 0.1F,
                ReverseDotProductCutOff = 0.1F,
                MatchedPeaksPercentageCutOff = 0.0F,
                MinimumSpectrumMatch = 1
            };
            _annotatorVisitor = annotatorVisitor;
            _createFuctory = createFuctory;
        }

        public DataBaseSettingModel DataBaseSettingModel { get; }

        public string AnnotatorID {
            get => annotatorID;
            set => SetProperty(ref annotatorID, value);
        }
        private string annotatorID = string.Empty;

        public SourceType AnnotationSource { get; } = SourceType.GeneratedLipid;

        public MsRefSearchParameterBase SearchParameter { get; } = new MsRefSearchParameterBase();

        public IAnnotationQueryFactory<MsScanMatchResult> CreateAnnotationQueryFactory(int priority, EadLipidDatabase db, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            return new EadLipidDatabaseRestorationKey(AnnotatorID, priority, SearchParameter, SourceType.GeneratedLipid).Accept(_createFuctory(refer), _annotatorVisitor, db);
        }

        public IReferRestorationKey<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> CreateRestorationKey(int priority) {
            return new EadLipidDatabaseRestorationKey(AnnotatorID, priority, SearchParameter, SourceType.GeneratedLipid);
        }
    }

    public sealed class ImmsAnnotatorSettingModelFactory : IAnnotatorSettingModelFactory
    {
        private readonly ParameterBase _parameter;
        private readonly ILoadAnnotatorVisitor _annotatorVisitor;

        public ImmsAnnotatorSettingModelFactory(ParameterBase parameter) {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _annotatorVisitor = new ImmsLoadAnnotatorVisitor(parameter);
        }

        private IAnnotationQueryFactoryGenerationVisitor CreateFactory(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            return new ImmsAnnotationQueryFactoryGenerationVisitor(_parameter.PeakPickBaseParam, _parameter.RefSpecMatchBaseParam, _parameter.ProteomicsParam, refer);
        }

        public IAnnotatorSettingModel Create(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase? searchParameter = null) {
            switch (dataBaseSettingModel.DBSource) {
                case DataBaseSource.Msp:
                case DataBaseSource.Lbm:
                    return new ImmsMspAnnotatorSettingModel(dataBaseSettingModel, annotatorID, searchParameter, _annotatorVisitor, CreateFactory);
                case DataBaseSource.Text:
                    return new ImmsTextDBAnnotatorSettingModel(dataBaseSettingModel, annotatorID, searchParameter, _annotatorVisitor, CreateFactory);
                case DataBaseSource.EieioLipid:
                case DataBaseSource.EidLipid:
                case DataBaseSource.OadLipid:
                    return new ImmsEadLipidAnnotatorSettingModel(dataBaseSettingModel, annotatorID, searchParameter, _annotatorVisitor, CreateFactory);
                default:
                    throw new NotSupportedException(nameof(dataBaseSettingModel.DBSource));
            }
        }
    }
}
