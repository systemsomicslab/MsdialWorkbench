using CompMs.Raw.Abstractions;

namespace CompMs.MsdialCore.Algorithm.Internal;

internal sealed class MS1CachedDataProviderFactory<T>(IDataProviderFactory<T> other) : IDataProviderFactory<T>
{
    private readonly IDataProviderFactory<T> _dataProviderFactory = other;

    public IDataProvider Create(T source) {
        return new MS1CachedDataProvider(_dataProviderFactory.Create(source));
    }
}
