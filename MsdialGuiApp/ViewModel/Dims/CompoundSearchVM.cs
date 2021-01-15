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

        public int FileID => analysisFile.AnalysisFileId;
        public string FileName => analysisFile.AnalysisFileName;
        public double AccurateMass => peakFeature.Mass;
        public string AdductName => peakFeature.PeakCharacter.AdductType.AdductIonName;
        public string MetaboliteName => peakFeature.Name;

        private readonly AnalysisFileBean analysisFile;
        private readonly ChromatogramPeakFeature peakFeature;
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

            this.analysisFile = analysisFile;
            this.peakFeature = peakFeature;
            this.msdecResult = msdecResult;
            this.mspDB = mspDB;
            this.mspParam = mspParam;
            this.omics = omics;
            this.isotopes = isotopes;

            Ms2DecSpectrum = msdecResult.Spectrum.Select(spec => new SpectrumPeakWrapper(spec)).ToList();
            Search();
        }

        public DelegateCommand SearchCommand => searchCommand ?? (searchCommand = new DelegateCommand(Search));
        private DelegateCommand searchCommand;

        private void Search() {
            AnnotationProcess.Run(peakFeature, msdecResult, mspDB, null, mspParam, omics, isotopes, out var mspResults, out _);
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
