using CompMs.MsdialCore.Export;

namespace CompMs.App.Msdial.Model.Export
{
    internal interface IAlignmentMetadataAccessorFactory
    {
        IMetadataAccessor CreateAccessor(bool trimSpectrumToExcelLimit);
    }
}
