using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CompMs.App.Msdial.Model.Lcms
{
    // TODO: These interfaces maybe moved to MsdialCore project in future.
    public interface ILcmsAnnotatorSettingModel : INotifyPropertyChanged
    {
        DataBaseSettingModel DataBaseSettingModel { get; }
        string AnnotatorID { get; set; }
        SourceType AnnotationSource { get; }
    }

    public interface ILcmsMetabolomicsAnnotatorSettingModel : ILcmsAnnotatorSettingModel
    {
        MsRefSearchParameterBase SearchParameter { get; }

        List<ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> CreateAnnotator(MoleculeDataBase db, int priority, TargetOmics omics);
    }

    public interface ILcmsProteomicsAnnotatorSettingModel : ILcmsAnnotatorSettingModel
    {
        MsRefSearchParameterBase SearchParameter { get; }
        ProteomicsParameter ProteomicsParameter { get; }

        List<ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> CreateAnnotator(ShotgunProteomicsDB db, int priority, TargetOmics omics);
    }

    public class LcmsMspAnnotatorSettingModel : BindableBase, ILcmsMetabolomicsAnnotatorSettingModel
    {
        public LcmsMspAnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, int serialNumber) {
            DataBaseSettingModel = dataBaseSettingModel;
            AnnotatorID = $"{DataBaseSettingModel.DataBaseID}_{serialNumber}";
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
                new LcmsMspAnnotator(db, SearchParameter, omics, AnnotatorID, -1)
            };
        }
    }

    public class LcmsTextDBAnnotatorSettingModel : BindableBase, ILcmsMetabolomicsAnnotatorSettingModel
    {
        public LcmsTextDBAnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, int serialNumber) {
            DataBaseSettingModel = dataBaseSettingModel;
            AnnotatorID = $"{DataBaseSettingModel.DataBaseID}_{serialNumber}";
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
                new LcmsTextDBAnnotator(db, SearchParameter, AnnotatorID, -1)
            };
        }
    }

    public class LcmsProteomicsAnnotatorSettingModel : BindableBase, ILcmsProteomicsAnnotatorSettingModel
    {
        public LcmsProteomicsAnnotatorSettingModel(DataBaseSettingModel dataBaseSettingModel, int serialNumber) {
            DataBaseSettingModel = dataBaseSettingModel;
            AnnotatorID = $"{DataBaseSettingModel.DataBaseID}_{serialNumber}";
        }

        public DataBaseSettingModel DataBaseSettingModel { get; }

        public string AnnotatorID {
            get => annotatorID;
            set => SetProperty(ref annotatorID, value);
        }
        private string annotatorID = string.Empty;

        public SourceType AnnotationSource { get; } = SourceType.FastaDB;

        public MsRefSearchParameterBase SearchParameter { get; } = new MsRefSearchParameterBase();

        public ProteomicsParameter ProteomicsParameter { get; } = new ProteomicsParameter();

        public List<ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> CreateAnnotator(ShotgunProteomicsDB db, int priority, TargetOmics omics) {
            return new List<ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>>{
                new LcmsFastaAnnotator(db, SearchParameter, ProteomicsParameter, annotatorID, SourceType.FastaDB, -1),
            };
        }
    }

    public class LcmsAnnotatorSettingFactory
    {
        public ILcmsAnnotatorSettingModel Create(DataBaseSettingModel dataBaseSettingModel, int serialNumber) {
            switch (dataBaseSettingModel.DBSource) {
                case DataBaseSource.Msp:
                case DataBaseSource.Lbm:
                    return new LcmsMspAnnotatorSettingModel(dataBaseSettingModel, serialNumber);
                case DataBaseSource.Text:
                    return new LcmsTextDBAnnotatorSettingModel(dataBaseSettingModel, serialNumber);
                case DataBaseSource.Fasta:
                    return new LcmsProteomicsAnnotatorSettingModel(dataBaseSettingModel, serialNumber);
                default:
                    throw new NotSupportedException(nameof(dataBaseSettingModel.DBSource));
            }
        }
    }
}
