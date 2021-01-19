using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialDimsCore.Common;
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

        public double Ms1Tolerance {
            get => ms1Tolerance;
            set => SetProperty(ref ms1Tolerance, value);
        }
        private double ms1Tolerance;

        public int FileID { get; }
        public string FileName { get; }
        public double AccurateMass { get; }
        public string AdductName { get; }
        public string MetaboliteName { get; }

        private readonly MSDecResult msdecResult;
        private readonly List<MoleculeMsReference> mspDB;
        private readonly MsRefSearchParameterBase mspParam;
        private readonly TargetOmics omics;
        private readonly IReadOnlyList<IsotopicPeak> isotopes;
        
        public CompoundSearchVM(
            AnalysisFileBean analysisFile,
            ChromatogramPeakFeature peakFeature, MSDecResult msdecResult,
            List<MoleculeMsReference> mspDB, MsRefSearchParameterBase mspParam, 
            TargetOmics omics, IReadOnlyList<IsotopicPeak> isotopes) {

            this.msdecResult = msdecResult;
            this.mspDB = mspDB;
            this.mspParam = mspParam;
            this.omics = omics;
            this.isotopes = isotopes;

            FileID = analysisFile.AnalysisFileId;
            FileName = analysisFile.AnalysisFileName;
            AccurateMass = peakFeature.PrecursorMz;
            AdductName = peakFeature.PeakCharacter.AdductType.AdductIonName;
            MetaboliteName = peakFeature.Name;
            Ms1Tolerance = mspParam.Ms1Tolerance;

            Ms2DecSpectrum = msdecResult.Spectrum.Select(spec => new SpectrumPeakWrapper(spec)).ToList();
            Search();
        }

        public CompoundSearchVM(
            AlignmentFileBean alignmentFile, 
            AlignmentSpotProperty spot, MSDecResult msdecResult,
            List<MoleculeMsReference> mspDB, MsRefSearchParameterBase mspParam,
            TargetOmics omics, IReadOnlyList<IsotopicPeak> isotopes) {

            var peakFeature = spot.AlignedPeakProperties[spot.RepresentativeFileID];

            this.msdecResult = msdecResult;
            this.mspDB = mspDB;
            this.mspParam = mspParam;
            this.omics = omics;
            this.isotopes = isotopes;

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

        private async void Search() {
            var mspResults = await AnnotationProcess.RunMspAnnotationAsync(AccurateMass, msdecResult, mspDB, mspParam, omics, isotopes, Ms1Tolerance);
            Compounds = new ObservableCollection<CompoundResult>(
                mspResults.OrderByDescending(result => result.TotalScore)
                          .Select(result => new CompoundResult(mspDB[result.LibraryIDWhenOrdered], result))
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
}
