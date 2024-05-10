using CompMs.App.Msdial.Model.Export;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Export;
using CompMs.MsdialLcImMsApi.Export;
using CompMs.MsdialLcImMsApi.Parameter;
using System;

namespace CompMs.App.Msdial.Model.Lcimms
{
    internal sealed class LcimmsAlignmentMetadataAccessorFactory : IAlignmentMetadataAccessorFactory
    {
        private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> _refer;
        private readonly MsdialLcImMsParameter _parameter;

        public LcimmsAlignmentMetadataAccessorFactory(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, MsdialLcImMsParameter parameter) {
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        public IMetadataAccessor CreateAccessor(bool trimSpectrumToExcelLimit) {
            return new LcimmsMetadataAccessor(_refer, _parameter, trimSpectrumToExcelLimit);
        }
    }
}
