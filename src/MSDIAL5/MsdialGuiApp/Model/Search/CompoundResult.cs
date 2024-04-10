using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Search
{
    public interface ICompoundResult
    {
        MoleculeMsReference MsReference { get; }
        MsScanMatchResult MatchResult { get; }
    }

    public class CompoundResult : ICompoundResult
    {
        public int LibraryID => matchResult.LibraryID;
        public string Name => msReference.Name;
        public string AdductName => msReference.AdductType.AdductIonName;
        public double PrecursorMz => msReference.PrecursorMz;
        public ChromXs ChromXs => msReference.ChromXs;
        public string Instrument => msReference.InstrumentModel;
        public string Comment => msReference.Comment;
        public double WeightedDotProduct => matchResult.WeightedDotProduct;
        public double SimpleDotProduct => matchResult.SimpleDotProduct;
        public double ReverseDotProduct => matchResult.ReverseDotProduct;
        public double MassSimilarity => matchResult.AcurateMassSimilarity;
        public double Presence => matchResult.MatchedPeaksPercentage;
        public double TotalScore => matchResult.TotalScore;
        public List<SpectrumPeakWrapper> Spectrum => spectrum ??= msReference.Spectrum.Select(spec => new SpectrumPeakWrapper(spec)).ToList();
        private List<SpectrumPeakWrapper>? spectrum;

        protected readonly MoleculeMsReference msReference;
        protected readonly MsScanMatchResult matchResult;

        MoleculeMsReference ICompoundResult.MsReference => msReference;
        MsScanMatchResult ICompoundResult.MatchResult => matchResult;

        public CompoundResult(MoleculeMsReference msReference, MsScanMatchResult matchResult) {
            this.msReference = msReference;
            this.matchResult = matchResult;
        }
    }
}
