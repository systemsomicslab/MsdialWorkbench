using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Setting
{
    sealed class MassAnnotationSettingModel : DataBaseAnnotationSettingModelBase, IAnnotationSettingModel {
        public MassAnnotationSettingModel(DataBaseAnnotationSettingModelBase other) : base(other) {
            
        }

        MoleculeDataBase? molecules;
        public ISerializableAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> Build(ParameterBase parameter) {
            if (molecules is null) {
                molecules = LoadDataBase(parameter);
            }
            return Build(parameter.ProjectParam, molecules);
        }

        private ISerializableAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> Build(ProjectBaseParameter projectParameter, MoleculeDataBase molecules) {
            return new DatabaseAnnotatorContainer(
                new MassAnnotator(molecules, Parameter, projectParameter.TargetOmics, AnnotationSource, DataBaseID, -1),
                molecules,
                Parameter);
        }

        private MoleculeDataBase LoadDataBase(ParameterBase parameter) {
            switch (DBSource) {
                case DataBaseSource.Msp:
                case DataBaseSource.Lbm:
                    return new MoleculeDataBase(LoadMspDataBase(DataBasePath, DBSource, parameter), DataBaseID, DataBaseSource.Lbm, SourceType.MspDB);
                case DataBaseSource.Text:
                    var textdb = TextLibraryParser.TextLibraryReader(DataBasePath, out string error);
                    if (!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }
                    return new MoleculeDataBase(textdb, DataBaseID, DataBaseSource.Text, SourceType.TextDB);
                default:
                    throw new NotSupportedException(DBSource.ToString());
            }
        }

        private static List<MoleculeMsReference> LoadMspDataBase(string path, DataBaseSource source, ParameterBase parameter) {
            List<MoleculeMsReference> db;
            switch (source) {
                case DataBaseSource.Msp:
                    db = LibraryHandler.ReadMspLibrary(path);
                    break;
                case DataBaseSource.Lbm:
                    db = LibraryHandler.ReadLipidMsLibrary(path, parameter);
                    break;
                default:
                    db = new List<MoleculeMsReference>(0);
                    break;
            }
            for (int i = 0; i < db.Count; i++) {
                db[i].ScanID = i;
            }
            return db;
        }
    }
}
