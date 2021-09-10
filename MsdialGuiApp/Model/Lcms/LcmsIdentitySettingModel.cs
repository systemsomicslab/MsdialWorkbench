using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Proteomics.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcMsApi.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Lcms
{
    public class LcmsIdentitySettingModel : BindableBase
    {
        public LcmsIdentitySettingModel(ParameterBase parameter) {
            annotatorFactory = new LcmsAnnotatorSettingFactory();
            this.parameter = parameter;
        }

        private readonly LcmsAnnotatorSettingFactory annotatorFactory;
        private readonly ParameterBase parameter;
        private int serialNumber = 1;

        public ObservableCollection<DataBaseSettingModel> DataBaseModels { get; } = new ObservableCollection<DataBaseSettingModel>();

        public ObservableCollection<ILcmsAnnotatorSettingModel> AnnotatorModels { get; } = new ObservableCollection<ILcmsAnnotatorSettingModel>();

        public DataBaseSettingModel DataBaseModel {
            get => dataBaseModel;
            set => SetProperty(ref dataBaseModel, value);
        }
        private DataBaseSettingModel dataBaseModel;

        public ILcmsAnnotatorSettingModel AnnotatorModel {
            get => annotatorModel;
            set => SetProperty(ref annotatorModel, value);
        }
        private ILcmsAnnotatorSettingModel annotatorModel;

        public void AddDataBase() {
            var db = new DataBaseSettingModel(parameter);
            DataBaseModels.Add(db);
            DataBaseModel = db;
        }

        public void RemoveDataBase() {
            var db = DataBaseModel;
            if (!(db is null)) {
                DataBaseModels.Remove(db);
                DataBaseModel = DataBaseModels.LastOrDefault();
            }
        }

        public void AddAnnotator() {
            var db = DataBaseModel;
            if (!(db is null)) {
                var annotatorModel = annotatorFactory.Create(db, serialNumber++);
                AnnotatorModels.Add(annotatorModel);
                AnnotatorModel = annotatorModel;
            }
        }

        public void RemoveAnnotator() {
            var annotatorModel = AnnotatorModel;
            if (!(annotatorModel is null)) {
                AnnotatorModels.Remove(annotatorModel);
                AnnotatorModel = AnnotatorModels.LastOrDefault();
            }
        }

        public void SetAnnotatorContainer(DataBaseStorage storage) {
            foreach (var group in AnnotatorModels.OfType<ILcmsMetabolomicsAnnotatorSettingModel>().GroupBy(m => m.DataBaseSettingModel)) {
                var dbModel = group.Key;
                var db = dbModel.CreateMoleculeDataBase();
                if (db is null) {
                    continue;
                }
                var results = new List<IAnnotatorParameterPair<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>>();
                foreach (var annotatorModel in group) {
                    var annotators = annotatorModel.CreateAnnotator(db, parameter.TargetOmics);
                    results.AddRange(annotators.Select(annotator => new MetabolomicsAnnotatorParameterPair(annotator, annotatorModel.SearchParameter)));
                }
                storage.AddMoleculeDataBase(db, results);
            }
        }

        public void SetProteomicsAnnotatorContainer(DataBaseStorage storage) {
            foreach (var group in AnnotatorModels.OfType<ILcmsProteomicsAnnotatorSettingModel>().GroupBy(m => m.DataBaseSettingModel)) {
                var dbModel = group.Key;
                var db = dbModel.CreatePorteomicsDB();
                if (db is null) {
                    continue;
                }
                var results = new List<IAnnotatorParameterPair<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>>();
                foreach (var annotatorModel in group) {
                    var annotators = annotatorModel.CreateAnnotator(db, parameter.TargetOmics);
                    results.AddRange(annotators.Select(annotator => new ProteomicsAnnotatorParameterPair(annotator, annotatorModel.SearchParameter, annotatorModel.ProteomicsParameter)));
                }
                storage.AddProteomicsDataBase(db, results);
            }
        }
    }
}
