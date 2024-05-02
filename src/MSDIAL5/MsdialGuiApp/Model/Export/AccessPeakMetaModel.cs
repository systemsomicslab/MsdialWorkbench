using CompMs.CommonMVVM;
using CompMs.MsdialCore.Export;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AccessPeakMetaModel : BindableBase {
        private readonly IAlignmentMetadataAccessorFactory _accessorFactory;

        public AccessPeakMetaModel(IAlignmentMetadataAccessorFactory accessorFactory)
        {
            _accessorFactory = accessorFactory;
        }

        public bool TrimToExcelLimit {
            get => _trimToExcelLimit;
            set => SetProperty(ref _trimToExcelLimit, value);
        }
        private bool _trimToExcelLimit = true;

        public IMetadataAccessor GetAccessor() => _accessorFactory.CreateAccessor(TrimToExcelLimit);
    }
}
