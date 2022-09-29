using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Imms
{

    public sealed class ImmsMspAnnotatorSettingModel : BindableBase, IMetabolomicsAnnotatorSettingModel
    {
        public ImmsMspAnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase searchParameter) {
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

        public DataBaseSettingModel DataBaseSettingModel { get; }

        public string AnnotatorID {
            get => annotatorID;
            set => SetProperty(ref annotatorID, value);
        }
        private string annotatorID = string.Empty;

        public SourceType AnnotationSource { get; } = SourceType.MspDB;

        public MsRefSearchParameterBase SearchParameter { get; } = new MsRefSearchParameterBase();

        public List<ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> CreateAnnotator(MoleculeDataBase db, int priority, TargetOmics omics) {
            return new List<ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> {
                new ImmsMspAnnotator(db, SearchParameter, omics, AnnotatorID, priority)
            };
        }
    }

    public sealed class ImmsTextDBAnnotatorSettingModel : BindableBase, IMetabolomicsAnnotatorSettingModel
    {
        public ImmsTextDBAnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase searchParameter) {
            DataBaseSettingModel = dataBaseSettingModel;
            AnnotatorID = annotatorID;
            SearchParameter = searchParameter ?? new MsRefSearchParameterBase();
        }

        public DataBaseSettingModel DataBaseSettingModel { get; }

        public string AnnotatorID {
            get => annotatorID;
            set => SetProperty(ref annotatorID, value);
        }
        private string annotatorID = string.Empty;

        public SourceType AnnotationSource { get; } = SourceType.TextDB;

        public MsRefSearchParameterBase SearchParameter { get; } = new MsRefSearchParameterBase();

        public List<ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> CreateAnnotator(MoleculeDataBase db, int priority, TargetOmics omics) {
            return new List<ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> {
                new ImmsTextDBAnnotator(db, SearchParameter, AnnotatorID, priority)
            };
        }
    }

    public sealed class ImmsEadLipidAnnotatorSettingModel : BindableBase, IEadLipidAnnotatorSettingModel
    {
        public ImmsEadLipidAnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase searchParameter) {
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

    public sealed class ImmsAnnotatorSettingModelFactory : IAnnotatorSettingModelFactory
    {
        public IAnnotatorSettingModel Create(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase searchParameter = null) {
            switch (dataBaseSettingModel.DBSource) {
                case DataBaseSource.Msp:
                case DataBaseSource.Lbm:
                    return new ImmsMspAnnotatorSettingModel(dataBaseSettingModel, annotatorID, searchParameter);
                case DataBaseSource.Text:
                    return new ImmsTextDBAnnotatorSettingModel(dataBaseSettingModel, annotatorID, searchParameter);
                case DataBaseSource.EieioLipid:
                case DataBaseSource.OadLipid:
                    return new ImmsEadLipidAnnotatorSettingModel(dataBaseSettingModel, annotatorID, searchParameter);
                default:
                    throw new NotSupportedException(nameof(dataBaseSettingModel.DBSource));
            }
        }
    }
}
