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
                DataBaseModel = DataBaseModels.LastOrDefault();
                var removeAnnotators = AnnotatorModels.Where(annotator => annotator.DataBaseSettingModel == db).ToArray();
                foreach (var annotator in removeAnnotators) {
                    AnnotatorModels.Remove(annotator);
                }
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
            if (!(db is null) && db.DBSource != DataBaseSource.None) {
                var annotatorModel = annotatorFactory.Create(db, serialNumber++);
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

        public void SetAnnotatorContainer(DataBaseStorage storage) {
            foreach (var group in AnnotatorModels.OfType<ILcmsMetabolomicsAnnotatorSettingModel>().GroupBy(m => m.DataBaseSettingModel)) {
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

        public void SetProteomicsAnnotatorContainer(DataBaseStorage storage) {
            foreach (var group in AnnotatorModels.OfType<ILcmsProteomicsAnnotatorSettingModel>().GroupBy(m => m.DataBaseSettingModel)) {
                var dbModel = group.Key;
                var db = dbModel.CreatePorteomicsDB();
                if (db is null) {
                    continue;
                }
                var results = new List<IAnnotatorParameterPair<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>>();
                foreach (var annotatorModel in group) {
                    var index = AnnotatorModels.IndexOf(annotatorModel);
                    var annotators = annotatorModel.CreateAnnotator(db, AnnotatorModels.Count - index, parameter.TargetOmics);
                    results.AddRange(annotators.Select(annotator => new ProteomicsAnnotatorParameterPair(annotator, annotatorModel.SearchParameter, annotatorModel.ProteomicsParameter)));
                }
                storage.AddProteomicsDataBase(db, results);
            }
        }
    }
}
