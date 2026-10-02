using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Avae.ViewModels;

public static class Extensions
{
    public static void RegisterNavigation(this IServiceCollection services)
    {
        services.AddTransient<NavigationHistory>();
        services.AddTransient<NavigationGate>();
        services.AddTransient<NavigationLifecycle>();
        services.AddTransient<IRouter, Router>();
    }

    public static void RegisterEnvironments(this IServiceCollection services,
        Runtime runtime)
    {
        services.AddSingleton<IRuntime>(new RuntimeImplementation(runtime));
        services.AddSingleton<IDesktop>(new DesktopImplementation());
        services.AddSingleton<IDistribution>(new DistributionImplementation());
    }

    /// <summary>
    /// Registers an independent navigation region: a <see cref="Router"/> resolved as a keyed
    /// service under <paramref name="key"/>. Use this instead of registering <see cref="Router"/>
    /// as a plain, unkeyed singleton whenever a view model needs more than one independent
    /// navigation area (e.g. a main pane and a side pane) — resolving an unkeyed singleton twice
    /// returns the *same* instance, silently merging what were meant to be two separate
    /// navigation histories.
    /// </summary>
    /// <param name="key">The region's identifier, e.g. "main" or "side". Resolve it later with <see cref="GetRegion"/>.</param>
    /// <param name="lifetime">
    /// <see cref="ServiceLifetime.Scoped"/> (the default) gives each DI scope — each Blazor
    /// Server circuit — its own independent <see cref="Router"/>/history/cache chain, which is
    /// required on Server since every circuit shares the same process. On WASM/MAUI there is
    /// only ever one scope for the app's lifetime, so <see cref="ServiceLifetime.Scoped"/>
    /// behaves identically to <see cref="ServiceLifetime.Singleton"/> there — pass Singleton
    /// explicitly only if you specifically need the region's state to survive across scopes
    /// your own app creates for other reasons.
    /// </param>
    public static IServiceCollection AddNavigationRegion(
        this IServiceCollection services, string key, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        services.Add(new ServiceDescriptor(typeof(NavigationHistory), key, (sp, k) => new NavigationHistory(), lifetime));
        services.Add(new ServiceDescriptor(typeof(NavigationGate), key, (sp, k) => new NavigationGate(), lifetime));
        services.Add(new ServiceDescriptor(typeof(NavigationLifecycle), key, (sp, k) => new NavigationLifecycle(), lifetime));
        services.Add(new ServiceDescriptor(typeof(IRouter), key, (sp, k) =>
            new Router(
                sp.GetRequiredKeyedService<NavigationHistory>(k),
                sp.GetRequiredKeyedService<NavigationGate>(k),
                sp.GetRequiredKeyedService<NavigationLifecycle>(k),
                sp),
            lifetime));

        return services;
    }

    /// <summary>
    /// Resolves the <see cref="Router"/> registered for <paramref name="key"/> via <see cref="AddNavigationRegion"/>.
    /// </summary>
    /// <param name="key">The region's identifier passed to <see cref="AddNavigationRegion"/>.</param>
    public static IRouter GetRegion(this IServiceProvider provider, string key)
        => provider.GetRequiredKeyedService<IRouter>(key);

