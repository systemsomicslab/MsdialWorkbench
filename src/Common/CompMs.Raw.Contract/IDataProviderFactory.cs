namespace CompMs.Raw.Contract;

public interface IDataProviderFactory<in T>
{
    IDataProvider Create(T source);
}
