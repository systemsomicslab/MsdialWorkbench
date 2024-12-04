namespace CompMs.Raw.Abstractions;

public interface IDataProviderFactory<in T>
{
    IDataProvider Create(T source);
}
