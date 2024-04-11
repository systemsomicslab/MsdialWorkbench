using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    internal class AnalysisFilePropertyResetModel : BindableBase
    {
        private readonly FilePropertiesModel _projectParameter;

        public AnalysisFilePropertyResetModel(AnalysisFileBeanModelCollection fileCollection, FilePropertiesModel projectParameter) {
            AnalysisFileModelCollection = fileCollection;
            _projectParameter = projectParameter;
        }

        public AnalysisFileBeanModelCollection AnalysisFileModelCollection { get; }

        public void Update() {
            _projectParameter.SetFileDependentProperties(AnalysisFileModelCollection.AnalysisFiles.Select(file => file.File).ToList());
        }
    }
}
