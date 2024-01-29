using CompMs.App.Msdial.Model.Setting;
using CompMs.MsdialCore.DataObj;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Core
{
    internal interface IProjectModel : INotifyPropertyChanged
    {
        IDatasetModel? CurrentDataset { get; set; }

        ObservableCollection<IDatasetModel> Datasets { get; }

        ProjectDataStorage Storage { get; }

        DatasetSettingModel DatasetSettingModel { get; }

        Task SaveAsync();

        Task SaveAsAsync();
    }
}