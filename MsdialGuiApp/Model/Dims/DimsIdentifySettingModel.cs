using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Dims
{
    public sealed class DimsIdentifySettingModel : BindableBase
    {
        public DimsIdentifySettingModel(ParameterBase parameter) {
            annotatorFactory = new DimsAnnotatorSettingModelFactory();
            this.parameter = parameter;
        }

        private readonly DimsAnnotatorSettingModelFactory annotatorFactory;
        private readonly ParameterBase parameter;
        private int serialNumber = 1;

        public ObservableCollection<DataBaseSettingModel> DataBaseModels { get; } = new ObservableCollection<DataBaseSettingModel>();

        public ObservableCollection<IAnnotatorSettingModel> AnnotatorModels { get; } = new ObservableCollection<IAnnotatorSettingModel>();


        public DataBaseSettingModel DataBaseModel {
            get => dataBaseModel;
            set {
                if (SetProperty(ref dataBaseModel, value)){
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
            var db = new DataBaseSettingModel(parameter);
            DataBaseModels.Add(db);
            DataBaseModel = db;
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

        public void MoveUpAnnotator() {
            var annotatorModel = AnnotatorModel;
            if (!(annotatorModel is null)) {
                var index = AnnotatorModels.IndexOf(annotatorModel);
                if (index == 0) {
                    return;
                }
                AnnotatorModels.Move(index, index - 1);
            }
        }

        public void MoveDownAnnotator() {
            var annotatorModel = AnnotatorModel;
            if (!(annotatorModel is null)) {
                var index = AnnotatorModels.IndexOf(annotatorModel);
                if (index == AnnotatorModels.Count - 1) {
                    return;
                }
                AnnotatorModels.Move(index, index + 1);
            }
        }

        public void SetAnnotatorContainer(DataBaseStorage storage) {
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
    }
}
