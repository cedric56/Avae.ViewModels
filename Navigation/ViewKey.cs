namespace Avae.ViewModels;

internal readonly record struct ViewKey(object key, Type? Type, string? Discriminator = null)
{
    public static ViewKey For(object key)
        => new(key, null, null);

    public static ViewKey For(Type type, object key)
        => new(key, type, type.FullName);

    public override string ToString() => (Type, Discriminator) switch
    {
        (not null, not null) => $"{key}:{Type.FullName ?? Type.Name}:{Discriminator}",
        (not null, null) => $"{key}:{Type.FullName ?? Type.Name}",
        (null, not null) => $"{key}:{Discriminator}",
        (null, null) => key.ToString() ?? string.Empty,
    };

    public static (string viewKey, string viewModelKey) GetKeys(object? viewKey, object? viewModelKey, Type type)
    {
        return
            (
            viewKey is not null ? For(viewKey).ToString() : 
            viewModelKey is not null ? For(viewModelKey).ToString() : For(type, type.Name).ToString(),
            viewModelKey is not null ? For(viewModelKey).ToString() : For(type, type).ToString());
    }
}
