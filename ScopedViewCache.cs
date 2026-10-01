namespace Avae.ViewModels;

internal class ScopedViewCache : IDisposable
{
    private readonly Dictionary<object, object> _cache = new();
    private readonly object _gate = new();

    public object GetOrCreate(object key, Func<object> factory)
    {
        if (_cache.TryGetValue(key, out var existing))
            return existing;

        lock (_gate)
        {
            return _cache.TryGetValue(key, out existing)
                ? existing
                : _cache[key] = factory();
        }
    }

    public void Dispose()
    {
        _cache.Clear();
    }
}