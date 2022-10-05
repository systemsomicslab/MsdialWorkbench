using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class AnalysisFileBeanModel : BindableBase, IFileBean
    {
        private readonly AnalysisFileBean _file;

        public AnalysisFileBeanModel(AnalysisFileBean file) {
            _file = file;
        }

        public AnalysisFileType AnalysisFileType {
            get => _file.AnalysisFileType;
            set {
                if (_file.AnalysisFileType != value) {
                    _file.AnalysisFileType = value;
                    OnPropertyChanged(nameof(AnalysisFileType));
                }
            }
        }
        public string AnalysisFileClass {
            get => _file.AnalysisFileClass;
            set {
                if (_file.AnalysisFileClass != value) {
                    _file.AnalysisFileClass = value;
                    OnPropertyChanged(nameof(AnalysisFileClass));
                }
            }
        }
        public int AnalysisFileAnalyticalOrder {
            get => _file.AnalysisFileAnalyticalOrder;
            set {
                if (_file.AnalysisFileAnalyticalOrder != value) {
                    _file.AnalysisFileAnalyticalOrder = value;
                    OnPropertyChanged(nameof(AnalysisFileAnalyticalOrder));
                }
            }
        }
        public bool AnalysisFileIncluded {
            get => _file.AnalysisFileIncluded;
            set {
                if (_file.AnalysisFileIncluded != value) {
                    _file.AnalysisFileIncluded = value;
                    OnPropertyChanged(nameof(AnalysisFileIncluded));
                }
            }
        }
        public int AnalysisBatch {
            get => _file.AnalysisBatch;
            set {
                if (_file.AnalysisBatch != value) {
                    _file.AnalysisBatch = value;
                    OnPropertyChanged(nameof(AnalysisBatch));
                }
            }
        }

        public double DilutionFactor {
            get => _file.DilutionFactor;
            set {
                if (_file.DilutionFactor != value) {
                    _file.DilutionFactor = value;
                    OnPropertyChanged(nameof(DilutionFactor));
                }
            }
        }

        public double ResponseVariable {
            get => _file.ResponseVariable;
            set {
                if (_file.ResponseVariable != value) {
                    _file.ResponseVariable = value;
                    OnPropertyChanged(nameof(ResponseVariable));
                }
            }
        }

        public string AnalysisFilePath => _file.AnalysisFilePath;
        public string AnalysisFileName => _file.AnalysisFileName;
        public int AnalysisFileId => _file.AnalysisFileId;

        int IFileBean.FileID => AnalysisFileId;
        string IFileBean.FileName => AnalysisFileName;
        string IFileBean.FilePath => AnalysisFilePath;
    }
}
