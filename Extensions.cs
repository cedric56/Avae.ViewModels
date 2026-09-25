using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Avae.ViewModels;

public static class Extensions
{
    /// <summary>
    /// Registers an independent navigation region: a <see cref="Router"/> resolved as a keyed singleton
    /// under <paramref name="key"/>. Use this instead of registering <see cref="Router"/> as a plain,
    /// unkeyed singleton whenever a view model needs more than one independent navigation area (e.g. a
    /// main pane and a side pane) — resolving an unkeyed singleton twice returns the *same* instance,
    /// silently merging what were meant to be two separate navigation histories.
    /// </summary>
    /// <param name="key">The region's identifier, e.g. "main" or "side". Resolve it later with <see cref="GetRegion"/>.</param>
    public static IServiceCollection AddNavigationRegion(this IServiceCollection services, string key)
    {
        services.AddKeyedSingleton<Router>(key, (sp, _) => new Router(sp));
        return services;
    }

    /// <summary>
    /// Resolves the <see cref="Router"/> registered for <paramref name="key"/> via <see cref="AddNavigationRegion"/>.
    /// </summary>
    /// <param name="key">The region's identifier passed to <see cref="AddNavigationRegion"/>.</param>
    public static Router GetRegion(this IServiceProvider provider, string key)
        => provider.GetRequiredKeyedService<Router>(key);


    static T GetOrAdd<T>(this IServiceCollection services) where T : class, new()
    {
        var existing = services
            .FirstOrDefault(d => d.ServiceType == typeof(T))
            ?.ImplementationInstance as T;

        if (existing is not null)
            return existing;

        var map = new T();
        services.AddSingleton<T>(map);
        return map;
    }

    public static Task<TResult?> ShowModalAsync<TViewModel, TResult>(
        this IServiceProvider provider,
        NavigableContext? context = null) where TViewModel : ICloseableViewModel<TResult>
    {
        var viewModel = provider.GetViewModel<TViewModel>(context);
        var view = provider.GetModalFor<TViewModel, TResult>(context ?? new NavigableContext()) ?? throw new InvalidOperationException($"Unable to create view for {typeof(TViewModel).Name}.  Ensure that it is registered in the container.");
        view.Context = viewModel;
        return view.ShowModalAsync();
    }

    public static void RegisterWithLifetime<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(
    this IServiceCollection services,
    Func<IServiceProvider, TView> func,
    ServiceLifetime viewModelLifetime = ServiceLifetime.Transient,
    ServiceLifetime viewLifetime = ServiceLifetime.Transient,
    string? key = null)
    where TView : class where TViewModel : class
    => services.RegisterPageCore<TView, TViewModel>(
        key, (sp, args) => func(sp), viewModelLifetime, viewLifetime);

