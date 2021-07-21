using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace CompMs.App.Msdial.Model.Setting
{
    class MassAnnotationSettingModel : DataBaseAnnotationSettingModelBase
    {
        public MassAnnotationSettingModel() {

        }

        public MassAnnotationSettingModel(DataBaseAnnotationSettingModelBase other)
            : base(other) {
            
        }

        public override Annotator Build(ParameterBase parameter) {
            var molecules = LoadDataBase(parameter);
            return Build(parameter.ProjectParam, molecules);
        }

        public override Annotator Build(ProjectBaseParameter projectParameter, MoleculeDataBase molecules) {
            return new Annotator(
                new MassAnnotator(molecules.Database, Parameter, projectParameter.TargetOmics, Source, DataBaseID),
                Parameter);
        }

        public override MoleculeDataBase LoadDataBase(ParameterBase parameter) {
            var ext = Path.GetExtension(DataBasePath);
            switch (Source) {
                case SourceType.MspDB:
                    if (Regex.IsMatch(ext, @"\.msp\d*")) {
                        return new MoleculeDataBase(LibraryHandler.ReadMspLibrary(DataBasePath), DataBaseID);
                    }
                    else if (Regex.IsMatch(ext, @"\.lbm\d*")) {
                        return new MoleculeDataBase(LibraryHandler.ReadLipidMsLibrary(DataBasePath, parameter), DataBaseID);
                    }
                    throw new NotSupportedException(DataBasePath);
                case SourceType.TextDB:
                    var textdb = TextLibraryParser.TextLibraryReader(DataBasePath, out string error);
                    if (!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }
                    return new MoleculeDataBase(textdb, DataBaseID);
                default:
                    throw new NotSupportedException(Source.ToString());
            }
        }
    }
}
