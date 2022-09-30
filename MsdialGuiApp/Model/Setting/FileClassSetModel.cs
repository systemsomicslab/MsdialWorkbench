using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Setting
{
    internal sealed class FileClassSetModel : BindableBase {
        private readonly ProjectBaseParameterModel _parameter;

        public FileClassSetModel(ProjectBaseParameterModel parameter) {
            FileClassProperties = new ObservableCollection<FileClassPropertyModel>();
            var c2o = parameter.ClassnameToOrder;
            var c2c = parameter.ClassnameToColorBytes;
            foreach (var className in Enumerable.Union(c2o.Keys, c2c.Keys)) {
                var property = new FileClassPropertyModel(
                    className,
                    c2c.TryGetValue(className, out var c) ? Color.FromArgb(c[0], c[1], c[2], c[3]) : Colors.Gray,
                    c2o.TryGetValue(className, out var o) ? o : 0);
                FileClassProperties.Add(property);
            }
            _parameter = parameter;
        }

        public ObservableCollection<FileClassPropertyModel> FileClassProperties { get; }

        public void Apply() {
            _parameter.SetClassOrderProperties(FileClassProperties.ToDictionary(property => property.Name, property => property.Order));
            _parameter.SetClassColorProperties(FileClassProperties.ToDictionary(property => property.Name, property => property.Color));
        }
    }
}
