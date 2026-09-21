namespace Avae.ViewModels;

public interface IIconResolver
{
    object? GetIcon(string key);

    object? GetSource(string key);
}

public static class IconResolver
{
    private static readonly List<IIconResolver> _providers = [];

    public static void Register(IIconResolver provider) => _providers.Add(provider);

    public static object? GetIcon(string key)
    {
        foreach (var provider in _providers)
        {
            var icon = provider.GetIcon(key);
            if (icon != null) return icon;
        }
        return null;
    }

    public static object? GetSource(string key)
    {
        foreach (var provider in _providers)
        {
            var source = provider.GetSource(key);
            if (source != null) return source;
        }
        return null;
    }
}