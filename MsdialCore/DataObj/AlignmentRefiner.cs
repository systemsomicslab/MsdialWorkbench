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
                            DataObjConverter.SetDefaultCompoundInformation(alignments[currentPeakId]);
                            currentPeakId = i;
                        }
                        else {
                            DataObjConverter.SetDefaultCompoundInformation(alignments[i]);
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
                            DataObjConverter.SetDefaultCompoundInformation(alignments[currentPeakId]);
                            currentPeakId = i;
                        }
                        else {
                            DataObjConverter.SetDefaultCompoundInformation(alignments[i]);
                        }
                    }
                }
            }
        }

        protected virtual List<AlignmentSpotProperty> FilterByBlank(List<AlignmentSpotProperty> alignments) {
            var fcSpots = new List<AlignmentSpotProperty>();
            int blankNumber = 0;
            int sampleNumber = 0;
            foreach (var value in _param.FileID_AnalysisFileType.Values) {
                if (value == AnalysisFileType.Blank) blankNumber++;
                if (value == AnalysisFileType.Sample) sampleNumber++;
            }

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

                    sampleAve = sampleAve / sampleNumber;
                    blankAve = blankAve / blankNumber;
                    if (blankAve == 0) {
                        if (nonMinValue != double.MaxValue)
                            blankAve = nonMinValue * 0.1;
                        else
                            blankAve = 1.0;
                    }

                    var blankThresh = blankAve * _param.FoldChangeForBlankFiltering;
                    var sampleThresh = _param.BlankFiltering == BlankFiltering.SampleMaxOverBlankAve ? sampleMax : sampleAve;
                
                    if (sampleThresh < blankThresh) {
                        if (_param.IsKeepRemovableFeaturesAndAssignedTagForChecking) {

                            if (_param.IsKeepRefMatchedMetaboliteFeatures
                              && (spot.MspID >= 0 || spot.TextDbID >= 0) && !spot.Name.Contains("w/o")) {

                            }
                            else if (_param.IsKeepSuggestedMetaboliteFeatures
                              && (spot.MspID >= 0 || spot.TextDbID >= 0) && spot.Name.Contains("w/o")) {

                            }
                            else {
                                spot.FeatureFilterStatus.IsBlankFiltered = true;
                            }
                        }
                        else {

                            if (_param.IsKeepRefMatchedMetaboliteFeatures
                             && (spot.MspID >= 0 || spot.TextDbID >= 0) && !spot.Name.Contains("w/o")) {

                            }
                            else if (_param.IsKeepSuggestedMetaboliteFeatures
                              && (spot.MspID >= 0 || spot.TextDbID >= 0) && spot.Name.Contains("w/o")) {

                            }
                            else {
                                continue;
                            }
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
    }
}
