using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel
{
    public class CompoundSearchVM<T> : ViewModelBase where T : IMSProperty, IMoleculeProperty, IIonProperty
    {
        public ObservableCollection<CompoundResult> Compounds {
            get => compounds;
            set {
                var old = compounds;
                if (SetProperty(ref compounds, value)) {
                    if (compoundsView != null) {
                        compoundsView.CurrentChanged -= RaiseExecuteChange;
                    }
                    compoundsView = CollectionViewSource.GetDefaultView(compounds);
                    if (compoundsView != null) {
                        compoundsView.MoveCurrentToFirst();
                        compoundsView.CurrentChanged += RaiseExecuteChange;
                    }
                }
            }
        }
        private ObservableCollection<CompoundResult> compounds = new ObservableCollection<CompoundResult>();
        private ICollectionView compoundsView;

        public List<SpectrumPeakWrapper> Ms2DecSpectrum {
            get => ms2DecSpectrum;
            set => SetProperty(ref ms2DecSpectrum, value);
        }
        private List<SpectrumPeakWrapper> ms2DecSpectrum = new List<SpectrumPeakWrapper>();

        public MsRefSearchParameterVM ParameterVM {
            get => parameterVM;
            set {
                var oldValue = parameterVM;
                var newValue = value;
                if (SetProperty(ref parameterVM, value)) {
                    if (oldValue != null) {
                        oldValue.PropertyChanged -= OnParameterChanged;
                    }
                    if (newValue != null) {
                        newValue.PropertyChanged += OnParameterChanged;
                    }
                }
            }
        }
        private MsRefSearchParameterVM parameterVM;

        public int FileID { get; }
        public string FileName { get; }
        public double AccurateMass { get; }
        public string AdductName { get; }
        public string MetaboliteName { get; }

        private readonly MSDecResult msdecResult;
        private readonly T property;
        private readonly IAnnotator<T, MSDecResult> Annotator;
        private readonly IReadOnlyList<IsotopicPeak> isotopes;

        public CompoundSearchVM(
            AnalysisFileBean analysisFile,
            T peakFeature, MSDecResult msdecResult,
            IReadOnlyList<IsotopicPeak> isotopes,
            IAnnotator<T, MSDecResult> annotator,
            MsRefSearchParameterBase parameter = null) {

            this.msdecResult = msdecResult;
            this.isotopes = isotopes;
            Annotator = annotator;
            ParameterVM = new MsRefSearchParameterVM(parameter != null ? new MsRefSearchParameterBase(parameter) : new MsRefSearchParameterBase());

            FileID = analysisFile.AnalysisFileId;
            FileName = analysisFile.AnalysisFileName;
            AccurateMass = peakFeature.PrecursorMz;
            AdductName = peakFeature.AdductType.AdductIonName;
            MetaboliteName = peakFeature.Name;
            property = peakFeature;

            Ms2DecSpectrum = msdecResult.Spectrum.Select(spec => new SpectrumPeakWrapper(spec)).ToList();
            Search();
        }

        public CompoundSearchVM(
            AlignmentFileBean alignmentFile,
            T spot, MSDecResult msdecResult,
            IReadOnlyList<IsotopicPeak> isotopes,
            IAnnotator<T, MSDecResult> annotator,
            MsRefSearchParameterBase parameter = null) {

            this.msdecResult = msdecResult;
            this.isotopes = isotopes;
            Annotator = annotator;
            ParameterVM = new MsRefSearchParameterVM(parameter != null ? new MsRefSearchParameterBase(parameter) : new MsRefSearchParameterBase());

            FileID = alignmentFile.FileID;
            FileName = alignmentFile.FileName;
            AccurateMass = spot.PrecursorMz;
            AdductName = spot.AdductType.AdductIonName;
            MetaboliteName = spot.Name;
            property = spot;

            Ms2DecSpectrum = msdecResult.Spectrum.Select(spec => new SpectrumPeakWrapper(spec)).ToList();
            Search();
        }

        public DelegateCommand SearchCommand => searchCommand ?? (searchCommand = new DelegateCommand(Search, CanSearch));
        private DelegateCommand searchCommand; 

        private bool canSearch = false;
        private static readonly double EPS = 1e-10;
        private bool CanSearch() {
            if (ParameterVM.Ms1Tolerance <= EPS || ParameterVM.Ms2Tolerance <= EPS)
                return false;
            return canSearch;
        }

        private CancellationTokenSource cts = null;

        private void Search() {
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            canSearch = false;
            if (cts != null) {
                cts.Cancel();
            }

            var candidates = Annotator.FindCandidates(property, msdecResult, isotopes, ParameterVM.innerModel);
            foreach (var candidate in candidates) {
                candidate.IsManuallyModified = true;
                candidate.Source |= SourceType.Manual;
            }
            Compounds = new ObservableCollection<CompoundResult>(
                candidates.OrderByDescending(result => result.TotalScore)
                    .Select(result => new CompoundResult(Annotator.Refer(result), result)));

            canSearch = true;
            System.Windows.Input.Mouse.OverrideCursor = null;
        }

        private async void OnParameterChanged(object sender, PropertyChangedEventArgs e) {
            if (!CanSearch()) {
                return;
            }

            if (cts != null) {
                cts.Cancel();
            }

            var localCts = new CancellationTokenSource();
            cts = localCts;

            await SearchAsync(cts.Token).ContinueWith(
                t => {
                    localCts.Dispose();
                    if (cts == localCts)
                        cts = null;
                });
        }

        private async Task SearchAsync(CancellationToken token) {
            if (!canSearch)
                return;

            var compounds = await Task.Run(() => {
                var candidates = Annotator.FindCandidates(property, msdecResult, isotopes, ParameterVM.innerModel);
                foreach (var candidate in candidates) {
                    candidate.IsManuallyModified = true;
                    candidate.Source |= SourceType.Manual;
                }
                token.ThrowIfCancellationRequested();

                return new ObservableCollection<CompoundResult>(
                    candidates
                        .OrderByDescending(result => result.TotalScore)
                        .Select(result => new CompoundResult(Annotator.Refer(result), result)));
            }, token);

            token.ThrowIfCancellationRequested();

            Compounds = compounds;
        }

        public DelegateCommand SetConfidenceCommand => setConfidenceCommand ?? (setConfidenceCommand = new DelegateCommand(SetConfidence, CanSetAnnotation));
        private DelegateCommand setConfidenceCommand;

        private void SetConfidence() {
            var compound = (CompoundResult)compoundsView.CurrentItem;
            var reference = compound.msReference;
            var result = compound.matchResult;
            DataAccess.SetMoleculeMsPropertyAsConfidence(property, reference, result);
            if (property is IAnnotatedObject obj) {
                obj.MatchResults.RemoveManuallyResults();
                obj.MatchResults.AddResult(result);
            }
        }

        public DelegateCommand SetUnsettledCommand => setUnsettledCommand ?? (setUnsettledCommand = new DelegateCommand(SetUnsettled, CanSetAnnotation));
        private DelegateCommand setUnsettledCommand;

        private void SetUnsettled() {
            var compound = (CompoundResult)compoundsView.CurrentItem;
            var reference = compound.msReference;
            var result = compound.matchResult;
            DataAccess.SetMoleculeMsPropertyAsUnsettled(property, reference, result);
            if (property is IAnnotatedObject obj) {
                obj.MatchResults.RemoveManuallyResults();
                obj.MatchResults.AddResult(result);
            }
        }

        private bool CanSetAnnotation() {
            return (CompoundResult)compoundsView.CurrentItem != null;
        }

        private void RaiseExecuteChange(object sender, EventArgs e) {
            SetConfidenceCommand.RaiseCanExecuteChanged();
            SetUnsettledCommand.RaiseCanExecuteChanged();
        }

        public DelegateCommand SetUnknownCommand => setUnknownCommand ?? (setUnknownCommand = new DelegateCommand(SetUnknown));
        private DelegateCommand setUnknownCommand;

        private void SetUnknown() {
            DataAccess.ClearMoleculePropertyInfomation(property);
            if (property is IAnnotatedObject obj) {
                obj.MatchResults.RemoveManuallyResults();
                obj.MatchResults.AddResult(new MsScanMatchResult { Source = SourceType.Manual | SourceType.Unknown });
            }
        }
    }
}

