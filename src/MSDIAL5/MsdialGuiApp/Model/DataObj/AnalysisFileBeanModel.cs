using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class AnalysisFileBeanModel : BindableBase, IFileBean
    {
        private readonly AnalysisFileBean _file;

        public AnalysisFileBeanModel(AnalysisFileBean file) {
            _file = file;
        }

        public AnalysisFileBean File => _file;

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

        public string AnalysisFilePath {
            get => _file.AnalysisFilePath;
            set {
                if (_file.AnalysisFilePath != value) {
                    _file.AnalysisFilePath = value;
                    OnPropertyChanged(nameof(AnalysisFilePath));
                }
            }
        }

        public string AnalysisFileName {
            get => _file.AnalysisFileName;
            set {
                if (_file.AnalysisFileName != value) {
                    _file.AnalysisFileName = value;
                    OnPropertyChanged(nameof(AnalysisFileName));
                }
            }
        }

        public int AnalysisFileId {
            get => _file.AnalysisFileId;
            set {
                if (_file.AnalysisFileId != value) {
                    _file.AnalysisFileId = value;
                    OnPropertyChanged(nameof(AnalysisFileId));
                }
            }
        }

        public AcquisitionType AcquisitionType {
            get => _file.AcquisitionType;
            set {
                if (_file.AcquisitionType != value) {
                    _file.AcquisitionType = value;
                    OnPropertyChanged(nameof(AcquisitionType));
                }
            }
        }

        public string PeakAreaBeanInformationFilePath => _file.PeakAreaBeanInformationFilePath;
        public string DeconvolutionFilePath => _file.DeconvolutionFilePath;
        public string ProteinAssembledResultFilePath => _file.ProteinAssembledResultFilePath;

        int IFileBean.FileID => AnalysisFileId;
        string IFileBean.FileName => AnalysisFileName;
        string IFileBean.FilePath => AnalysisFilePath;
    }
}
