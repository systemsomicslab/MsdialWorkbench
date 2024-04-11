using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using System;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Setting
{
    internal sealed class FileClassSetModel : DisposableModelBase {
        public FileClassSetModel(FilePropertiesModel parameter) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            FileClassProperties = parameter.ClassProperties;
        }

        public ReadOnlyObservableCollection<FileClassPropertyModel> FileClassProperties { get; }
    }
}
