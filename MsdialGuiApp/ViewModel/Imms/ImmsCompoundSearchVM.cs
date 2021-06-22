using CompMs.App.Msdial.Model.Imms;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    class ImmsCompoundSearchVM<T> : CompoundSearchVM<T> where T: IMSProperty, IMoleculeProperty, IIonProperty
    {
        public ImmsCompoundSearchVM(ImmsCompoundSearchModel<T> model) : base(model) {
            searchUnsubscriber?.Dispose();

            var ms1Tol = ParameterVM.Ms1Tolerance;
            var ms2Tol = ParameterVM.Ms2Tolerance;
            var ccsTol = ParameterVM.CcsTolerance;
            var condition = new[]
            {
                ms1Tol.ObserveHasErrors.Inverse(),
                ms2Tol.ObserveHasErrors.Inverse(),
                ccsTol.ObserveHasErrors.Inverse(),
            }.CombineLatestValuesAreAllTrue();

            searchUnsubscriber = new[] {
                SearchCommand.ToUnit()
            }.Merge()
            .CombineLatest(condition, (_, c) => c)
            .Where(c => c)
            .Select(_ => SearchAsync())
            .Switch()
            .Subscribe(cs => Compounds = cs)
            .AddTo(Disposables);

            SearchCommand.Execute();
        }

        public ImmsCompoundSearchVM(
            AnalysisFileBean analysisFile,
            T peakFeature, MSDecResult msdecResult,
            IReadOnlyList<IsotopicPeak> isotopes,
            IAnnotator<T, MSDecResult> annotator,
            MsRefSearchParameterBase parameter = null)
            : this(new ImmsCompoundSearchModel<T>(
                analysisFile,
                peakFeature,
                msdecResult,
                isotopes,
                annotator,
                parameter)) {

        }

        public ImmsCompoundSearchVM(
            AlignmentFileBean alignmentFile,
            T spot, MSDecResult msdecResult,
            IReadOnlyList<IsotopicPeak> isotopes,
            IAnnotator<T, MSDecResult> annotator,
            MsRefSearchParameterBase parameter = null)
            : this(new ImmsCompoundSearchModel<T>(
                alignmentFile,
                spot,
                msdecResult,
                isotopes,
                annotator,
                parameter)) {

        }
    }
}