    private static void RegisterFactory<T>(
        this IServiceCollection services,
        object key,
        ServiceLifetime lifetime,
        Func<IServiceProvider, object[], T> create)
        where T : class
    {
        var cache = services.GetOrAdd<ScopedViewCache>();

        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                {
                    T? cached = null;
                    var gate = new object();
                    services.AddKeyedSingleton<Func<IServiceProvider, object[], object>>(key, (sp, parameters) =>
                    {
                        if (cached is not null) return cached;
                        lock (gate) { return cached ??= create(sp, parameters); }
                    });
                    break;
                }

            case ServiceLifetime.Scoped:
                services.AddKeyedSingleton<Func<IServiceProvider, object[], object>>(key, (sp, parameters) =>
                {
                    return cache.GetOrCreate(key, () => create(sp, parameters));
                });
                break;

            case ServiceLifetime.Transient:
                services.AddKeyedSingleton<Func<IServiceProvider, object[], object>>(key, create);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    public static void RegisterWithLifetime(
        this IServiceCollection services,
        string key,
        Func<IServiceProvider, object[], object> factory,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        => services.RegisterFactory(key, lifetime, factory);

    private static void RegisterPageCore<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(
        this IServiceCollection services,
        string? key,
        Func<IServiceProvider, object[], TView> createView,
        ServiceLifetime viewModelLifetime,
        ServiceLifetime viewLifetime)
        where TView : class where TViewModel : class
    {
        key ??= typeof(TViewModel).Name;

        services.GetOrAdd<ViewModelViewMap>().Map<TView, TViewModel>(key);
        services.RegisterViewModel<TViewModel>(viewModelLifetime);
        services.RegisterFactory(key, viewLifetime, createView);
    }

    public static void RegisterWithLifetime<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel,
        TArg1>(
        this IServiceCollection services,
        Func<IServiceProvider, TArg1, TView> func,
        ServiceLifetime viewModelLifetime = ServiceLifetime.Transient,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient,
        string? key = null)
        where TView : class where TViewModel : class
        => services.RegisterPageCore<TView, TViewModel>(
            key, (sp, args) => func(sp, args.Resolve<TArg1>(0)), viewModelLifetime, viewLifetime);

    private static T Resolve<T>(this object[] args, int index)
    {
        if (index < 0 || index >= args.Length)
            throw new ArgumentException($"Invalid index : {index} but length is : {args.Length}");
        var value = args[index];
        return value is T t ? t : 
            throw new InvalidCastException($"Excepted argument is type of : {typeof(T)} but resolved : {value.GetType()}");
    }

    public static void RegisterWithLifetime<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel,
        TArg1, TArg2>(
        this IServiceCollection services,
        Func<IServiceProvider, TArg1, TArg2, TView> func,
        ServiceLifetime viewModelLifetime = ServiceLifetime.Transient,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient,
        string? key = null)
        where TView : class where TViewModel : class
        => services.RegisterPageCore<TView, TViewModel>(
            key, (sp, args) => func(sp, args.Resolve<TArg1>(0), args.Resolve<TArg2>(1)), viewModelLifetime, viewLifetime);

    public static void RegisterWithLifetime<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel,
        TArg1, TArg2, TArg3>(
        this IServiceCollection services,
        Func<IServiceProvider, TArg1, TArg2, TArg3, TView> func,
        ServiceLifetime viewModelLifetime = ServiceLifetime.Transient,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient,
        string? key = null)
        where TView : class where TViewModel : class
        => services.RegisterPageCore<TView, TViewModel>(
            key, (sp, args) => func(sp, args.Resolve<TArg1>(0), args.Resolve<TArg2>(1), args.Resolve<TArg3>(2)),
            viewModelLifetime, viewLifetime);

    public static void RegisterWithLifetime<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel,
        TArg1, TArg2, TArg3, TArg4>(
        this IServiceCollection services,
        Func<IServiceProvider, TArg1, TArg2, TArg3, TArg4, TView> func,
        ServiceLifetime viewModelLifetime = ServiceLifetime.Transient,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient,
        string? key = null)
        where TView : class where TViewModel : class
        => services.RegisterPageCore<TView, TViewModel>(
            key, (sp, args) => func(sp, args.Resolve<TArg1>(0), args.Resolve<TArg2>(1), args.Resolve<TArg3>(2), args.Resolve<TArg4>(3)),
            viewModelLifetime, viewLifetime);

    public static void RegisterWithLifetime<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel,
        TArg1, TArg2, TArg3, TArg4, TArg5>(
        this IServiceCollection services,
        Func<IServiceProvider, TArg1, TArg2, TArg3, TArg4, TArg5, TView> func,
        ServiceLifetime viewModelLifetime = ServiceLifetime.Transient,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient,
        string? key = null)
        where TView : class where TViewModel : class
        => services.RegisterPageCore<TView, TViewModel>(
            key, (sp, args) => func(sp, args.Resolve<TArg1>(0), args.Resolve<TArg2>(1), args.Resolve<TArg3>(2), args.Resolve<TArg4>(3), args.Resolve<TArg5>(4)),
            viewModelLifetime, viewLifetime);

    public static void RegisterWithLifetime<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(
        this IServiceCollection services,
        ServiceLifetime viewModelLifetime = ServiceLifetime.Transient,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient,
        string? key = null)
        where TView : class where TViewModel : class
        => services.RegisterPageCore<TView, TViewModel>(
            key, ActivatorUtilities.CreateInstance<TView>, viewModelLifetime, viewLifetime);

    public static void Register<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(
        this IServiceCollection services, string? key = null)
        where TView : class where TViewModel : class
        => services.RegisterWithLifetime<TView, TViewModel>(
            ServiceLifetime.Singleton, ServiceLifetime.Transient, key);

    private static void RegisterViewModel<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TViewModel : class
        => services.RegisterFactory<TViewModel>(
            typeof(TViewModel), lifetime, ActivatorUtilities.CreateInstance<TViewModel>);

    public static T GetViewModel<T>(this IServiceProvider provider, NavigableContext? context = null)
        => provider.GetViewModel<T>(typeof(T), context);

    public static T GetViewModel<T>(this IServiceProvider provider, object key, NavigableContext? context = null)
        => (T)GetViewModel(provider, key, context);

    public static object GetViewModel(this IServiceProvider provider, object key, NavigableContext? context = null)
    {
        var factory = provider.GetKeyedService<Func<IServiceProvider, object[], object>>(key)
            ?? throw new InvalidOperationException($"Unable to create {key}. Ensure that it is registered with the service provider.");

        return factory(provider, [.. context?.ViewModelParameters.Select(v => v.value).ToArray() ?? []])
            ?? throw new InvalidOperationException($"Factory for {key} returned null.");
    }

    public static object GetView(this IServiceProvider provider, string key, object[] context)
    {
        var factory = provider.GetKeyedService<Func<IServiceProvider, object[], object>>(key)
            ?? throw new InvalidOperationException($"No such view registered: {key}");

        return factory(provider, [.. context ?? []])
            ?? throw new InvalidOperationException($"Unable to create view for {key}. Ensure that it is registered with the service provider.");
    }

    public static IViewFor? GetContextFor(this IServiceProvider provider, object key, NavigableContext? context = null)
    {
        context ??= new NavigableContext();
        var resolvedKey = context.Key ?? key;

        if (resolvedKey is not string stringKey)
            throw new InvalidOperationException($"GetContextFor requires a string key, got {resolvedKey?.GetType().Name ?? "null"}.");

        var view = provider.GetView(stringKey, [.. context.ViewParameters.Select(v => v.value).ToArray() ?? []]);
        return view as IViewFor
            ?? throw new InvalidOperationException($"View must implement {nameof(IViewFor)}");
    }

    public static IViewFor? GetContextFor<TViewModel>(this IServiceProvider provider, NavigableContext context)
        => provider.GetContextFor(typeof(TViewModel).Name, context) as IViewFor;

    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "View types are always registered via explicit compile-time factories " +
                         "in RegisterPageCore. The concrete type and its constructor are referenced " +
                         "directly at the registration call site, so the trimmer already roots that " +
                         "type — including the interfaces it implements.")]
    public static IModalFor<TViewModel, TResult>? GetModalFor<TViewModel, TResult>(
        this IServiceProvider provider, NavigableContext context)
        where TViewModel : ICloseableViewModel<TResult>
    {
        var view = provider.GetContextFor(typeof(TViewModel).Name, context);
        if (view != null)
        {
            var modalInterface = view.GetType().GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IModalFor<,>));

            if (modalInterface != null)
            {
                var resultType = modalInterface.GetGenericArguments()[1];
                if (resultType != typeof(TResult))
                    throw new InvalidOperationException(
                        $"The view associated with view model {typeof(TViewModel).Name} expects result type " +
                        $"{resultType.Name}, but {typeof(TResult).Name} was requested.");
            }
        }

        return view as IModalFor<TViewModel, TResult>
            ?? throw new InvalidOperationException(
                $"The view associated with the view model {typeof(TViewModel).Name} is not a {nameof(IModalFor<TViewModel, TResult>)}.");
    }

    public static void Update<X, Y>(this IList<Y> items, IEnumerable<X> selectedItems, Func<X, Y, bool> predicate, Func<X, Y> add)
    {
        foreach (var x in selectedItems)
            if (!items.Any(y => predicate(x, y)))
                items.Add(add(x));

        var deleted = items.Where(item => !selectedItems.Any(x => predicate(x, item))).ToList();
        foreach (var item in deleted)
            items.Remove(item);
    }
}