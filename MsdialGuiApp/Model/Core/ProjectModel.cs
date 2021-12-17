using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Core
{
    public class ProjectModel : BindableBase, IProjectModel
    {
        public ObservableCollection<IDatasetModel> Datasets { get; }

        public IDatasetModel CurrentDataset {
            get => currentDataset;
            set => SetProperty(ref currentDataset, value);
        }
        private IDatasetModel currentDataset;

        public bool Start() {

            return true;
        }

        public void Close() {

        }

        public void Add() {
            Datasets.Add(new DatasetModel());
        }

        public void Change(IDatasetModel dataset) {
            if (Datasets.Contains(dataset)) {
                CurrentDataset = dataset;
            }
        }

        public void Reprocess(ProcessOption processOption) {
            var query = CurrentDataset?.CreateProcessQuery(processOption);

            query.Execute();
        }
    }
}
