using CompMs.App.Msdial.Model.Core;
using CompMs.CommonMVVM;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    public class DatasetSettingModel : BindableBase
    {
        private readonly Action<IDatasetModel> continuous;

        public DatasetSettingModel(Action<IDatasetModel> continuous) {
            this.continuous = continuous;
        }

        public bool IsComplete {
            get => isComplete;
            private set => SetProperty(ref isComplete, value);
        }
        private bool isComplete;

        public void Execute() {
            var dataset = new DatasetModel();

            continuous?.Invoke(dataset);
        }
    }
}
