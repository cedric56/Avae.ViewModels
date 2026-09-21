namespace Avae.ViewModels;

/// <summary>
/// Class initially copied from https://github.com/eten-tech/bible-well/blob/main/src/BibleWell.App/Router.cs
/// </summary>
public partial class Router(IServiceProvider provider)
{
    private int _currentIndex = -1;
    private List<(object viewmodel, IViewFor view, NavigableContext context)> _history = [];
    private const uint MaxHistorySize = 20;

    /// <summary>
    /// Gets a value indicating whether there is history to navigate back to.
    /// </summary>
    public bool CanGoBack => _currentIndex > 0;

    /// <summary>
    /// Gets a value indicating whether there is history to navigate forward to.
    /// </summary>
    public bool CanGoForward => _history.Count > 0 && _currentIndex < _history.Count - 1;

    /// <summary>
    /// Gets the view model currently at the front of navigation history, or <see langword="null"/> if history is empty.
    /// </summary>
    public object? CurrentViewModel => _currentIndex < 0 ? null : _history[_currentIndex].viewmodel;

    public IViewFor? CurrentView => _currentIndex < 0 ? null : _history[_currentIndex].view;

    /// <summary>
    /// Occurs whenever the current view model changes, whether via <see cref="Back"/>, <see cref="Forward"/>,
    /// or one of the <c>GoTo</c> overloads.
    /// </summary>
    public event Action<object>? CurrentViewModelChanged;

    /// <summary>
    /// Clears all navigation history and resets the current position.
    /// </summary>
    public void EraseHistory()
    {
        _currentIndex = -1;
        _history.Clear();
    }

    public async Task<object?> BackAsync()
    {
        if (!CanGoBack) return null;

        var leaving = _history[_currentIndex];
        if (leaving.viewmodel is INavigable confirm && !await confirm.CanNavigateAsync())
            return null;

        _currentIndex--;
        await TransitionTo(leaving, _history[_currentIndex]);
        CurrentViewModelChanged?.Invoke(CurrentViewModel!);
        return CurrentViewModel;
    }

    public async Task<object?> ForwardAsync()
    {
        if (!CanGoForward) return null;

        var leaving = _history[_currentIndex];
        if (leaving.viewmodel is INavigable confirm && !await confirm.CanNavigateAsync())
            return null;

        _currentIndex++;
        await TransitionTo(leaving, _history[_currentIndex]);
        CurrentViewModelChanged?.Invoke(CurrentViewModel!);
        return CurrentViewModel;
    }

    private async Task TransitionTo((object viewmodel, object view, NavigableContext context) leaving, (object viewmodel, object view, NavigableContext context) entering)
    {        
        if (leaving.viewmodel is INavigable lvm) await lvm.OnNavigatedFrom(leaving.context);
        if (entering.viewmodel is INavigable evm) await evm.OnNavigatedTo(entering.context);
        if (leaving.view is INavigable lv) await lv.OnNavigatedFrom(leaving.context);
        if (entering.view is INavigable ev) await ev.OnNavigatedTo(entering.context);
    }

    async Task<IViewFor?> GoToCore(object key, object viewModel, NavigableContext? context = null)
    {
        var view = provider.GetContextFor(key, context) ?? throw new InvalidOperationException($"Unable to resolve view for {key}.");
        return await GoTo(view, viewModel, context);
    }

    public async Task<IViewFor?> GoTo(IViewFor view, object viewModel, NavigableContext? context = null)
    {
        if (CurrentViewModel is INavigable confirm && !await confirm.CanNavigateAsync())
            return null;

        context ??= new NavigableContext();
        view.Context = viewModel;       
        var previous = _currentIndex >= 0 ? _history[_currentIndex] : default;        
        await TransitionTo(previous, (viewModel, view, context));
        AddHistory(viewModel, view, context);
        CurrentViewModelChanged?.Invoke(viewModel);        
        return view;
    }

    /// <summary>
    /// Navigates to the view associated with the specified view model type.
    /// If you directly know the type of the view model at compile time, use <see cref="GoTo{T}()"/> instead.
    /// </summary>
    /// <typeparam name="TBaseType">The base type of the view model.</typeparam>
    /// <param name="viewModelType">The view model type.</param>
    /// <returns>The created view model cast to the <typeparamref name="TBaseType"/>.</returns>        
    public Task<IViewFor?> GoToType(Type viewModelType, out object viewModel, string? key = null, NavigableContext? context = null)
    {
        viewModel = provider.GetViewModel(viewModelType, context);
        return GoToCore(key ?? viewModelType.Name, viewModel, context);
    }

    /// <summary>
    /// Navigates to the view associated with the specified view model type, without exposing the created view model.
    /// </summary>
    /// <param name="viewModelType">The view model type to navigate to.</param>
    /// <param name="context">Optional navigation context supplying parameters for the view model, view, and factory.</param>
    /// <returns>The view resolved for the created view model.</returns>
    public Task<IViewFor?> GoToType(Type viewModelType, string? key = null, NavigableContext? context = null)
    {
        return GoToType(viewModelType, out var _, key, context);
    }

    /// <summary>
    /// Navigates to the view associated with an already-created view model instance.
    /// </summary>
    /// <typeparam name="TViewModel">The type of the view model.</typeparam>
    /// <param name="viewModel">The existing view model instance to navigate to.</param>
    /// <param name="context">Optional navigation context supplying parameters for the view.</param>
    /// <returns>The view resolved for <paramref name="viewModel"/>.</returns>
    public Task<IViewFor?> GoTo<TViewModel>(TViewModel viewModel, string? key = null, NavigableContext? context = null) where TViewModel : class
    {
        if (viewModel == null)
            throw new InvalidOperationException("Viewmodel must not be null");
        return GoToCore(key ?? typeof(TViewModel).Name, viewModel, context);
    }

    /// <summary>
    /// Navigates to the view associated with the specified view model type.
    /// </summary>
    /// <typeparam name="TViewModel">The type of the view model.</typeparam>
    /// <returns>The created view model.</returns>
    public Task<IViewFor?> GoTo<TViewModel>(out TViewModel viewModel, string? key = null, NavigableContext? context = null) where TViewModel : class
    {
        viewModel = provider.GetViewModel<TViewModel>(context)!;
        return GoToCore(key ?? typeof(TViewModel).Name, viewModel, context);
    }

    /// <summary>
    /// Appends a view model to navigation history and makes it current, truncating any "forward" history
    /// beyond the current position and trimming the oldest entry if <see cref="MaxHistorySize"/> is exceeded.
    /// </summary>
    /// <param name="item">The view model to add to history.</param>
    public void AddHistory(object viewmodel, IViewFor view, NavigableContext context)
    {
        // After navigating back the current index may not be the most forward position.
        // Delete all "forward" items in the history when this happens.
        if (CanGoForward)
        {
            _history = [.. _history.Take(_currentIndex + 1)];
        }

        // add the item and recalculate the index
        _history.Add((viewmodel,  view, context));

        // history exceeded the max size
        if (_history.Count > MaxHistorySize)
        {
            _history.RemoveAt(0);
        }

        _currentIndex = _history.Count - 1;
    }
}