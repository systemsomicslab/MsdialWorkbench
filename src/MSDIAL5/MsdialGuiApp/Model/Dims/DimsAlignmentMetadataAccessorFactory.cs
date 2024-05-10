using CompMs.App.Msdial.Model.Export;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Export;

namespace CompMs.App.Msdial.Model.Dims
{
    internal sealed class DimsAlignmentMetadataAccessorFactory : IAlignmentMetadataAccessorFactory
    {
        private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> _refer;
        private readonly ParameterBase _parameter;

        public DimsAlignmentMetadataAccessorFactory(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter) {
            _refer = refer;
            _parameter = parameter;
        }

        public IMetadataAccessor CreateAccessor(bool trimSpectrumToExcelLimit) {
            return new DimsMetadataAccessor(_refer, _parameter, trimSpectrumToExcelLimit);
        }
    }
}
