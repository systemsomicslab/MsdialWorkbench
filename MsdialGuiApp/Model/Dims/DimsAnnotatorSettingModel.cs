using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using CompMs.MsdialDimsCore.Parser;
using CompMs.MsdialLcMsApi.Parser;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Dims
{
    public sealed class DimsMetabolomicsUseMs2AnnotatorSettingModel : BindableBase, IMetabolomicsAnnotatorSettingModel {
        private readonly ILoadAnnotatorVisitor _annotatorVisitor;
        private readonly Func<IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, IAnnotationQueryFactoryGenerationVisitor> _createFuctory;

        public DimsMetabolomicsUseMs2AnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase searchParameter, ILoadAnnotatorVisitor annotatorVisitor, Func<IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, IAnnotationQueryFactoryGenerationVisitor> createFuctory) {
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

        public MsRefSearchParameterBase SearchParameter { get; }

        public SourceType AnnotationSource { get; } = SourceType.MspDB;

        public string AnnotatorID {
            get => annotatorID;
            set => SetProperty(ref annotatorID, value);
        }
        private string annotatorID;

        public DataBaseSettingModel DataBaseSettingModel { get; }

        public List<ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> CreateAnnotator(MoleculeDataBase db, int priority, TargetOmics omics) {
            return new List<ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>>
            {
                new DimsMspAnnotator(db, SearchParameter, omics, annotatorID, priority),
            };
        }

        public IAnnotationQueryFactory<MsScanMatchResult> CreateAnnotationQueryFactory(int priority, MoleculeDataBase db, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            return new StandardRestorationKey(AnnotatorID, priority, SearchParameter, AnnotationSource).Accept(_createFuctory(refer), _annotatorVisitor, db);
        }
    }

    public sealed class DimsMetabolomicsAnnotatorSettingModel : BindableBase, IMetabolomicsAnnotatorSettingModel
    {
        private readonly ILoadAnnotatorVisitor _annotatorVisitor;
        private readonly Func<IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, IAnnotationQueryFactoryGenerationVisitor> _createFuctory;

        public DimsMetabolomicsAnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase searchParameter, ILoadAnnotatorVisitor annotatorVisitor, Func<IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, IAnnotationQueryFactoryGenerationVisitor> createFuctory) {
            DataBaseSettingModel = dataBaseSettingModel;
            AnnotatorID = annotatorID;
            SearchParameter = searchParameter ?? new MsRefSearchParameterBase();
            _annotatorVisitor = annotatorVisitor;
            _createFuctory = createFuctory;
        }

        public MsRefSearchParameterBase SearchParameter { get; }

        public SourceType AnnotationSource { get; } = SourceType.TextDB;

        public string AnnotatorID {
            get => annotatorID;
            set => SetProperty(ref annotatorID, value);
        }
        private string annotatorID;

        public DataBaseSettingModel DataBaseSettingModel { get; }

        public List<ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> CreateAnnotator(MoleculeDataBase db, int priority, TargetOmics omics) {
            return new List<ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>>
            {
                new DimsTextDBAnnotator(db, SearchParameter, annotatorID, priority),
            };
        }

        public IAnnotationQueryFactory<MsScanMatchResult> CreateAnnotationQueryFactory(int priority, MoleculeDataBase db, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            return new StandardRestorationKey(AnnotatorID, priority, SearchParameter, AnnotationSource).Accept(_createFuctory(refer), _annotatorVisitor, db);
        }
    }

    public sealed class DimsEadLipidAnnotatorSettingModel : BindableBase, IEadLipidAnnotatorSettingModel
    {
        private readonly ILoadAnnotatorVisitor _annotatorVisitor;
        private readonly Func<IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, IAnnotationQueryFactoryGenerationVisitor> _createFuctory;

        public DimsEadLipidAnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase searchParameter, ILoadAnnotatorVisitor annotatorVisitor, Func<IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, IAnnotationQueryFactoryGenerationVisitor> createFuctory) {
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

        public List<ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>> CreateAnnotator(EadLipidDatabase db, int priority) {
            return new List<ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>> {
                new EadLipidAnnotator(db, AnnotatorID, priority, SearchParameter),
            };
        }

        public IAnnotationQueryFactory<MsScanMatchResult> CreateAnnotationQueryFactory(int priority, EadLipidDatabase db, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            return new EadLipidDatabaseRestorationKey(AnnotatorID, priority, SearchParameter, SourceType.GeneratedLipid).Accept(_createFuctory(refer), _annotatorVisitor, db);
        }
    }

    public sealed class DimsAnnotatorSettingModelFactory : IAnnotatorSettingModelFactory
    {
        private readonly ParameterBase _parameter;
        private readonly ILoadAnnotatorVisitor _annotatorVisitor;

        public DimsAnnotatorSettingModelFactory(ParameterBase parameter) {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _annotatorVisitor = new DimsLoadAnnotatorVisitor(parameter);
        }

        private IAnnotationQueryFactoryGenerationVisitor CreateFactory(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            return new DimsAnnotationQueryFactoryGenerationVisitor(_parameter.PeakPickBaseParam, _parameter.RefSpecMatchBaseParam, _parameter.ProteomicsParam, refer);
        }

        public IAnnotatorSettingModel Create(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase searchParameter = null) {
            switch (dataBaseSettingModel.DBSource) {
                case DataBaseSource.Msp:
                case DataBaseSource.Lbm:
                    return new DimsMetabolomicsUseMs2AnnotatorSettingModel(dataBaseSettingModel, annotatorID, searchParameter, _annotatorVisitor, CreateFactory);
                case DataBaseSource.Text:
                    return new DimsMetabolomicsAnnotatorSettingModel(dataBaseSettingModel, annotatorID, searchParameter, _annotatorVisitor, CreateFactory);
                case DataBaseSource.EieioLipid:
                case DataBaseSource.OadLipid:
                    return new DimsEadLipidAnnotatorSettingModel(dataBaseSettingModel, annotatorID, searchParameter, _annotatorVisitor, CreateFactory);
                default:
                    throw new NotSupportedException(nameof(dataBaseSettingModel.DBSource));
            }
        }
    }
}
