using CompMs.App.Msdial.Model.Export;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialImmsCore.Export;
using System;

namespace CompMs.App.Msdial.Model.Imms
{
    internal sealed class ImmsAlignmentMetadataAccessorFactory : IAlignmentMetadataAccessorFactory
    {
        private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> _refer;
        private readonly ParameterBase _parameter;

        public ImmsAlignmentMetadataAccessorFactory(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter) {
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        public IMetadataAccessor CreateAccessor(bool trimSpectrumToExcelLimit) {
            return new ImmsMetadataAccessor(_refer, _parameter, trimSpectrumToExcelLimit);
        }
    }
}
