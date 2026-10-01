namespace Avae.ViewModels;

/// <summary>
/// Provides ViewModel-first navigation with history and lifecycle callbacks.
/// Navigation operations are serialized per router instance.
/// </summary>
internal class Router(
    NavigationHistory history,
    NavigationGate gate,
    NavigationLifecycle lifecycle,
    IServiceProvider provider) : IRouter
{
    public bool CanGoBack => history.CanGoBack;
    public bool CanGoForward => history.CanGoForward;
    public object? CurrentViewModel => history.Current?.ViewModel;
    public IViewFor? CurrentView => history.Current?.View;

    public async Task EraseHistoryAsync()
    {
        await gate.EnterAsync();
        try { history.Clear(); }
        finally { gate.Exit(); }
    }

    public async Task<object?> BackAsync()
    {
        await gate.EnterAsync();
        try
        {
            var leaving = history.Current;
            if (leaving is null) return null;

            var target = history.PeekBack();
            if (target is null) return null;

            if (!await lifecycle.CanLeaveAsync(leaving)) return null;

            await lifecycle.TransitionAsync(leaving, target);

            history.MoveBack();
            return history.Current!.ViewModel;
        }
        finally { gate.Exit(); }
    }

    public async Task<IViewFor?> GoTo(IViewFor view, object viewModel, NavigableContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(viewModel);

        await gate.EnterAsync();
        var previousContext = view.Context;

        try
        {
            context ??= new NavigableContext();

            var previous = history.Current;
            if (previous is not null && !await lifecycle.CanLeaveAsync(previous))
                return null;

            view.Context = viewModel;

            var entering = new NavigationEntry(viewModel, view, context);

            if (previous is not null)
                await lifecycle.TransitionAsync(previous, entering);
            else
                await lifecycle.TransitionAsync(new NavigationEntry(viewModel, view, context), entering);

            history.Push(entering);
            return view;
        }
        catch
        {
            view.Context = previousContext;
            throw;
        }
        finally { gate.Exit(); }
    }

    public async Task<object?> ForwardAsync()
    {
        await gate.EnterAsync();

        try
        {
            var leaving = history.Current;
            if (leaving is null)
                return null;

            var target = history.PeekForward();
            if (target is null)
                return null;

            if (!await lifecycle.CanLeaveAsync(leaving))
                return null;

            await lifecycle.TransitionAsync(leaving, target);

            history.MoveForward();
            return history.Current!.ViewModel;
        }
        finally
        {
            gate.Exit();
        }
    }

    private async Task<IViewFor?> GoToCore(        
        object viewModel,
        object viewKey,
        NavigableContext? context = null,        
        object? viewModelKey = null)
    {
        var view = provider.GetContextFor(viewKey, context)
            ?? throw new InvalidOperationException(
                $"Unable to resolve view for {viewKey}.");

        return await GoTo(view, viewModel, context);
    }

    /// <summary>
    /// Navigates to the view associated with the specified view model type.
    /// </summary>
    public async Task<(IViewFor? view, object viewmodel)> GoToType(
        Type viewModelType,
        object? viewKey = null,
        object? viewModelKey = null,
        NavigableContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);

        var keys = ViewKey.GetKeys(viewKey, viewModelKey, viewModelType);
        viewKey = keys.viewKey;
        viewModelKey = keys.viewModelKey;

        var viewModel = provider.GetViewModel(viewModelKey, context);

        return (
            await GoToCore(
                viewModel,
                viewKey,
                context,
                viewModelKey),
            viewModel);
    }

    /// <summary>
    /// Navigates to the view associated with an existing view model instance.
    /// </summary>
    public async Task<(IViewFor? view, TViewModel viewmodel)> GoTo<TViewModel>(
        TViewModel viewModel,
        object? viewKey = null,
        object? viewModelKey = null,
        NavigableContext? context = null)
        where TViewModel : class
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        var keys = ViewKey.GetKeys(viewKey, viewModelKey, typeof(TViewModel));
        viewKey = keys.viewKey;
        viewModelKey = keys.viewModelKey;

        return (
            await GoToCore(
                viewModel,
                viewKey,
                context,
                viewModelKey),
            viewModel);
    }

    /// <summary>
    /// Navigates to the view associated with the specified view model type.
    /// </summary>
    public async Task<(IViewFor? view, TViewModel viewmodel)> GoTo<TViewModel>(
        object? viewKey = null,
        object? viewModelKey = null,
        NavigableContext? context = null)
        where TViewModel : class
    {
        var keys = ViewKey.GetKeys(viewKey, viewModelKey, typeof(TViewModel));
        viewKey = keys.viewKey;
        viewModelKey = keys.viewModelKey;

        var viewModel = provider.GetViewModel<TViewModel>(viewModelKey, context)
            ?? throw new InvalidOperationException(
                $"Unable to create {typeof(TViewModel).Name}.");

        return (
            await GoToCore(                
                viewModel,
                viewKey,
                context,
                viewModelKey),
            viewModel);
    }
}