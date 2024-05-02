using CompMs.MsdialCore.Export;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class IdentityAlignmentMetadataAccessorFactory : IAlignmentMetadataAccessorFactory {
        private readonly IMetadataAccessor _accessor;

        public IdentityAlignmentMetadataAccessorFactory(IMetadataAccessor accessor)
        {
            _accessor = accessor;
        }

        public IMetadataAccessor CreateAccessor(bool trimSpectrumToExcelLimit) => _accessor;
    }
}
