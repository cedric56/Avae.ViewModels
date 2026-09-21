using System.Diagnostics.CodeAnalysis;

namespace Avae.ViewModels;

class ViewModelViewMap : IDisposable
{
    private readonly Dictionary<string, KeyValuePair<Type, Type>> _map = new();
    public void Map<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView, TViewModel>(string key) where TViewModel : class where TView : class
        => _map[key] = new KeyValuePair<Type, Type>(typeof(TView), typeof(TViewModel));

    [UnconditionalSuppressMessage("Trimming", "IL2073",
        Justification = "Les valeurs de _map proviennent uniquement de Map<TView>(), " +
                         "dont le paramètre TView porte déjà [DynamicallyAccessedMembers(PublicConstructors)]. " +
                         "L'invariant est donc garanti à l'écriture, mais non traçable par le linker à travers le Dictionary<Type,Type>.")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type? GetViewType(string key)
     => _map.GetValueOrDefault(key).Key;

    public void Dispose()
    {
        _map.Clear();
    }
}

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