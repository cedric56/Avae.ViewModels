namespace Avae.ViewModels;

/// <summary>
/// Base class for a view model that supports back/forward navigation through a <see cref="Router"/>'s history.
/// </summary>
public abstract class RouterViewModelBase
{
    /// <summary>
    /// The router used to navigate backward and forward through view model history.
    /// </summary>
    protected Router _router;

    /// <summary>
    /// Initializes a new instance of the <see cref="RouterViewModelBase"/> class.
    /// </summary>
    /// <param name="router">The router used to manage navigation history.</param>
    public RouterViewModelBase(Router router)
    {
        _router = router;
    }

    /// <summary>
    /// Navigates to the previous view model in the router's history, if available.
    /// </summary>
    public virtual async Task GoBack()
    {
        if (CanGoBack())
        {
            var viewModel = await _router.BackAsync();
            OnViewModelChanged(viewModel);
        }
    }

    /// <summary>
    /// Determines whether backward navigation is currently possible.
    /// </summary>
    /// <returns><see langword="true"/> if there is history to go back to; otherwise, <see langword="false"/>.</returns>
    public bool CanGoBack()
    {
        return _router.CanGoBack;
    }

    /// <summary>
    /// Navigates to the next view model in the router's history, if available.
    /// </summary>
    public virtual async Task GoForward()
    {
        if (CanGoForward())
        {
            var viewModel = await _router.ForwardAsync();
            OnViewModelChanged(viewModel);
        }
    }

    /// <summary>
    /// Determines whether forward navigation is currently possible.
    /// </summary>
    /// <returns><see langword="true"/> if there is history to go forward to; otherwise, <see langword="false"/>.</returns>
    public bool CanGoForward()
    {
        return _router.CanGoForward;
    }

    /// <summary>
    /// Called whenever navigation changes the active view model, e.g. via <see cref="GoBack"/> or <see cref="GoForward"/>.
    /// The base implementation refreshes command availability.
    /// </summary>
    /// <param name="viewModel">The view model that has become active.</param>
    protected virtual void OnViewModelChanged(object? viewModel)
    {
        RaiseCanExecutesChanged();
    }

    /// <summary>
    /// When implemented in a derived class, notifies the UI that the executability of commands
    /// (e.g. those depending on <see cref="CanGoBack"/> or <see cref="CanGoForward"/>) may have changed.
    /// </summary>
    protected abstract void RaiseCanExecutesChanged();
}