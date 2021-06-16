using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    class ImmsCompoundSearchVM<T> : CompoundSearchVM<T> where T: IMSProperty, IMoleculeProperty, IIonProperty
    {
        public ImmsCompoundSearchVM(
            AnalysisFileBean analysisFile,
            T peakFeature, MSDecResult msdecResult,
            IReadOnlyList<IsotopicPeak> isotopes,
            IAnnotator<T, MSDecResult> annotator,
            MsRefSearchParameterBase parameter = null)
            : base(analysisFile, peakFeature, msdecResult, isotopes, annotator, parameter) {

        }

        public ImmsCompoundSearchVM(
            AlignmentFileBean alignmentFile,
            T spot, MSDecResult msdecResult,
            IReadOnlyList<IsotopicPeak> isotopes,
            IAnnotator<T, MSDecResult> annotator,
            MsRefSearchParameterBase parameter = null)
            : base(alignmentFile, spot, msdecResult, isotopes, annotator, parameter) {

        }

        protected override IReadOnlyList<CompoundResult> SearchCore() {
            var candidates = Annotator.FindCandidates(property, msdecResult, isotopes, ParameterVM.innerModel);
            foreach (var candidate in candidates) {
                candidate.IsManuallyModified = true;
                candidate.Source |= SourceType.Manual;
            }
            return new ObservableCollection<ImmsCompoundResult>(
                candidates.OrderByDescending(result => result.TotalScore)
                    .Select(result => new ImmsCompoundResult(Annotator.Refer(result), result)));
        }
    }

    class ImmsCompoundResult : CompoundResult
    {
        public ImmsCompoundResult(MoleculeMsReference msReference, MsScanMatchResult matchResult)
            : base(msReference, matchResult) {

        }

        public double CcsSimilarity => matchResult.CcsSimilarity;
    }
}