    /// <summary>
    /// Registers <typeparamref name="T"/> as a real DI-scoped service, once, if it hasn't
    /// been registered already. Use this for state that must not leak between circuits/scopes (e.g.
    /// <see cref="ScopedViewCache"/>);
    /// </summary>
    static void EnsureScoped<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services) where T : class
    {
        if (!services.Any(d => d.ServiceType == typeof(T)))
            services.AddScoped<T>();
    }

    public static Task<TResult?> ShowModalAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel, TResult>(
        this IServiceProvider provider,
        NavigableContext? context = null,
        object? viewKey = null,
        object? viewModelKey = null) where TViewModel : class, ICloseableViewModel<TResult>
    {
        var keys = ViewKey.GetKeys(viewKey, viewModelKey, typeof(TViewModel));
        viewKey = keys.viewKey;
        viewModelKey = keys.viewModelKey;

        var viewModel = provider.GetViewModel<TViewModel>(viewModelKey, context);
        var view = provider.GetModalFor<TViewModel, TResult>(context ?? new NavigableContext(), viewKey) ?? throw new InvalidOperationException($"Unable to create view for {typeof(TViewModel).Name}.  Ensure that it is registered in the container.");
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
    object? viewKey = null,
    object? viewModelKey = null)
    where TView : class where TViewModel : class
    => services.RegisterPageCore<TView, TViewModel>(
        (sp, args) => func(sp), viewModelLifetime, viewLifetime, viewKey, viewModelKey);

    /// <summary>
    /// Registers <paramref name="create"/> as a keyed <em>instance</em>: the resulting
    /// <c>Func&lt;IServiceProvider, object[], object&gt;</c> IS the keyed service itself (see the
    /// <c>AddKeyedSingleton&lt;TService&gt;(key, TService instance)</c> overload — not the
    /// <c>Func&lt;IServiceProvider, object?, TService&gt;</c> factory overload). This is
    /// deliberate: callers (<see cref="GetViewModel(IServiceProvider, object, NavigableContext?)"/>,
    /// <see cref="GetView"/>) invoke the returned delegate themselves, passing whatever
    /// <see cref="IServiceProvider"/> and arguments they have at hand — the <c>sp</c>/<c>parameters</c>
    /// below are the delegate's own parameters, supplied per call, never captured from a
    /// DI-resolved registration-time provider.
    /// </summary>
    private static void RegisterFactory<T>(
        this IServiceCollection services,
        object key,
        ServiceLifetime lifetime,
        Func<IServiceProvider, object[], T> create)
        where T : class
    {
        services.EnsureScoped<ScopedViewCache>();

        Func<IServiceProvider, object[], object> factory = lifetime switch
        {
            ServiceLifetime.Singleton => CreateSingletonFactory(create),
            // Resolve the cache from `sp` — the per-call provider the caller (GetViewModel/
            // GetView) passes in — NOT from a cache instance captured once at registration
            // time. ScopedViewCache is now AddScoped, so this returns the current scope's own
            // cache: one per circuit on Blazor Server, disposed with that circuit, instead of
            // one process-wide dictionary shared by every user.
            ServiceLifetime.Scoped => (sp, parameters) =>
            {
                //using 
                var scope = sp.CreateScope();
                return scope.ServiceProvider
                            .GetRequiredService<ScopedViewCache>()
                            .GetOrCreate(key, () => create(scope.ServiceProvider, parameters));
            },
            ServiceLifetime.Transient => (sp, parameters) => create(sp, parameters),
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null),
        };

        services.AddKeyedSingleton(key, factory);
    }

    private static Func<IServiceProvider, object[], object> CreateSingletonFactory<T>(
        Func<IServiceProvider, object[], T> create)
        where T : class
    {
        T? cached = null;
        var gate = new object();
        return (sp, parameters) =>
        {
            if (cached is not null) return cached;
            lock (gate) { return cached ??= create(sp, parameters); }
        };
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
        Func<IServiceProvider, object[], TView> createView,
        ServiceLifetime viewModelLifetime,
        ServiceLifetime viewLifetime,
        object? viewKey = null,
        object? viewModelKey = null)
        where TView : class where TViewModel : class
    {
        var keys = ViewKey.GetKeys(viewKey, viewModelKey, typeof(TViewModel));
        viewKey = keys.viewKey;
        viewModelKey = keys.viewModelKey;

        services.RegisterViewModel<TViewModel>(viewModelKey, viewModelLifetime);
        services.RegisterFactory(viewKey, viewLifetime, createView);
    }

    public static void RegisterWithLifetime<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel,
        TArg1>(
        this IServiceCollection services,
        Func<IServiceProvider, TArg1, TView> func,
        ServiceLifetime viewModelLifetime = ServiceLifetime.Transient,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient,
        object? viewKey = null,
        object? viewModelKey = null)
        where TView : class where TViewModel : class
    {
        var keys = ViewKey.GetKeys(viewKey, viewModelKey, typeof(TViewModel));
        viewKey = keys.viewKey;
        viewModelKey = keys.viewModelKey;

        services.RegisterPageCore<TView, TViewModel>(
            (sp, args) => func(sp, args.Resolve<TArg1>(0)), viewModelLifetime, viewLifetime,
            viewKey, viewModelKey);
    }

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
        object? viewKey = null,
        object? viewModelKey = null)
        where TView : class where TViewModel : class
        => services.RegisterPageCore<TView, TViewModel>(
            (sp, args) => func(sp, args.Resolve<TArg1>(0), args.Resolve<TArg2>(1)), viewModelLifetime, viewLifetime, viewKey, viewModelKey);

    public static void RegisterWithLifetime<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel,
        TArg1, TArg2, TArg3>(
        this IServiceCollection services,
        Func<IServiceProvider, TArg1, TArg2, TArg3, TView> func,
        ServiceLifetime viewModelLifetime = ServiceLifetime.Transient,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient,
        object? viewKey = null,
        object? viewModelKey = null)
        where TView : class where TViewModel : class
        => services.RegisterPageCore<TView, TViewModel>(
            (sp, args) => func(sp, args.Resolve<TArg1>(0), args.Resolve<TArg2>(1), args.Resolve<TArg3>(2)),
            viewModelLifetime, viewLifetime, viewKey, viewModelKey);

    public static void RegisterWithLifetime<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel,
        TArg1, TArg2, TArg3, TArg4>(
        this IServiceCollection services,
        Func<IServiceProvider, TArg1, TArg2, TArg3, TArg4, TView> func,
        ServiceLifetime viewModelLifetime = ServiceLifetime.Transient,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient,
        object? viewKey = null,
        object? viewModelKey = null)
        where TView : class where TViewModel : class
        => services.RegisterPageCore<TView, TViewModel>(
            (sp, args) => func(sp, args.Resolve<TArg1>(0), args.Resolve<TArg2>(1), args.Resolve<TArg3>(2), args.Resolve<TArg4>(3)),
            viewModelLifetime, viewLifetime, viewKey, viewModelKey);

    public static void RegisterWithLifetime<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel,
        TArg1, TArg2, TArg3, TArg4, TArg5>(
        this IServiceCollection services,
        Func<IServiceProvider, TArg1, TArg2, TArg3, TArg4, TArg5, TView> func,
        ServiceLifetime viewModelLifetime = ServiceLifetime.Transient,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient,
        object? viewKey = null,
        object? viewModelKey = null)
        where TView : class where TViewModel : class
        => services.RegisterPageCore<TView, TViewModel>(
            (sp, args) => func(sp, args.Resolve<TArg1>(0), args.Resolve<TArg2>(1), args.Resolve<TArg3>(2), args.Resolve<TArg4>(3), args.Resolve<TArg5>(4)),
            viewModelLifetime, viewLifetime, viewKey, viewModelKey);

    public static void RegisterWithLifetime<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(
        this IServiceCollection services,
        ServiceLifetime viewModelLifetime = ServiceLifetime.Transient,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient,
        object? viewKey = null,
        object? viewModelKey = null)
        where TView : class where TViewModel : class
        => services.RegisterPageCore<TView, TViewModel>(
            ActivatorUtilities.CreateInstance<TView>, viewModelLifetime, viewLifetime, viewKey, viewModelKey);

    public static void Register<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(
        this IServiceCollection services, object? viewKey = null,
        object? viewModelKey = null)
        where TView : class where TViewModel : class
        => services.RegisterWithLifetime<TView, TViewModel>(
            ServiceLifetime.Singleton, ServiceLifetime.Singleton, viewKey, viewModelKey);

    private static void RegisterViewModel<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(
        this IServiceCollection services,
        object key,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TViewModel : class
        => services.RegisterFactory<TViewModel>(
            key, lifetime, ActivatorUtilities.CreateInstance<TViewModel>);

    public static T GetViewModel<T>(this IServiceProvider provider, object key, NavigableContext? context = null)
        => (T)GetViewModel(provider, key, context);

    public static object GetViewModel(this IServiceProvider provider, object key, NavigableContext? context = null)
    {
        var factory = provider.GetKeyedService<Func<IServiceProvider, object[], object>>(key)
            ?? throw new InvalidOperationException($"Unable to create {key}. Ensure that it is registered with the service provider.");

        return factory(provider, [.. context?.ViewModelParameters.Select(v => v.value).ToArray() ?? []])
            ?? throw new InvalidOperationException($"Factory for {key} returned null.");
    }

    public static object GetView(this IServiceProvider provider, object key, object[] context)
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

        if (resolvedKey is not object stringKey)
            throw new InvalidOperationException($"GetContextFor requires a key, got {resolvedKey?.GetType().Name ?? "null"}.");

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
        this IServiceProvider provider, NavigableContext context,
        object? viewKey = null)
        where TViewModel : ICloseableViewModel<TResult>
    {
        var view = provider.GetContextFor(viewKey ?? typeof(TViewModel).Name, context);
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