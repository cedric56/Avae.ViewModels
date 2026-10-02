using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace Avae.ViewModels;

/// <summary>
/// Base class for a navigable view model that can be closed and return a result value.
/// </summary>
/// <typeparam name="TResult">The type of the result produced when this view model is closed.</typeparam>
/// <param name="router">The router used to manage navigation between views.</param>
/// <param name="initialize">
/// If <see langword="true"/>, the first available navigable item is selected automatically on construction.
/// </param>
public abstract partial class NavigableViewModelBase<TResult>(IRouter router, bool initialize = true) :
    NavigableViewModelBase(router, initialize),
    ICloseableViewModel<TResult>    
{
    /// <summary>
    /// Occurs when a close has been requested for this view model, carrying the resulting value, if any.
    /// </summary>
    public event EventHandler<TResult?>? CloseRequested;

    /// <summary>
    /// Gets the display title for this view model.
    /// </summary>
    public abstract string Title { get; }

    /// <summary>
    /// Determines whether this view model can currently be closed.
    /// </summary>
    /// <returns>
    /// A task that resolves to <see langword="true"/> if closing is allowed; otherwise, <see langword="false"/>.
    /// The base implementation always returns <see langword="true"/>.
    /// </returns>
    public virtual Task<bool> CanClose() => Task.FromResult(true);

    /// <summary>
    /// Gets the command that, when executed, checks <see cref="CanClose"/> and closes the view model if allowed.
    /// </summary>
    public abstract ICommand CloseCommand { get; }

    /// <summary>
    /// Gets the collection of commands exposed by this view model. By default, contains only the close command.
    /// </summary>
    public virtual ObservableCollection<NamedCommand> Commands => [new() { Command = CloseCommand, Name = "Close" }];

    /// <summary>
    /// Closes this view model, raising <see cref="CloseRequested"/> with the supplied result value.
    /// </summary>
    /// <param name="value">The result value to pass to subscribers of <see cref="CloseRequested"/>.</param>
    /// <returns>A completed task.</returns>
    public Task Close(TResult? value)
    {

        CloseRequested?.Invoke(this, value);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Base class for a view model that manages navigation between a set of <see cref="NavigableView"/> items,
/// caching the view/view-model pair for each one as it is visited.
/// </summary>
public abstract partial class NavigableViewModelBase : RouterViewModelBase, IDisposable, INavigable
{
    /// <summary>
    /// Occurs when the currently displayed view changes.
    /// </summary>
    public EventHandler<IViewFor?>? CurrentViewChanged;

    /// <summary>
    /// Updates <see cref="SelectedNavigable"/> and <see cref="CurrentView"/> to reflect a change in the
    /// active view model, and raises the corresponding change notifications.
    /// </summary>
    /// <param name="viewModel">The view model that has become active.</param>
    protected override void OnViewModelChanged(object? viewModel)
    {
        if (viewModel == null)
            return;

        var type = viewModel.GetType();
        _selectedNavigable = Navigables.FirstOrDefault(p => p.ViewModelType == type);
        if (_selectedNavigable != null && 
            dico.TryGetValue(_selectedNavigable, out var context))
        {
            _currentView = context.view;
        }
        NotifyPropertyChanged(nameof(SelectedNavigable));
        NotifyPropertyChanged(nameof(CurrentView));
        CurrentViewChanged?.Invoke(this, _currentView);
        base.OnViewModelChanged(viewModel);
    }

    /// <summary>
    /// Raises a property-changed notification for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected abstract void NotifyPropertyChanged(string propertyName);

    /// <summary>
    /// Cache mapping each <see cref="NavigableView"/> to the view/view-model pair created for it,
    /// so that previously visited navigables are not recreated.
    /// </summary>
    private readonly Dictionary<NavigableView, (IViewFor view, object viewmodel)> dico = [];

    private IViewFor? _currentView = null!;

    /// <summary>
    /// Gets or sets the view currently being displayed. Setting this property raises
    /// <see cref="CurrentViewChanged"/> and a property-changed notification.
    /// </summary>
    public IViewFor? CurrentView
    {
        get { return _currentView; }
        set
        {
            _currentView = value;
            NotifyPropertyChanged(nameof(CurrentView));
            CurrentViewChanged?.Invoke(this, _currentView);
        }
    }

    private NavigableView? _selectedNavigable;

    private readonly SemaphoreSlim _selectionGate = new(1, 1);
    private int _selectionVersion;

    public NavigableView? SelectedNavigable
    {
        get => _selectedNavigable;
        set
        {
            if (Equals(_selectedNavigable, value))
                return;
            var old = _selectedNavigable;
            _selectedNavigable = value;

            // Fire-and-forget for XAML two-way binding (the setter itself can't be async).
            // SelectAsync below does the actual serialized, version-guarded work; callers that
            // need to await completion or observe failure should call SelectAsync directly
            // instead of going through this property.
            _ = SelectAsync(value, old);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigableViewModelBase"/> class.
    /// </summary>
    /// <param name="router">The router used to manage navigation between views.</param>
    /// <param name="initialize">
    /// If <see langword="true"/>, <see cref="SelectedNavigable"/> is set to the first available
    /// navigable item on construction.
    /// </param>
    public NavigableViewModelBase(IRouter router, bool initialize = true)
        : base(router)
    {
        if (initialize)
        {
            _selectedNavigable = Navigables.FirstOrDefault();
            _ = SelectAsync(_selectedNavigable, null);
        }
    }

    /// <summary>
    /// Selects <paramref name="value"/> and awaits the resulting navigation. Serialized against
    /// every other call to <see cref="SelectAsync"/> (including ones triggered by the
    /// <see cref="SelectedNavigable"/> setter), and version-guarded so that if a newer selection
    /// is requested while this one is still in flight (e.g. waiting on the gate, or awaiting
    /// <see cref="GetView"/>), this call's result is discarded rather than applied out of order
    /// — the newer selection always wins, regardless of which one actually finishes first.
    /// </summary>
    public async Task SelectAsync(NavigableView? value, NavigableView? old = null)
    {
        var version = Interlocked.Increment(ref _selectionVersion);

        await _selectionGate.WaitAsync().ConfigureAwait(false);
        try
        {
            // A newer selection was requested while we were waiting for the gate — our work
            // is already stale before it even started, so don't touch any state.
            if (version != _selectionVersion)
                return;

            await OnSelectedNavigableChangedAsync(value, old, version).ConfigureAwait(false);
        }
        finally
        {
            _selectionGate.Release();
        }
    }

    private ObservableCollection<NavigableView>? _navigables;

    /// <summary>
    /// Gets the collection of navigable items available to this view model, lazily populated
    /// via <see cref="GetNavigables"/> on first access.
    /// </summary>
    public ObservableCollection<NavigableView> Navigables { get { return _navigables ??= GetNavigables(); } }

    /// <summary>
    /// When implemented in a derived class, returns the collection of navigable items this view model exposes.
    /// </summary>
    /// <returns>The collection of available <see cref="NavigableView"/> items.</returns>
    protected abstract ObservableCollection<NavigableView> GetNavigables();

    /// <summary>
    /// Handles a change to <see cref="SelectedNavigable"/> by resolving (or creating) the associated
    /// view/view-model pair, updating <see cref="CurrentView"/>, and recording navigation history.
    /// </summary>
    /// <param name="value">The newly selected navigable item, or <see langword="null"/> if none is selected.</param>
    /// <param name="old">The previously selected item, restored on failure.</param>
    /// <param name="version">
    /// The selection version this call is running under, as assigned by <see cref="SelectAsync"/>.
    /// Checked again after the awaited work completes so that a result superseded by a newer
    /// selection (requested while this one was still awaiting <see cref="GetView"/>) is discarded
    /// instead of overwriting the newer selection's already-applied state.
    /// </param>
    public virtual async Task OnSelectedNavigableChangedAsync(NavigableView? value, NavigableView? old, int version = 0)
    {
        try
        {
            if (value == null)
                return;

            var view = await GetView(value);

            // A newer selection superseded this one while GetView was in flight — its result
            // has already been applied (or is about to be), so don't overwrite it with a
            // slower, now-stale result.
            if (version != 0 && version != _selectionVersion)
                return;

            if (view != null)
            {
                CurrentView = view;
            }
            else
            {
                _selectedNavigable = old;
            }
        }
        catch
        {
            if (version == 0 || version == _selectionVersion)
                _selectedNavigable = old;

            throw;
        }
        finally
        {
            if (version == 0 || version == _selectionVersion)
            {
                RaiseCanExecutesChanged();
                NotifyPropertyChanged(nameof(SelectedNavigable));
            }
        }
    }

    protected virtual async Task<IViewFor?> GetView(NavigableView value)
    {
        if (dico.TryGetValue(value, out var context))
        {
            var view = await _router.GoTo(context.view, context.viewmodel, value.Context);
            if (view != null)
            {
                return view;
            }
        }
        else
        {
            var result = await GoTo(value);
            if (result.view != null)
            {
                dico.Add(value, (result.view, result.viewmodel));
                return result.view;
            }
        }

        return null;
    }

    /// <summary>
    /// Navigates to the view associated with the specified navigable item, creating its view model
    /// if one is not already assigned.
    /// </summary>
    /// <param name="value">The navigable item to navigate to.</param>
    /// <param name="viewModel">
    /// When this method returns, contains the view model used for navigation—either <paramref name="value"/>'s
    /// existing view model, or a newly created one.
    /// </param>
    /// <returns>The view resolved for the navigation target.</returns>
    private async Task<(IViewFor? view, object viewmodel)> GoTo(NavigableView value)
    {
        if (value.ViewModel == null)
            return await _router.GoToType(value.ViewModelType, context: value.Context);
        else
            return await _router.GoTo(value.ViewModel, context: value.Context);
    }

    public virtual void Dispose()
    {
        foreach (var pair in dico.Values)
        {
            (pair.viewmodel as IDisposable)?.Dispose();
            (pair.view as IDisposable)?.Dispose();
        }
        dico.Clear();

        _navigables?.Clear();
        _navigables = null;
    }
}