using CompMs.App.Msdial.Model.Export;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialGcMsApi.Export;
using System;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsAlignmentMetadataAccessorFactory : IAlignmentMetadataAccessorFactory
    {
        private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> _refer;
        private readonly ParameterBase _parameter;

        public GcmsAlignmentMetadataAccessorFactory(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter) {
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        public IMetadataAccessor CreateAccessor(bool trimSpectrumToExcelLimit) {
            return new GcmsAlignmentMetadataAccessor(_refer, _parameter, trimSpectrumToExcelLimit);
        }
    }
}
