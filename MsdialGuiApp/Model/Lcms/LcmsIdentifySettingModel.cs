using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Proteomics.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace CompMs.App.Msdial.Model.Lcms
{
    public sealed class LcmsIdentifySettingModel : BindableBase
    {
        public LcmsIdentifySettingModel(ParameterBase parameter, DataBaseStorage dataBaseStorage = null) {
            annotatorFactory = new LcmsAnnotatorSettingFactory();
            this.parameter = parameter;

            if (!(dataBaseStorage is null)) {
                foreach (var dataBase in dataBaseStorage.MetabolomicsDataBases) {
                    var dbModel = new DataBaseSettingModel(this.parameter, dataBase.DataBase);
                    DataBaseModels.Add(dbModel);
                    foreach (var pair in dataBase.Pairs) {
                        AnnotatorModels.Add(annotatorFactory.Create(dbModel, pair.AnnotatorID, pair.SearchParameter));
                    }
                }
                foreach (var dataBase in dataBaseStorage.ProteomicsDataBases) {
                    var dbModel = new DataBaseSettingModel(this.parameter, dataBase.DataBase);
                    DataBaseModels.Add(dbModel);
                    foreach (var pair in dataBase.Pairs) {
                        AnnotatorModels.Add(annotatorFactory.Create(dbModel, pair.AnnotatorID, pair.SearchParameter));
                    }
                }
            }
        }

        private readonly LcmsAnnotatorSettingFactory annotatorFactory;
        private readonly ParameterBase parameter;
        private int serialNumber = 1;

        public ObservableCollection<DataBaseSettingModel> DataBaseModels { get; } = new ObservableCollection<DataBaseSettingModel>();

        public ObservableCollection<IAnnotatorSettingModel> AnnotatorModels { get; } = new ObservableCollection<IAnnotatorSettingModel>();

        public DataBaseSettingModel DataBaseModel {
            get => dataBaseModel;
            set {
                if (SetProperty(ref dataBaseModel, value)) {
                    if (AnnotatorModel?.DataBaseSettingModel != value) {
                        AnnotatorModel = AnnotatorModels.LastOrDefault(annotator => annotator.DataBaseSettingModel == value);
                    }
                }
            }
        }
        private DataBaseSettingModel dataBaseModel;

        public IAnnotatorSettingModel AnnotatorModel {
            get => annotatorModel;
            set {
                if (SetProperty(ref annotatorModel, value)) {
                    if (!(value is null) && DataBaseModel != value.DataBaseSettingModel) {
                        DataBaseModel = value.DataBaseSettingModel;
                    }
                }
            }
        }
        private IAnnotatorSettingModel annotatorModel;

        public void AddDataBase() {
            //Debug.WriteLine("1\t" + DataBaseModels.Count + "\t" + AnnotatorModels.Count);
            var db = new DataBaseSettingModel(parameter);
            DataBaseModels.Add(db);
            DataBaseModel = db;
            //Debug.WriteLine("2\t" + DataBaseModels.Count + "\t" + AnnotatorModels.Count);
        }

        public void RemoveDataBase() {
            var db = DataBaseModel;
            if (!(db is null)) {
                DataBaseModels.Remove(db);
                var removeAnnotators = AnnotatorModels.Where(annotator => annotator.DataBaseSettingModel == db).ToArray();
                foreach (var annotator in removeAnnotators) {
                    AnnotatorModels.Remove(annotator);
                }
                DataBaseModel = DataBaseModels.LastOrDefault();
            }
        }

        public void AddAnnotator() {
            //Debug.WriteLine("3\t" + DataBaseModels.Count + "\t" + AnnotatorModels.Count);
            var db = DataBaseModel;
            //if (db.DBSource == DataBaseSource.None) {
            //    Debug.WriteLine("3\t" + DataBaseModels.Count + "\t" + AnnotatorModels.Count);
            //    return;
            //}
            Debug.WriteLine("5\t" + db.DataBasePath + "\t" + db.DBSource);
            if (!(db is null)) {
                var annotatorModel = annotatorFactory.Create(db, $"{db.DataBaseID}_{serialNumber++}");
                AnnotatorModels.Add(annotatorModel);
                AnnotatorModel = annotatorModel;
            }
            //Debug.WriteLine("4\t" + DataBaseModels.Count + "\t" + AnnotatorModels.Count);
        }

        public void RemoveAnnotator() {
            var annotatorModel = AnnotatorModel;
            if (!(annotatorModel is null)) {
                AnnotatorModels.Remove(annotatorModel);
                AnnotatorModel = AnnotatorModels.LastOrDefault();
            }
        }

        public void MoveUpAnnotator() {
            var annotatorModel = AnnotatorModel;
            if (!(annotatorModel is null)) {
                var index = AnnotatorModels.IndexOf(annotatorModel);
                if (index == 0) {
                    return;
                }
                AnnotatorModels.Move(index, index-1);
            }
        }

        public void MoveDownAnnotator() {
            var annotatorModel = AnnotatorModel;
            if (!(annotatorModel is null)) {
                var index = AnnotatorModels.IndexOf(annotatorModel);
                if (index == AnnotatorModels.Count - 1) {
                    return;
                }
                AnnotatorModels.Move(index, index+1);
            }
        }

        public DataBaseStorage Create() {
            var result = DataBaseStorage.CreateEmpty();
            SetAnnotatorContainer(result);
            SetProteomicsAnnotatorContainer(result);
            return result;
        }

        private void SetAnnotatorContainer(DataBaseStorage storage) {
            foreach (var group in AnnotatorModels.OfType<IMetabolomicsAnnotatorSettingModel>().GroupBy(m => m.DataBaseSettingModel)) {
                var dbModel = group.Key;
                var db = dbModel.CreateMoleculeDataBase();
                if (db is null) {
                    continue;
                }
                var results = new List<IAnnotatorParameterPair<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>>();
                foreach (var annotatorModel in group) {
                    var index = AnnotatorModels.IndexOf(annotatorModel);
                    var annotators = annotatorModel.CreateAnnotator(db, AnnotatorModels.Count - index, parameter.TargetOmics);
                    results.AddRange(annotators.Select(annotator => new MetabolomicsAnnotatorParameterPair(annotator, annotatorModel.SearchParameter)));
                }
                storage.AddMoleculeDataBase(db, results);
            }
        }

        private void SetProteomicsAnnotatorContainer(DataBaseStorage storage) {
            foreach (var group in AnnotatorModels.OfType<IProteomicsAnnotatorSettingModel>().GroupBy(m => m.DataBaseSettingModel)) {
                var dbModel = group.Key;
                var db = dbModel.CreatePorteomicsDB();
                if (db is null) {
                    continue;
                }
                var results = new List<IAnnotatorParameterPair<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>>();
                foreach (var annotatorModel in group) {
                    var index = AnnotatorModels.IndexOf(annotatorModel);
                    var annotators = annotatorModel.CreateAnnotator(db, AnnotatorModels.Count - index, parameter.TargetOmics);
                    results.AddRange(annotators.Select(annotator => new ProteomicsAnnotatorParameterPair(annotator, annotatorModel.SearchParameter, db.ProteomicsParameter)));
                }
                storage.AddProteomicsDataBase(db, results);
            }
        }
    }
}
