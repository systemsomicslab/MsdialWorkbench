namespace CompMs.Raw.Contract;

public interface IImagingDataProviderFactory<in T> : IDataProviderFactory<T>
{
    new IImagingDataProvider Create(T target);
}
