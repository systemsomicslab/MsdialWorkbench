using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.View.Lcms;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    public class AlignmentLcmsVM : TempAlignmentFileVM
    {
        public ICollectionView Ms1Spots {
            get => ms1Spots;
            set {
                var old = ms1Spots;
                if (SetProperty(ref ms1Spots, value)) {
                    if (old != null) old.Filter -= PeakFilter;
                    if (ms1Spots != null) ms1Spots.Filter += PeakFilter;
                }
            }
        }
        private ICollectionView ms1Spots;
        private ObservableCollection<AlignmentSpotPropertyModel> _ms1Spots = new ObservableCollection<AlignmentSpotPropertyModel>();

        public override ICollectionView PeakSpots => ms1Spots;

        public double MassMin => _ms1Spots.Min(spot => spot.MassCenter);
        public double MassMax => _ms1Spots.Max(spot => spot.MassCenter);
        public double MassLower {
            get => massLower;
            set {
                if (SetProperty(ref massLower, value))
                    Ms1Spots?.Refresh();
            }
        }
        public double MassUpper {
            get => massUpper;
            set {
                if (SetProperty(ref massUpper, value))
                    Ms1Spots?.Refresh();
            }
        }
        private double massLower, massUpper;

        public double RtMin => _ms1Spots.Min(spot => spot.TimesCenter);
        public double RtMax => _ms1Spots.Max(spot => spot.TimesCenter);
        public double RtLower {
            get => rtLower;
            set {
                if (SetProperty(ref rtLower, value))
                    Ms1Spots?.Refresh();
            }
        }
        public double RtUpper {
            get => rtUpper;
            set {
                if (SetProperty(ref rtUpper, value))
                    Ms1Spots?.Refresh();
            }
        }
        private double rtLower, rtUpper;


        public List<BarItem> BarItems {
            get => barItems;
            set => SetProperty(ref barItems, value);
        }
        private List<BarItem> barItems = new List<BarItem>();
        
        public AlignmentResultContainer Container {
            get => container;
            set => SetProperty(ref container, value);
        }
        private AlignmentResultContainer container;

        public AlignmentSpotPropertyModel Target {
            get => target;
            set {
                if (SetProperty(ref target, value)) {
                    OnTargetChanged();
                }
            }
        }
        private AlignmentSpotPropertyModel target;

        public List<Chromatogram> EicChromatograms {
            get => eicChromatograms;
            set {
                if (SetProperty(ref eicChromatograms, value)) {
                    OnPropertyChanged(nameof(EicMax));
                    OnPropertyChanged(nameof(EicMin));
                    OnPropertyChanged(nameof(IntensityMax));
                    OnPropertyChanged(nameof(IntensityMin));
                }
            }
        }
        private List<Chromatogram> eicChromatograms;

       

        public double EicMax => EicChromatograms?.SelectMany(chrom => chrom.Peaks).DefaultIfEmpty().Max(peak => peak?.Time) ?? 0;
        public double EicMin => EicChromatograms?.SelectMany(chrom => chrom.Peaks).DefaultIfEmpty().Min(peak => peak?.Time) ?? 0;
        public double IntensityMax => EicChromatograms?.SelectMany(chrom => chrom.Peaks).DefaultIfEmpty().Max(peak => peak?.Intensity) ?? 0;
        public double IntensityMin => EicChromatograms?.SelectMany(chrom => chrom.Peaks).DefaultIfEmpty().Min(peak => peak?.Intensity) ?? 0;

        public List<SpectrumPeakWrapper> Ms2Spectrum {
            get => ms2Spectrum;
            set {
                if (SetProperty(ref ms2Spectrum, value)) {
                    OnPropertyChanged(nameof(Ms2MassMin));
                    OnPropertyChanged(nameof(Ms2MassMax));
                }
            }
        }
        private List<SpectrumPeakWrapper> ms2Spectrum = new List<SpectrumPeakWrapper>();

        public List<SpectrumPeakWrapper> Ms2ReferenceSpectrum {
            get => ms2ReferenceSpectrum;
            set {
                if (SetProperty(ref ms2ReferenceSpectrum, value)) {
                    OnPropertyChanged(nameof(Ms2MassMin));
                    OnPropertyChanged(nameof(Ms2MassMax));
                }
            }
        }
        private List<SpectrumPeakWrapper> ms2ReferenceSpectrum = new List<SpectrumPeakWrapper>();

        public double Ms2MassMin => Ms2Spectrum.Concat(Ms2ReferenceSpectrum).DefaultIfEmpty().Min(peak => peak?.Mass) ?? 0;
        public double Ms2MassMax => Ms2Spectrum.Concat(Ms2ReferenceSpectrum).DefaultIfEmpty().Max(peak => peak?.Mass) ?? 0;
        
        
        public string FileName {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        private string fileName; 

        public bool RefMatchedChecked => ReadDisplayFilters(DisplayFilter.RefMatched);
        public bool SuggestedChecked => ReadDisplayFilters(DisplayFilter.Suggested);
        public bool UnknownChecked => ReadDisplayFilters(DisplayFilter.Unknown);
        public bool Ms2AcquiredChecked => ReadDisplayFilters(DisplayFilter.Ms2Acquired);
        public bool MolecularIonChecked => ReadDisplayFilters(DisplayFilter.MolecularIon);
        public bool BlankFilterChecked => ReadDisplayFilters(DisplayFilter.Blank);
        public DisplayFilter DisplayFilters {
            get => displayFilters;
            internal set {
                if (SetProperty(ref displayFilters, value))
                    Ms1Spots?.Refresh();
            }
        }
        
        private DisplayFilter displayFilters = 0;
        private bool ReadDisplayFilters(DisplayFilter flags) {
            return (flags & DisplayFilters) != 0;
        }

        private readonly AlignmentFileBean alignmentFile;
        private readonly List<long> seekPointers = new List<long>();
        private readonly ParameterBase param = null;
        private readonly string resultFile = string.Empty;
        private readonly string eicFile = string.Empty;
        private readonly string spectraFile = string.Empty;
        private readonly List<MoleculeMsReference> msp = new List<MoleculeMsReference>();

        private MSDecResult msdecResult = null;
        private static ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;

        static AlignmentLcmsVM() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");
        }

        public AlignmentLcmsVM(AlignmentFileBean alignmentFileBean, ParameterBase param) {
            alignmentFile = alignmentFileBean;
            fileName = alignmentFileBean.FileName;
            resultFile = alignmentFileBean.FilePath;
            eicFile = alignmentFileBean.EicFilePath;
            spectraFile = alignmentFileBean.SpectraFilePath;

            this.param = param;
            Container = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(resultFile);

            _ms1Spots = new ObservableCollection<AlignmentSpotPropertyModel>(Container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyModel(prop)));
            Ms1Spots = CollectionViewSource.GetDefaultView(_ms1Spots);

            MassLower = MassMin;
            MassUpper = MassMax;

            RtLower = RtMin;
            RtUpper = RtMax;

            MsdecResultsReader.GetSeekPointers(spectraFile, out _, out seekPointers, out _);

            PropertyChanged += OnTargetChanged;
        }

        private async void OnTargetChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(Target)) {
                await OnTargetChanged(Target).ConfigureAwait(false);
            }
        }

        private async Task OnTargetChanged(AlignmentSpotPropertyModel target) {
            await Task.WhenAll(
                LoadBarItemsAsync(target),
                LoadEicAsync(target),
                LoadMs2SpectrumAsync(target),
                LoadMs2ReferenceAsync(target)
           ).ConfigureAwait(false);
        }

        async Task LoadBarItemsAsync(AlignmentSpotPropertyModel target) {
            BarItems = new List<BarItem>();
            if (target == null)
                return;

            // TODO: Implement other features (PeakHeight, PeakArea, Normalized PeakHeight, Normalized PeakArea)
            BarItems = await Task.Run(() =>
                target.AlignedPeakProperties
                .GroupBy(peak => param.FileID_ClassName[peak.FileID])
                .Select(pair => new BarItem { Class = pair.Key, Height = pair.Average(peak => peak.PeakHeightTop) })
                .ToList()).ConfigureAwait(false);
        }

        async Task LoadEicAsync(AlignmentSpotPropertyModel target) {
            EicChromatograms = new List<Chromatogram>();
            if (target == null)
                return;

            // maybe using file pointer is better
            EicChromatograms = await Task.Run(() => {
                var spotinfo = chromatogramSpotSerializer.DeserializeAtFromFile(eicFile, target.MasterAlignmentID);
                var chroms = new List<Chromatogram>(spotinfo.PeakInfos.Count);
                foreach (var peakinfo in spotinfo.PeakInfos) {
                    var items = peakinfo.Chromatogram.Select(chrom => new PeakItem(chrom)).ToList();
                    var peakitems = items.Where(item => peakinfo.ChromXsLeft.Value <= item.Time && item.Time <= peakinfo.ChromXsRight.Value).ToList();
                    chroms.Add(new Chromatogram {
                        Class = param.FileID_ClassName[peakinfo.FileID],
                        Peaks = items,
                        PeakArea = peakitems,
                    });
                }
                return chroms;
            }).ConfigureAwait(false);
        }

        async Task LoadMs2SpectrumAsync(AlignmentSpotPropertyModel target) {
            Ms2Spectrum = new List<SpectrumPeakWrapper>();
            if (target == null)
                return;

            await Task.Run(() => {
                var idx = _ms1Spots.IndexOf(target);
                msdecResult = MsdecResultsReader.ReadMSDecResult(spectraFile, seekPointers[idx]);
                Ms2Spectrum = msdecResult.Spectrum.Select(spec => new SpectrumPeakWrapper(spec)).ToList();
            }).ConfigureAwait(false);
        }

        async Task LoadMs2ReferenceAsync(AlignmentSpotPropertyModel target) {
            Ms2ReferenceSpectrum = new List<SpectrumPeakWrapper>();
            if (target == null)
                return;

            await Task.Run(() => {
                if (target.TextDbBasedMatchResult == null && target.MspBasedMatchResult is MsScanMatchResult matched) {
                    var reference = msp[matched.LibraryIDWhenOrdered];
                    if (matched.LibraryID != reference.ScanID) {
                        reference = msp.FirstOrDefault(msp => msp.ScanID == matched.LibraryID);
                    }
                    Ms2ReferenceSpectrum = reference?.Spectrum.Select(peak => new SpectrumPeakWrapper(peak)).ToList() ?? new List<SpectrumPeakWrapper>();
                }
            }).ConfigureAwait(false);
        }


        private void OnTargetChanged() {
            var target = Target;
            if (target == null) {
                BarItems = new List<BarItem>();
                EicChromatograms = new List<Chromatogram>();
                return;
            }

            // TODO: Implement other features (PeakHeight, PeakArea, Normalized PeakHeight, Normalized PeakArea)
            BarItems = target.AlignedPeakProperties
                .GroupBy(peak => param.FileID_ClassName[peak.FileID])
                .Select(pair => new BarItem { Class = pair.Key, Height = pair.Average(peak => peak.PeakHeightTop) })
                .ToList();

            // maybe using file pointer is better
            var spotinfo = chromatogramSpotSerializer.DeserializeAtFromFile(eicFile, target.MasterAlignmentID);
            EicChromatograms = spotinfo.PeakInfos.Select(peakinfo => new Chromatogram { Peaks = peakinfo.Chromatogram.Select(chrom => new PeakItem(chrom)).ToList() }).ToList();
        }

        bool PeakFilter(object obj) {
            if (obj is AlignmentSpotPropertyModel spot) {
                return AnnotationFilter(spot)
                    && MzFilter(spot)
                    && (!Ms2AcquiredChecked || spot.IsMsmsAssigned)
                    && (!MolecularIonChecked || spot.IsBaseIsotopeIon)
                    && (!BlankFilterChecked || spot.IsBlankFiltered);
            }
            return false;
        }

        bool AnnotationFilter(AlignmentSpotPropertyModel spot) {
            if (!ReadDisplayFilters(DisplayFilter.Annotates)) return true;
            return RefMatchedChecked && spot.IsRefMatched
                || SuggestedChecked && spot.IsSuggested
                || UnknownChecked && spot.IsUnknown;
        }

        bool MzFilter(AlignmentSpotPropertyModel spot) {
            return MassLower <= spot.MassCenter
                && spot.MassCenter <= MassUpper;
        }

        public DelegateCommand<Window> SearchCompoundCommand => searchCompoundCommand ?? (searchCompoundCommand = new DelegateCommand<Window>(SearchCompound));
        private DelegateCommand<Window> searchCompoundCommand;

        private void SearchCompound(Window owner) {
            var vm = new CompoundSearchVM(alignmentFile, Target.innerModel, msdecResult, msp, param.MspSearchParam, param.TargetOmics, null);
            var window = new CompoundSearchWindow {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            window.ShowDialog();
        }

        public DelegateCommand<Window> ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand<Window>(ShowIonTable));
        private DelegateCommand<Window> showIonTableCommand;

        private void ShowIonTable(Window owner) {
            var window = new View.Dims.IonTableViewer {
                DataContext = this,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = owner,
            };

            window.Show();
        }

        public void SaveProject() {
            MessagePackHandler.SaveToFile<AlignmentResultContainer>(Container, resultFile);
        }

    }
}
