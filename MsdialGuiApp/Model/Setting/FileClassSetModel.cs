using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Setting
{
    internal sealed class FileClassSetModel : DisposableModelBase {
        private readonly ProjectBaseParameterModel _parameter;

        public FileClassSetModel(ProjectBaseParameterModel parameter) {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));

            var c2o = parameter.ClassnameToOrder;
            var c2c = parameter.ClassnameToColorBytes;
            FileClassProperties = parameter.FileClasses.ToReadOnlyReactiveCollection(
                className => new FileClassPropertyModel(
                    className,
                    c2c.TryGetValue(className, out var c) ? Color.FromArgb(c[0], c[1], c[2], c[3]) : Colors.Gray,
                    c2o.TryGetValue(className, out var o) ? o : 0)).AddTo(Disposables);
        }

        public ReadOnlyObservableCollection<FileClassPropertyModel> FileClassProperties { get; }

        public void Commit() {
            _parameter.SetClassOrderProperties(FileClassProperties.ToDictionary(property => property.Name, property => property.Order));
            _parameter.SetClassColorProperties(FileClassProperties.ToDictionary(property => property.Name, property => property.Color));
        }
    }
}
