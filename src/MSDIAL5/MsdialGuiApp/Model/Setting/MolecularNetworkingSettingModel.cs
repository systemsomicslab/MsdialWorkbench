using CompMs.App.Msdial.Model.Core;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Setting {
    internal sealed class MolecularNetworkingSettingModel : DisposableModelBase {

        private readonly MolecularSpectrumNetworkingBaseParameter _parameter;
        private readonly IReadOnlyReactiveProperty<IAnalysisModel?> _currentFileModel;
        private readonly IReadOnlyReactiveProperty<IAlignmentModel?> _currentAlignmentModel;

        public MolecularNetworkingSettingModel(MolecularSpectrumNetworkingBaseParameter parameter, IReadOnlyReactiveProperty<IAnalysisModel?> currentFileModel, IReadOnlyReactiveProperty<IAlignmentModel?> currentAlignmentModel) {
            _currentFileModel = currentFileModel;
            _currentAlignmentModel = currentAlignmentModel;

            if (parameter.MaxEdgeNumberPerNode == 0) {
                parameter.MinimumPeakMatch = 3;
                parameter.MaxEdgeNumberPerNode = 6;
                parameter.MaxPrecursorDifference = 400;
            }
            _parameter = parameter;

            RtTolerance = _parameter.MnRtTolerance;
            IonCorrelationSimilarityCutOff = _parameter.MnIonCorrelationSimilarityCutOff;
            SpectrumSimilarityCutOff = _parameter.MnSpectrumSimilarityCutOff;
            RelativeAbundanceCutoff = _parameter.MnRelativeAbundanceCutOff;
            AbsluteAbundanceCutoff = _parameter.MnAbsoluteAbundanceCutOff;
            MassTolerance = _parameter.MnMassTolerance;
            IsExportIonCorrelation = _parameter.MnIsExportIonCorrelation;
            MinimumPeakMatch = _parameter.MinimumPeakMatch;
            MaxEdgeNumberPerNode = _parameter.MaxEdgeNumberPerNode;
            MaxPrecursorDifference = _parameter.MaxPrecursorDifference;
            MaxPrecursorDifferenceAsPercent = _parameter.MaxPrecursorDifferenceAsPercent;
            MsmsSimilarityCalc = _parameter.MsmsSimilarityCalc;
            ExportFolderPath = _parameter.ExportFolderPath;

            AvailableFileResult = currentFileModel.Select(m => m != null).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            AvailableAlignmentResult = currentAlignmentModel.Select(m => m != null).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            AvailableIonEdge = new[]
            {
                this.ObserveProperty(m => m.IsAlignSpotViewSelected),
                currentAlignmentModel.Select(m => (m?.AlignmentFile.CountRawFiles ?? 0) >= 6),
            }.CombineLatestValuesAreAllTrue()
            .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }
        public double RtTolerance {
            get => rtTolerance;
            set => SetProperty(ref rtTolerance, value);
        }
        private double rtTolerance;

        public double IonCorrelationSimilarityCutOff {
            get => ionCorrelationSimilarityCutOff;
            set => SetProperty(ref ionCorrelationSimilarityCutOff, value);
        }
        private double ionCorrelationSimilarityCutOff;

        public double SpectrumSimilarityCutOff {
            get => spectrumSimilarityCutOff;
            set => SetProperty(ref spectrumSimilarityCutOff, value);
        }
        private double spectrumSimilarityCutOff;

        public double RelativeAbundanceCutoff {
            get => relativeAbundanceCutoff;
            set => SetProperty(ref relativeAbundanceCutoff, value);
        }
        private double relativeAbundanceCutoff;

        public double AbsluteAbundanceCutoff {
            get => absoluteAbundanceCutoff;
            set => SetProperty(ref absoluteAbundanceCutoff, value);
        }
        private double absoluteAbundanceCutoff;

        public double MassTolerance
        {
            get => massTolerance;
            set => SetProperty(ref massTolerance, value);
        }
        private double massTolerance;

        public bool IsExportIonCorrelation {
            get => isExportIonCorrelation;
            set => SetProperty(ref isExportIonCorrelation, value);
        }
        private bool isExportIonCorrelation;

        public double MinimumPeakMatch {
            get => minimumPeakMatch;
            set => SetProperty(ref minimumPeakMatch, value);
        }
        private double minimumPeakMatch;

        public double MaxEdgeNumberPerNode {
            get => maxEdgeNumberPerNode;
            set => SetProperty(ref maxEdgeNumberPerNode, value);
        }
        private double maxEdgeNumberPerNode;

        public double MaxPrecursorDifference {
            get => maxPrecursorDifference;
            set => SetProperty(ref maxPrecursorDifference, value);
        }
        private double maxPrecursorDifference;

        public double MaxPrecursorDifferenceAsPercent {
            get => maxPrecursorDifferenceAsPercent;
            set => SetProperty(ref maxPrecursorDifferenceAsPercent, value);
        }
        private double maxPrecursorDifferenceAsPercent;

        public MsmsSimilarityCalc MsmsSimilarityCalc {
            get => msmsSimilarityCalc;
            set => SetProperty(ref msmsSimilarityCalc, value);
        }
        private MsmsSimilarityCalc msmsSimilarityCalc;

        public string ExportFolderPath {
            get => exportFolderPath;
            set => SetProperty(ref exportFolderPath, value);
        }
        private string exportFolderPath = string.Empty;

        public bool IsAlignSpotViewSelected {
            get => isAlignSpotViewSelected;
            set => SetProperty(ref isAlignSpotViewSelected, value);
        }
        private bool isAlignSpotViewSelected;

        public bool UseCurrentFiltering {
            get => _useCurrentFiltering;
            set => SetProperty(ref _useCurrentFiltering, value);
        }
        private bool _useCurrentFiltering;

        public ReadOnlyReactivePropertySlim<bool> AvailableFileResult { get; }
        public ReadOnlyReactivePropertySlim<bool> AvailableAlignmentResult { get; }
        public ReadOnlyReactivePropertySlim<bool> AvailableIonEdge { get; }

        public void Commit() {
            _parameter.MnRtTolerance = RtTolerance;
            _parameter.MnIonCorrelationSimilarityCutOff = IonCorrelationSimilarityCutOff;
            _parameter.MnSpectrumSimilarityCutOff = SpectrumSimilarityCutOff;
            _parameter.MnRelativeAbundanceCutOff = RelativeAbundanceCutoff;
            _parameter.MnAbsoluteAbundanceCutOff = AbsluteAbundanceCutoff;
            _parameter.MnMassTolerance = MassTolerance;
            _parameter.MnIsExportIonCorrelation = IsExportIonCorrelation;
            _parameter.MinimumPeakMatch = MinimumPeakMatch;
            _parameter.MaxEdgeNumberPerNode = MaxEdgeNumberPerNode;
            _parameter.MaxPrecursorDifference = MaxPrecursorDifference;
            _parameter.MaxPrecursorDifferenceAsPercent = MaxPrecursorDifferenceAsPercent;
            _parameter.MsmsSimilarityCalc = MsmsSimilarityCalc;
            _parameter.ExportFolderPath = ExportFolderPath;
        }

        public Task RunMolecularNetworkingAsync() {
            return Task.Run(() => {
                Commit();
                if (IsAlignSpotViewSelected) {
                    _currentAlignmentModel.Value?.ExportMoleculerNetworkingData(_parameter, UseCurrentFiltering);
                }
                else {
                    _currentFileModel.Value?.ExportMoleculerNetworkingData(_parameter, UseCurrentFiltering);
                }
            });
        }

        public Task SendMolecularNetworkingDataToCytoscapeJsAsync() {
            return Task.Run(() => {
                Commit();
                if (IsAlignSpotViewSelected) {
                    _currentAlignmentModel.Value?.InvokeMoleculerNetworking(_parameter, UseCurrentFiltering);
                }
                else {
                    _currentFileModel.Value?.InvokeMoleculerNetworking(_parameter, UseCurrentFiltering);
                }
            });
        }
    }
}
