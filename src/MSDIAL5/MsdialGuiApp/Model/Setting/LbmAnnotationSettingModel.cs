using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Query;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Setting
{
    public abstract class LbmAnnotationSettingModel : DataBaseAnnotationSettingModelBase, IAnnotationSettingModel {
        public LbmAnnotationSettingModel(DataBaseAnnotationSettingModelBase model, ParameterBase parameter) : base(model) {
            lipidQueryContainer = parameter.LipidQueryContainer;
            IonMode = parameter.IonMode;
        }

        public LipidQueryBean LipidQueryContainer {
            get => lipidQueryContainer;
            set => SetProperty(ref lipidQueryContainer, value);
        }
        private LipidQueryBean lipidQueryContainer;

        public IonMode IonMode {
            get => ionMode;
            set => SetProperty(ref ionMode, value);
        }
        private IonMode ionMode;

        private MoleculeDataBase? db;
        public ISerializableAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> Build(ParameterBase parameter) {
            if (db is null) {
                db = LoadDataBase(DataBaseID, DataBasePath, DBSource, parameter);
            }
            return BuildCore(parameter.ProjectParam, db);
        }

        protected abstract ISerializableAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> BuildCore(ProjectBaseParameter projectParameter, MoleculeDataBase molecules);

        private static MoleculeDataBase LoadDataBase(string id, string path, DataBaseSource dbsource, ParameterBase parameter) {
            switch (dbsource) {
                case DataBaseSource.Lbm:
                    return new MoleculeDataBase(LoadMspDataBase(path, parameter), id, DataBaseSource.Lbm, SourceType.MspDB);
                default:
                    throw new NotSupportedException(dbsource.ToString());
            }
        }

        protected static List<MoleculeMsReference> LoadMspDataBase(string path, ParameterBase parameter) {
            var db = LibraryHandler.ReadLipidMsLibrary(path, parameter);
            for (int i = 0; i < db.Count; ++i) {
                db[i].ScanID = i;
            }
            return db;
        }
    }
}
