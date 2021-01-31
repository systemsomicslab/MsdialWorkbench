using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    public class CompoundSearchVM : ViewModelBase
    {
        public ObservableCollection<CompoundResult> Compounds {
            get => compounds;
            set => SetProperty(ref compounds, value);
        }
        private ObservableCollection<CompoundResult> compounds = new ObservableCollection<CompoundResult>();

        public List<SpectrumPeakWrapper> Ms2DecSpectrum {
            get => ms2DecSpectrum;
            set => SetProperty(ref ms2DecSpectrum, value);
        }
        private List<SpectrumPeakWrapper> ms2DecSpectrum = new List<SpectrumPeakWrapper>();

        public MsRefSearchParameterVM ParameterVM {
            get => parameterVM;
            set => SetProperty(ref parameterVM, value);
        }
        private MsRefSearchParameterVM parameterVM;

        public int FileID { get; }
        public string FileName { get; }
        public double AccurateMass { get; }
        public string AdductName { get; }
        public string MetaboliteName { get; }

        private readonly MSDecResult msdecResult;
        private readonly IAnnotator Annotator;
        private readonly IReadOnlyList<IsotopicPeak> isotopes;

        public CompoundSearchVM(
            AnalysisFileBean analysisFile,
            ChromatogramPeakFeature peakFeature, MSDecResult msdecResult,
            List<MoleculeMsReference> mspDB, MsRefSearchParameterBase mspParam,
            TargetOmics omics, IReadOnlyList<IsotopicPeak> isotopes)
            : this(analysisFile, peakFeature, msdecResult, isotopes, new DimsMspAnnotator(mspDB, mspParam, omics)) {
        }

        public CompoundSearchVM(
            AnalysisFileBean analysisFile,
            ChromatogramPeakFeature peakFeature, MSDecResult msdecResult,
            IReadOnlyList<IsotopicPeak> isotopes,
            IAnnotator annotator) {

            this.msdecResult = msdecResult;
            this.isotopes = isotopes;
            this.Annotator = annotator;
            this.ParameterVM = new MsRefSearchParameterVM(new MsRefSearchParameterBase { });

            FileID = analysisFile.AnalysisFileId;
            FileName = analysisFile.AnalysisFileName;
            AccurateMass = peakFeature.PrecursorMz;
            AdductName = peakFeature.PeakCharacter.AdductType.AdductIonName;
            MetaboliteName = peakFeature.Name;

            Ms2DecSpectrum = msdecResult.Spectrum.Select(spec => new SpectrumPeakWrapper(spec)).ToList();
            Search();
        }

        public CompoundSearchVM(
            AlignmentFileBean alignmentFile,
            AlignmentSpotProperty spot, MSDecResult msdecResult,
            List<MoleculeMsReference> mspDB, MsRefSearchParameterBase mspParam,
            TargetOmics omics, IReadOnlyList<IsotopicPeak> isotopes)
            : this(alignmentFile, spot, msdecResult, isotopes, new DimsMspAnnotator(mspDB, mspParam, omics)) {
        }

        public CompoundSearchVM(
            AlignmentFileBean alignmentFile, 
            AlignmentSpotProperty spot, MSDecResult msdecResult,
            IReadOnlyList<IsotopicPeak> isotopes,
            IAnnotator annotator) {

            var peakFeature = spot.AlignedPeakProperties[spot.RepresentativeFileID];

            this.msdecResult = msdecResult;
            this.isotopes = isotopes;
            this.Annotator = annotator;
            this.ParameterVM = new MsRefSearchParameterVM(new MsRefSearchParameterBase { });

            FileID = alignmentFile.FileID;
            FileName = alignmentFile.FileName;
            AccurateMass = peakFeature.Mass;
            AdductName = peakFeature.PeakCharacter.AdductType.AdductIonName;
            MetaboliteName = peakFeature.Name;

            Ms2DecSpectrum = msdecResult.Spectrum.Select(spec => new SpectrumPeakWrapper(spec)).ToList();
            Search();
        }

        public DelegateCommand SearchCommand => searchCommand ?? (searchCommand = new DelegateCommand(Search));
        private DelegateCommand searchCommand;

        private void Search() {
            var mspResults = Annotator.FindCandidates(msdecResult, null, ParameterVM.innerModel);
            // var mspResults = await AnnotationProcess.RunMspAnnotationAsync(AccurateMass, msdecResult, mspDB, mspParam, omics, isotopes, Ms1Tolerance);
            Compounds = new ObservableCollection<CompoundResult>(
                mspResults.OrderByDescending(result => result.TotalScore)
                    .Select(result => new CompoundResult(Annotator.Refer(result), result))
            );
        }
    }

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

        private readonly MoleculeMsReference msReference;
        private readonly MsScanMatchResult matchResult;
        public CompoundResult(MoleculeMsReference msReference, MsScanMatchResult matchResult) {
            this.msReference = msReference;
            this.matchResult = matchResult;
        }
    }

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
