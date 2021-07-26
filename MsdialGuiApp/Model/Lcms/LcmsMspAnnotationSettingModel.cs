using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace CompMs.App.Msdial.Model.Lcms
{
    class LcmsMspAnnotationSettingModel : DataBaseAnnotationSettingModelBase
    {
        public LcmsMspAnnotationSettingModel() {

        }

        public LcmsMspAnnotationSettingModel(DataBaseAnnotationSettingModelBase other)
            : base(other) {
            
        }

        public override Annotator Build(ParameterBase parameter) {
            var molecules = LoadDataBase(parameter);
            return Build(parameter.ProjectParam, molecules);
        }

        public override Annotator Build(ProjectBaseParameter projectParameter, MoleculeDataBase molecules) {
            return new Annotator(
                new LcmsMspAnnotator(molecules.Database, Parameter, projectParameter.TargetOmics, AnnotatorID),
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
                default:
                    throw new NotSupportedException(Source.ToString());
            }
        }
    }
}
