using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsMethodModel : MethodModelBase
    {
        public GcmsMethodModel(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, IEnumerable<AlignmentFileBean> alignmentFiles, ProjectBaseParameterModel projectBaseParameter) : base(analysisFileBeanModelCollection, alignmentFiles, projectBaseParameter) {

        }

        public override Task RunAsync(ProcessOption option, CancellationToken token) {
            if (option.HasFlag(ProcessOption.PeakSpotting | ProcessOption.Identification)) {
                RunFromPeakSpotting();
            }
            else if (option.HasFlag(ProcessOption.Identification)) {
                RunFromIdentification();
            }

            if (option.HasFlag(ProcessOption.Alignment)) {
                RunAlignment();
            }

            return Task.CompletedTask;
        }

        private Task RunFromPeakSpotting() {
            throw new NotImplementedException();
        }

        private Task RunFromIdentification() {
            throw new NotImplementedException();
        }

        private Task RunAlignment() {
            throw new NotImplementedException();
        }

        protected override IAlignmentModel LoadAlignmentFileCore(AlignmentFileBean alignmentFile) {
            throw new NotImplementedException();
        }

        protected override IAnalysisModel LoadAnalysisFileCore(AnalysisFileBeanModel analysisFile) {
            throw new NotImplementedException();
        }
    }
}
