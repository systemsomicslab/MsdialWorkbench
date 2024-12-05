using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class FilePropertiesModel : DisposableModelBase
    {
        private readonly ProjectBaseParameter _projectParameter;

        public FilePropertiesModel(ProjectBaseParameter projectParameter) {
            _projectParameter = projectParameter ?? throw new ArgumentNullException(nameof(projectParameter));

            if (_projectParameter.FileID_AnalysisFileType is null) {
                _projectParameter.FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>();
            }
            _fileIdToAnalysisFileType = _projectParameter.FileID_AnalysisFileType;

            if (_projectParameter.FileID_ClassName is null) {
                _projectParameter.FileID_ClassName = new Dictionary<int, string>();
            }
            _fileIdToClassName = _projectParameter.FileID_ClassName;

            if (_projectParameter.ClassnameToOrder is null) {
                _projectParameter.ClassnameToOrder = new Dictionary<string, int>();
            }
            _classnameToOrder = _projectParameter.ClassnameToOrder;

            if (_projectParameter.ClassnameToColorBytes is null) {
                _projectParameter.ClassnameToColorBytes = new Dictionary<string, List<byte>>();
            }
            _classnameToColorBytes = _projectParameter.ClassnameToColorBytes;

            var fileClasses = _fileIdToClassName.Values.Distinct();
            var properties = fileClasses.Select(
                (class_, i) => {
                    var color = _classnameToColorBytes.TryGetValue(class_, out var colorBytes)
                        ? Color.FromArgb(colorBytes[3], colorBytes[0], colorBytes[1], colorBytes[2])
                        : ChartBrushes.GetChartBrush(i).Color;
                    if (!_classnameToOrder.TryGetValue(class_, out var order))
                    {
                        order = i;
                    }
                    return new FileClassPropertyModel(class_, color, order);
                });
            _classProperties = new ObservableCollection<FileClassPropertyModel>(properties);
            ClassProperties = new FileClassPropertiesModel(_classProperties).AddTo(Disposables);

            _classProperties.ObserveRemoveChanged().Subscribe(property =>
            {
                _classnameToOrder.Remove(property.Name);
                _classnameToColorBytes.Remove(property.Name);
            });
            _classProperties.ObserveAddChanged().Subscribe(property =>
            {
                _classnameToOrder.Add(property.Name, property.Order);
                _classnameToColorBytes.Add(property.Name, new List<byte> { property.Color.R, property.Color.G, property.Color.B, property.Color.A, });
            });
            _classProperties.ObserveElementProperty(property => property.Order).Select(pack => pack.Instance)
                .Subscribe(property => _classnameToOrder[property.Name] = property.Order);
            _classProperties.ObserveElementProperty(property => property.Color).Select(pack => pack.Instance)
                .Subscribe(property => _classnameToColorBytes[property.Name] = new List<byte> { property.Color.R, property.Color.G, property.Color.B, property.Color.A, });
        }

        public ReadOnlyDictionary<int, AnalysisFileType> FileIdToAnalysisFileType => new ReadOnlyDictionary<int, AnalysisFileType>(_fileIdToAnalysisFileType);
        private readonly Dictionary<int, AnalysisFileType> _fileIdToAnalysisFileType;

        public ReadOnlyDictionary<int, string> FileIdToClassName => new ReadOnlyDictionary<int, string>(_fileIdToClassName);
        private readonly Dictionary<int, string> _fileIdToClassName;

        private readonly Dictionary<string, int> _classnameToOrder;
        private readonly Dictionary<string, List<byte>> _classnameToColorBytes;

        public FileClassPropertiesModel ClassProperties { get; }
        private readonly ObservableCollection<FileClassPropertyModel> _classProperties;

        public bool IsBoxPlotForAlignmentResult {
            get => _isBoxPlotForAlignmentResult;
            private set => SetProperty(ref _isBoxPlotForAlignmentResult, value);
        }
        private bool _isBoxPlotForAlignmentResult;

        public void SetFileDependentProperties(IReadOnlyCollection<AnalysisFileBean> files) {
            _fileIdToAnalysisFileType.Clear();
            foreach (var analysisfile in files) {
                _fileIdToAnalysisFileType[analysisfile.AnalysisFileId] = analysisfile.AnalysisFileType;
            }
            OnPropertyChanged(nameof(FileIdToAnalysisFileType));

            _fileIdToClassName.Clear();
            foreach (var analysisfile in files) {
                _fileIdToClassName[analysisfile.AnalysisFileId] = analysisfile.AnalysisFileClass;
            }
            OnPropertyChanged(nameof(FileIdToClassName));

            {
                var newClasses = files.Select(analysisFile => analysisFile.AnalysisFileClass).ToHashSet();
                foreach (var classProperty in _classProperties.Where(property => !newClasses.Contains(property.Name)).ToArray()) {
                    _classProperties.Remove(classProperty);
                }
                var order = _classProperties.Select(property => property.Order).DefaultIfEmpty().Max();
                var colors = _classProperties.Select(property => property.Color).ToHashSet();
                var classes = _classProperties.Select(property => property.Name).ToHashSet();
                var idx = 0;
                foreach (var fileClass in newClasses) {
                    if (!classes.Contains(fileClass)) {
                        while (idx < ChartBrushes.SolidColorBrushList.Count && colors.Contains(ChartBrushes.SolidColorBrushList[idx].Color)) {
                            idx++;
                        }
                        var color = idx < ChartBrushes.SolidColorBrushList.Count ? ChartBrushes.SolidColorBrushList[idx++].Color : Colors.Gray;
                        _classProperties.Add(new FileClassPropertyModel(fileClass, color, ++order));
                    }
                }
                OnPropertyChanged(nameof(ClassProperties));
            }

            var classNumAve = files.GroupBy(analysisfile => analysisfile.AnalysisFileType)
                                   .Average(group => group.Count());
            IsBoxPlotForAlignmentResult = classNumAve > 4;
        }

        public IBrushMapper<SpectrumComment> GetSpectrumBrush(Color defaultColor) {
            var commentToColor = _projectParameter.SpectrumCommentToColorBytes
                .Where(kvp => Enum.IsDefined(typeof(SpectrumComment), kvp.Key))
                .ToDictionary(kvp => (SpectrumComment)Enum.Parse(typeof(SpectrumComment), kvp.Key), kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2]));
            return new KeyBrushMapper<SpectrumComment, SpectrumComment>(
                commentToColor,
                comment => comment.HasFlag(SpectrumComment.doublebond) ? SpectrumComment.doublebond : comment,
                defaultColor);
        }
    }
}
