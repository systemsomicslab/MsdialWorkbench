using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public abstract class AlignmentRefiner
    {
        protected ParameterBase _param;

        protected abstract List<AlignmentSpotProperty> GetCleanedSpots(List<AlignmentSpotProperty> alignments);
        protected abstract void SetLinks(List<AlignmentSpotProperty> alignments);
        protected abstract void PostProcess(List<AlignmentSpotProperty> alignments);

        public AlignmentRefiner(ParameterBase param) {
            _param = param;
        }

        public AlignmentRefiner() : this(new ParameterBase()) { }

        public List<AlignmentSpotProperty> Refine(IList<AlignmentSpotProperty> alignments) {
            var spots = alignments.ToList();

            Deduplicate(spots);
            var cleaned = GetCleanedSpots(spots);
            var filtered = FilterByBlank(cleaned);
            SetLinks(filtered);
            PostProcess(filtered);

            return filtered;
        }

        protected virtual void Deduplicate(List<AlignmentSpotProperty> alignments) {
            if (_param.OnlyReportTopHitInMspSearch) { //to remove duplicate identifications
                alignments = alignments.OrderByDescending(spot => spot.MspID).ToList();

                var currentPeakId = 0;
                var currentLibraryId = alignments[currentPeakId].MspID;

                for (int i = 1; i < alignments.Count; i++) {
                    if (alignments[i].MspID < 0) break;
                    if (alignments[i].MspID != currentLibraryId) {
                        currentPeakId = i;
                        currentLibraryId = alignments[currentPeakId].MspID;
                        continue;
                    }
                    else {
                        if (alignments[currentPeakId].MspBasedMatchResult.TotalScore < alignments[i].MspBasedMatchResult.TotalScore) {
                            SetDefaultCompoundInformationInMspSearch(alignments[currentPeakId]);
                            currentPeakId = i;
                        }
                        else {
                            SetDefaultCompoundInformationInMspSearch(alignments[i]);
                        }
                    }
                }
            }

            if (_param.OnlyReportTopHitInTextDBSearch) {
                alignments = alignments.OrderByDescending(n => n.TextDbID).ToList();

                var currentPeakId = 0;
                var currentLibraryId = alignments[currentPeakId].TextDbID;

                for (int i = 1; i < alignments.Count; i++) {
                    if (alignments[i].TextDbID < 0) break;
                    if (alignments[i].TextDbID != currentLibraryId) {
                        currentLibraryId = alignments[i].TextDbID;
                        currentPeakId = i;
                        continue;
                    }
                    else {
                        if (alignments[currentPeakId].TextDbBasedMatchResult.TotalScore < alignments[i].TextDbBasedMatchResult.TotalScore) {
                            SetDefaultCompoundInformationInTextSearch(alignments[currentPeakId]);
                            currentPeakId = i;
                        }
                        else {
                            SetDefaultCompoundInformationInTextSearch(alignments[i]);
                        }
                    }
                }
            }
        }

        protected virtual List<AlignmentSpotProperty> FilterByBlank(List<AlignmentSpotProperty> alignments) {
            var fcSpots = new List<AlignmentSpotProperty>();
            int blankNumber = _param.FileID_AnalysisFileType.Values.Count(v => v == AnalysisFileType.Blank);
            int sampleNumber = _param.FileID_AnalysisFileType.Values.Count(v => v == AnalysisFileType.Sample);

            if (blankNumber > 0 && _param.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange) {
              
                foreach (var spot in alignments) {
                    var sampleMax = 0.0;
                    var sampleAve = 0.0;
                    var blankAve = 0.0;
                    var nonMinValue = double.MaxValue;

                    foreach (var peak in spot.AlignedPeakProperties) {
                        var filetype = _param.FileID_AnalysisFileType[peak.FileID];
                        if (filetype == AnalysisFileType.Blank) {
                            blankAve += peak.PeakHeightTop;
                        }
                        else if (filetype == AnalysisFileType.Sample) {
                            if (peak.PeakHeightTop > sampleMax)
                                sampleMax = peak.PeakHeightTop;
                            sampleAve += peak.PeakHeightTop;
                        }

                        if (nonMinValue > peak.PeakHeightTop && peak.PeakHeightTop > 0.0001) {
                            nonMinValue = peak.PeakHeightTop;
                        }
                    }

                    sampleAve /= sampleNumber;
                    blankAve /= blankNumber;
                    if (blankAve == 0) {
                        if (nonMinValue != double.MaxValue)
                            blankAve = nonMinValue * 0.1;
                        else
                            blankAve = 1.0;
                    }

                    var blankThresh = blankAve * _param.FoldChangeForBlankFiltering;
                    var sampleThresh = _param.BlankFiltering == BlankFiltering.SampleMaxOverBlankAve ? sampleMax : sampleAve;
                
                    if (sampleThresh < blankThresh) {

                        if (_param.IsKeepRefMatchedMetaboliteFeatures && spot.IsReferenceMatched) {

                        }
                        else if (_param.IsKeepSuggestedMetaboliteFeatures && spot.IsAnnotationSuggested) {

                        }
                        else if (_param.IsKeepRemovableFeaturesAndAssignedTagForChecking) {
                            spot.FeatureFilterStatus.IsBlankFiltered = true;
                        }
                        else {
                            continue;
                        }
                    }
                    fcSpots.Add(spot);
                }
            }
            else {
                fcSpots = alignments;
            }

            return fcSpots;
        }

        static void SetDefaultCompoundInformationInMspSearch(AlignmentSpotProperty alignmentSpot) {
            var textdb = alignmentSpot.TextDbBasedMatchResult;
            if (textdb == null || textdb.Name != alignmentSpot.Name) {
                alignmentSpot.AdductType.AdductIonName = string.Empty;
                alignmentSpot.PeakCharacter.Charge = 1;
                alignmentSpot.Name = string.Empty;
            }

            alignmentSpot.MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
        }

        static void SetDefaultCompoundInformationInTextSearch(AlignmentSpotProperty alignmentSpot) {
            var mspdb = alignmentSpot.MspBasedMatchResult;
            if (mspdb == null || mspdb.Name != alignmentSpot.Name) {
                alignmentSpot.AdductType.AdductIonName = string.Empty;
                alignmentSpot.PeakCharacter.Charge = 1;
                alignmentSpot.Name = string.Empty;
            }

            alignmentSpot.TextDbBasedMatchResult = null;
        }
    }
}
