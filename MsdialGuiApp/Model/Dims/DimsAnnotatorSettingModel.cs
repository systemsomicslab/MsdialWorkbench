using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Dims
{
    public sealed class DimsMetabolomicsUseMs2AnnotatorSettingModel : BindableBase, IMetabolomicsAnnotatorSettingModel {
        public DimsMetabolomicsUseMs2AnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase searchParameter) {
            DataBaseSettingModel = dataBaseSettingModel;
            AnnotatorID = annotatorID;
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

        public List<ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> CreateAnnotator(MoleculeDataBase db, int priority, TargetOmics omics) {
            return new List<ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>>
            {
                new DimsMspAnnotator(db, SearchParameter, omics, annotatorID, priority),
            };
        }
    }

    public sealed class DimsMetabolomicsAnnotatorSettingModel : BindableBase, IMetabolomicsAnnotatorSettingModel
    {
        public DimsMetabolomicsAnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase searchParameter) {
            DataBaseSettingModel = dataBaseSettingModel;
            AnnotatorID = annotatorID;
            SearchParameter = searchParameter ?? new MsRefSearchParameterBase();
        }

        public MsRefSearchParameterBase SearchParameter { get; }

        public SourceType AnnotationSource { get; } = SourceType.TextDB;

        public string AnnotatorID {
            get => annotatorID;
            set => SetProperty(ref annotatorID, value);
        }
        private string annotatorID;

        public DataBaseSettingModel DataBaseSettingModel { get; }

        public List<ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> CreateAnnotator(MoleculeDataBase db, int priority, TargetOmics omics) {
            return new List<ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>>
            {
                new DimsTextDBAnnotator(db, SearchParameter, annotatorID, priority),
            };
        }
    }

    public sealed class DimsEadLipidAnnotatorSettingModel : BindableBase, IEadLipidAnnotatorSettingModel
    {
        public DimsEadLipidAnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase searchParameter) {
            DataBaseSettingModel = dataBaseSettingModel;
            AnnotatorID = annotatorID;
            SearchParameter = searchParameter ?? new MsRefSearchParameterBase {
                SimpleDotProductCutOff = 0.1F,
                WeightedDotProductCutOff = 0.1F,
                ReverseDotProductCutOff = 0.1F,
                MatchedPeaksPercentageCutOff = 0.0F,
                MinimumSpectrumMatch = 1
            };
        }

        public DataBaseSettingModel DataBaseSettingModel { get; }

        public string AnnotatorID {
            get => annotatorID;
            set => SetProperty(ref annotatorID, value);
        }
        private string annotatorID = string.Empty;

        public SourceType AnnotationSource { get; } = SourceType.GeneratedLipid;

        public MsRefSearchParameterBase SearchParameter { get; } = new MsRefSearchParameterBase();

        public List<ISerializableAnnotator<(IAnnotationQuery, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>> CreateAnnotator(EadLipidDatabase db, int priority) {
            return new List<ISerializableAnnotator<(IAnnotationQuery, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>> {
                new EadLipidAnnotator(db, AnnotatorID, priority, SearchParameter),
            };
        }
    }

    public sealed class DimsAnnotatorSettingModelFactory : IAnnotatorSettingModelFactory
    {
        public IAnnotatorSettingModel Create(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase searchParameter = null) {
            switch (dataBaseSettingModel.DBSource) {
                case DataBaseSource.Msp:
                case DataBaseSource.Lbm:
                    return new DimsMetabolomicsUseMs2AnnotatorSettingModel(dataBaseSettingModel, annotatorID, searchParameter);
                case DataBaseSource.Text:
                    return new DimsMetabolomicsAnnotatorSettingModel(dataBaseSettingModel, annotatorID, searchParameter);
                case DataBaseSource.EieioLipid:
                case DataBaseSource.OadLipid:
                    return new DimsEadLipidAnnotatorSettingModel(dataBaseSettingModel, annotatorID, searchParameter);
                default:
                    throw new NotSupportedException(nameof(dataBaseSettingModel.DBSource));
            }
        }
    }
}