namespace CompMs.App.Msdial.ViewModel
{
    public class CompoundResult
    {
        public int LibraryID => matchResult.LibraryID;
        public string Name => msReference.Name;
        public string AdductName => msReference.AdductType.AdductIonName;
        public double PrecursorMz => msReference.PrecursorMz;
        public string Instrument => msReference.InstrumentModel;
        public string Comment => msReference.Comment;
        public double WeightedDotProduct => matchResult.WeightedDotProduct;
        public double SimpleDotProduct => matchResult.SimpleDotProduct;
        public double ReverseDotProduct => matchResult.ReverseDotProduct;
        public double MassSimilarity => matchResult.AcurateMassSimilarity;
        public double Presence => matchResult.MatchedPeaksPercentage;
        public double TotalScore => matchResult.TotalScore;
        public List<SpectrumPeakWrapper> Spectrum => spectrum ?? (spectrum = msReference.Spectrum.Select(spec => new SpectrumPeakWrapper(spec)).ToList());
        private List<SpectrumPeakWrapper> spectrum = null;

        internal readonly MoleculeMsReference msReference;
        internal readonly MsScanMatchResult matchResult;
        public CompoundResult(MoleculeMsReference msReference, MsScanMatchResult matchResult) {
            this.msReference = msReference;
            this.matchResult = matchResult;
        }
    }
}

namespace CompMs.App.Msdial.ViewModel
{
    public class MsRefSearchParameterVM : ViewModelBase
    {
        public float Ms1Tolerance {
            get => innerModel.Ms1Tolerance;
            set {
                if (innerModel.Ms1Tolerance != value) {
                    innerModel.Ms1Tolerance = value;
                    OnPropertyChanged(nameof(Ms1Tolerance));
                }
            }
        }

        public float Ms2Tolerance {
            get => innerModel.Ms2Tolerance;
            set {
                if (innerModel.Ms2Tolerance != value) {
                    innerModel.Ms2Tolerance = value;
                    OnPropertyChanged(nameof(Ms2Tolerance));
                }
            }
        }

        internal readonly MsRefSearchParameterBase innerModel;

        public MsRefSearchParameterVM(MsRefSearchParameterBase innerModel) {
            this.innerModel = innerModel;
        }
    }
}
