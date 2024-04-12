using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AccessFileMetaModel : DisposableModelBase {
        private readonly FilePropertiesModel _fileProperties;

        public AccessFileMetaModel(FilePropertiesModel fileProperties)
        {
            _fileProperties = fileProperties;
            EstimatedClasses = this.ObserveProperty(m => m.NumberOfClasses)
                .Select(nc => FileClasses.AsClasses(fileProperties.ClassProperties, nc))
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public bool EnableMultiClass {
            get => _enableMultiClass;
            set => SetProperty(ref _enableMultiClass, value);
        }
        private bool _enableMultiClass = false;

        public int NumberOfClasses {
            get => _numberOfClasses;
            set => SetProperty(ref _numberOfClasses, Math.Max(0, value));
        }
        private int _numberOfClasses = 2;

        public ReadOnlyReactivePropertySlim<FileClasses[]?> EstimatedClasses { get; }

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


        internal sealed class FileClasses {
            public string Name { get; set; } = string.Empty;

            public string[] Values { get; set; } = Array.Empty<string>();

            public string ReprValues => string.Join(", ", Values);

            public static FileClasses[] AsClasses(FileClassPropertiesModel fileClassPropertyModels, int nclasses) {
                var classses = fileClassPropertyModels.Select(p => p.Name.Split(new[] { '_' }, nclasses)).ToArray();
                var result = new FileClasses[nclasses];
                for (int i = 0; i < nclasses; i++) {
                    result[i] = new FileClasses
                    {
                        Name = $"Parameter{i + 1}",
                        Values = classses.Select(c => c.ElementAtOrDefault(i)).ToArray()
                    };
                }
                return result;
            }
        }
    }
}
