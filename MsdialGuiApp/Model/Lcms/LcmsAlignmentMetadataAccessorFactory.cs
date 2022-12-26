using CompMs.App.Msdial.Model.Export;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcMsApi.Export;
using System;

namespace CompMs.App.Msdial.Model.Lcms
{
    internal sealed class LcmsAlignmentMetadataAccessorFactory : IAlignmentMetadataAccessorFactory
    {
        private readonly DataBaseMapper _mapper;
        private readonly ParameterBase _parameter;

        public LcmsAlignmentMetadataAccessorFactory(DataBaseMapper mapper, ParameterBase parameter) {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        public IMetadataAccessor CreateAccessor(bool trimSpectrumToExcelLimit) {
            switch (_parameter.ProjectParam.TargetOmics) {
                case TargetOmics.Proteomics:
                    return new LcmsProteomicsMetadataAccessor(_mapper, trimSpectrumToExcelLimit);
                default:
                    return new LcmsMetadataAccessor(_mapper, _parameter, trimSpectrumToExcelLimit);
            }
        }
    }
}
