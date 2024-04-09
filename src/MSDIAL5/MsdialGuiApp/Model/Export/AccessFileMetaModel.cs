using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Export;
using System.Linq;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AccessFileMetaModel : BindableBase {
        private readonly FilePropertiesModel _fileProperties;

        public AccessFileMetaModel(FilePropertiesModel fileProperties)
        {
            _fileProperties = fileProperties;
        }

        public bool EnableMultiClass {
            get => _enableMultiClass;
            set => SetProperty(ref _enableMultiClass, value);
        }
        private bool _enableMultiClass = false;

        public int NumberOfClasses {
            get => _numberOfClasses;
            set => SetProperty(ref _numberOfClasses, value);
        }
        private int _numberOfClasses = 2;

        public void EstimateNumberOfClasses() {
            if (_fileProperties.ClassProperties.Count == 0) {
                return;
            }
            var nclass = _fileProperties.ClassProperties.Select(p => p.Name.Split('_').Length).DefaultIfEmpty().Max();
            if (nclass >= 2) {
                NumberOfClasses = nclass;
            }
            else {
                EnableMultiClass = false;
            }
        }

        public MulticlassFileMetaAccessor GetAccessor() {
            return new MulticlassFileMetaAccessor(EnableMultiClass ? NumberOfClasses : 0);
        }
    }
}
