using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Core
{
    internal interface IDatasetModel : INotifyPropertyChanged
    {
        IMethodModel? Method { get; }

        IMsdialDataStorage<ParameterBase> Storage { get; }

        AnalysisFileBeanModelCollection AnalysisFiles { get; }

        MethodSettingModel AllProcessMethodSettingModel { get; }

        MethodSettingModel IdentificationProcessMethodSettingModel { get; }

        MethodSettingModel AlignmentProcessMethodSettingModel { get; }

        AnalysisFilePropertyResetModel AnalysisFilePropertyResetModel { get; }
        FileClassSetModel FileClassSetModel { get; }

        Task SaveAsync();

        Task SaveAsAsync();

        Task LoadAsync();
        Task SaveParameterAsAsync();
    }
}