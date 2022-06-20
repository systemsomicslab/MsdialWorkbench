using CompMs.App.Msdial.Model.Setting;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Core
{
    internal interface IDatasetModel : INotifyPropertyChanged
    {
        IMethodModel Method { get; }

        IMsdialDataStorage<ParameterBase> Storage { get; }

        MethodSettingModel AllProcessMethodSettingModel { get; }

        MethodSettingModel IdentificationProcessMethodSettingModel { get; }

        MethodSettingModel AlignmentProcessMethodSettingModel { get; }

        AnalysisFilePropertySetModel AnalysisFilePropertySetModel { get; }

        void AnalysisFilePropertyUpdate();

        Task SaveAsync();

        Task SaveAsAsync();

        Task LoadAsync();
    }
}