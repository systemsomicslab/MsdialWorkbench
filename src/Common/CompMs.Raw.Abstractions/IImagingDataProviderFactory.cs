namespace CompMs.Raw.Abstractions;

public interface IImagingDataProviderFactory<in T> : IDataProviderFactory<T>
{
    new IImagingDataProvider Create(T target);
}
