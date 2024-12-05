using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Setting
{
    public interface IIdentificationSettingModel {
        bool IsReadOnly { get; }
        DataBaseStorage Create(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer);
        void LoadParameter(ParameterBase parameter);
    }

    public class IdentifySettingModel : BindableBase, IIdentificationSettingModel
    {
        public IdentifySettingModel(ParameterBase parameter, IAnnotatorSettingModelFactory annotatorFactory, ProcessOption process, IMessageBroker broker, DataBaseStorage? dataBaseStorage = null) {
            this.parameter = parameter ?? throw new System.ArgumentNullException(nameof(parameter));
            this.annotatorFactory = annotatorFactory ?? throw new System.ArgumentNullException(nameof(annotatorFactory));
            _broker = broker;
            IsReadOnly = (process & ProcessOption.Identification) == 0;

            var databases = new List<DataBaseSettingModel>();
            var annotators = new List<(int Priority, IAnnotatorSettingModel Model)>();
            if (dataBaseStorage != null) {
                Restore(dataBaseStorage.MetabolomicsDataBases, databases, annotators, annotatorFactory, parameter);
                Restore(dataBaseStorage.ProteomicsDataBases, databases, annotators, annotatorFactory, parameter);
                Restore(dataBaseStorage.EadLipidomicsDatabases, databases, annotators, annotatorFactory, parameter);
                annotators.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
            DataBaseModels = new ObservableCollection<DataBaseSettingModel>(databases);
            AnnotatorModels = new ObservableCollection<IAnnotatorSettingModel>(annotators.Select(pair => pair.Model));
        }

        private static void Restore<TDataBase>(
            IEnumerable<DataBaseItem<TDataBase>> items,
            IList<DataBaseSettingModel> dataBaseModels,
            IList<(int, IAnnotatorSettingModel)> annotatorModels,
            IAnnotatorSettingModelFactory annotatorFactory,
            ParameterBase parameter)
            where TDataBase : IReferenceDataBase {

            foreach (var dataBase in items) {
                try {
                    var dbModel = new DataBaseSettingModel(parameter, dataBase.DataBase);
                    foreach (var pair in dataBase.Pairs) {
                        annotatorModels.Add((pair.AnnotationQueryFactory.Priority, annotatorFactory.Create(dbModel, pair.AnnotatorID, pair.AnnotationQueryFactory.PrepareParameter())));
                    }
                    dataBaseModels.Add(dbModel);
                }
                catch (NotSupportedException) {
                    // Skip if unsupported database
                }
            }
        }

        private readonly ParameterBase parameter;
        private readonly IAnnotatorSettingModelFactory annotatorFactory;
        private readonly IMessageBroker _broker;
        private int serialNumber = 1;

        public bool IsReadOnly { get; }

        public ObservableCollection<DataBaseSettingModel> DataBaseModels { get; }

        public DataBaseSettingModel? DataBaseModel {
            get => dataBaseModel;
            set {
                if (SetProperty(ref dataBaseModel, value)) {
                    if (AnnotatorModel?.DataBaseSettingModel != value) {
                        AnnotatorModel = AnnotatorModels.LastOrDefault(annotator => annotator.DataBaseSettingModel == value);
                    }
                }
            }
        }
        private DataBaseSettingModel? dataBaseModel;

        public ObservableCollection<IAnnotatorSettingModel> AnnotatorModels { get; }

        public IAnnotatorSettingModel? AnnotatorModel {
            get => annotatorModel;
            set {
                if (SetProperty(ref annotatorModel, value)) {
                    if (!(value is null) && DataBaseModel != value.DataBaseSettingModel) {
                        DataBaseModel = value.DataBaseSettingModel;
                    }
                }
            }
        }
        private IAnnotatorSettingModel? annotatorModel;

        private readonly object Lock = new object();
        private readonly object dbLock = new object();
        private readonly object annotatorLock = new object();

        public DataBaseSettingModel AddDataBase() {
            var db = new DataBaseSettingModel(parameter);
            DataBaseModels.Add(db);
            return db;
        }

        public void RemoveDataBase(DataBaseSettingModel db) {
            if (!(db is null)) {
                DataBaseModels.Remove(db);
                var removeAnnotators = AnnotatorModels.Where(annotator => annotator.DataBaseSettingModel == db).ToArray();
                foreach (var annotator in removeAnnotators) {
                    AnnotatorModels.Remove(annotator);
                }
            }
        }

        public IAnnotatorSettingModel? AddAnnotator(DataBaseSettingModel db) {
            if (db is not null) {
                if (db.DBSource == DataBaseSource.MsFinder) {
                    return null;
                }
                var annotatorModel = annotatorFactory.Create(db, $"{db.DataBaseID}_{serialNumber++}", null);
                lock (annotatorLock) {
                    AnnotatorModels.Add(annotatorModel);
                }
                return annotatorModel;
            }
            return null;
        }

        public void RemoveAnnotator(IAnnotatorSettingModel? annotator) {
            if (annotator is not null) {
                AnnotatorModels.Remove(annotator);
            }
        }

        public void MoveUpAnnotator(IAnnotatorSettingModel? annotator) {
            if (annotator is not null) {
                var index = AnnotatorModels.IndexOf(annotator);
                if (index <= 0 || index >= AnnotatorModels.Count) {
                    return;
                }
                AnnotatorModels.Move(index, index - 1);
            }
        }

        public void MoveDownAnnotator(IAnnotatorSettingModel? annotator) {
            if (annotator is not null) {
                var index = AnnotatorModels.IndexOf(annotator);
                if (index < 0 || index >= AnnotatorModels.Count - 1) {
                    return;
                }
                AnnotatorModels.Move(index, index + 1);
            }
        }

        public void AddLbmDatabase() {
            var db = new DataBaseSettingModel(parameter);
            if (db.TrySetLbmLibrary()) {
                DataBaseModels.Add(db);
            }
        }

        public bool IsCompleted {
            get => isCompleted;
            private set => SetProperty(ref isCompleted, value);
        }
        private bool isCompleted;

        public DataBaseStorage Create(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            var result = DataBaseStorage.CreateEmpty();
            if (IsReadOnly) {
                return result;
            }
            SetAnnotatorContainer(result, refer);
            SetProteomicsAnnotatorContainer(result, refer);
            SetEadLipidomicsAnnotatorContainer(result, refer);
            IsCompleted = true;
            return result;
        }

        private void SetAnnotatorContainer(DataBaseStorage storage, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            var request = new ProcessMessageRequest("Loading msp, lbm and text libraries...",
                async () =>
                {
                    var tasks = new List<Task>();
                    foreach (var group in AnnotatorModels.OfType<IMetabolomicsAnnotatorSettingModel>().GroupBy(m => m.DataBaseSettingModel)) {
                        var task = Task.Run(() =>
                        {
                            var dbModel = group.Key;
                            var db = dbModel.CreateMoleculeDataBase();
                            if (db is null) {
                                return;
                            }
                            var results = new List<IAnnotatorParameterPair<MoleculeDataBase>>();
                            foreach (var annotatorModel in group) {
                                var index = AnnotatorModels.IndexOf(annotatorModel);
                                var queryFactory = annotatorModel.CreateAnnotationQueryFactory(AnnotatorModels.Count - index, db, refer);
                                var key = annotatorModel.CreateRestorationKey(AnnotatorModels.Count - index);
                                results.Add(new MetabolomicsAnnotatorParameterPair(key, queryFactory));
                            }
                            storage.AddMoleculeDataBase(db, results);
                        });
                        tasks.Add(task);
                    }
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                });
            _broker.Publish(request);
        }

        private void SetProteomicsAnnotatorContainer(DataBaseStorage storage, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            var request = new ProcessMessageRequest("Loading fasta libraries...",
                async () =>
                {
                    var tasks = new List<Task>();
                    foreach (var group in AnnotatorModels.OfType<IProteomicsAnnotatorSettingModel>().GroupBy(m => m.DataBaseSettingModel)) {
                        var task = Task.Run(() =>
                        {
                            var dbModel = group.Key;
                            var db = dbModel.CreatePorteomicsDB();
                            if (db is null) {
                                return;
                            }
                            var results = new List<IAnnotatorParameterPair<ShotgunProteomicsDB>>();
                            foreach (var annotatorModel in group) {
                                var index = AnnotatorModels.IndexOf(annotatorModel);
                                var queryFactory = annotatorModel.CreateAnnotationQueryFactory(AnnotatorModels.Count - index, db, refer);
                                var key = annotatorModel.CreateRestorationKey(AnnotatorModels.Count - index);
                                results.Add(new ProteomicsAnnotatorParameterPair(key, queryFactory, db.ProteomicsParameter));
                            }
                            storage.AddProteomicsDataBase(db, results);
                        });
                        tasks.Add(task);
                    }
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                });
            _broker.Publish(request);
        }

        private void SetEadLipidomicsAnnotatorContainer(DataBaseStorage storage, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            var request = new ProcessMessageRequest("Building in silico lipid libraries...",
                async () =>
                {
                    var tasks = new List<Task>();
                    foreach (var group in AnnotatorModels.OfType<IEadLipidAnnotatorSettingModel>().GroupBy(m => m.DataBaseSettingModel)) {
                        var task = Task.Run(() =>
                        {
                            var dbModel = group.Key;
                            EadLipidDatabase? db = null;
                            switch (dbModel.DBSource) {
                                case DataBaseSource.OadLipid: db = dbModel.CreateOadLipidDatabase(); break;
                                case DataBaseSource.EieioLipid: db = dbModel.CreateEieioLipidDatabase(); break;
                                case DataBaseSource.EidLipid: db = dbModel.CreateEidLipidDatabase(); break;
                            }
                            if (db is null) {
                                return;
                            }
                            var results = new List<IAnnotatorParameterPair<EadLipidDatabase>>();
                            foreach (var annotatorModel in group) {
                                var index = AnnotatorModels.IndexOf(annotatorModel);
                                var queryFactory = annotatorModel.CreateAnnotationQueryFactory(AnnotatorModels.Count - index, db, refer);
                                var key = annotatorModel.CreateRestorationKey(AnnotatorModels.Count - index);
                                results.Add(new EadLipidAnnotatorParameterPair(key, queryFactory));
                            }
                            storage.AddEadLipidomicsDataBase(db, results);
                        });
                        tasks.Add(task);
                    }
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                });
            _broker.Publish(request);
        }

        void IIdentificationSettingModel.LoadParameter(ParameterBase parameter) {

        }
    }
}