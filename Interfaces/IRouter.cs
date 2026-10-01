namespace Avae.ViewModels;

public interface IRouter
{
    bool CanGoBack { get; }
    bool CanGoForward { get; }

    object? CurrentViewModel { get; }

    IViewFor? CurrentView { get; }

    Task<object?> BackAsync();
    Task<object?> ForwardAsync();

    Task<IViewFor?> GoTo(IViewFor view, object viewModel, NavigableContext? context = null);

    Task<(IViewFor? view, object viewmodel)> GoToType(
        Type viewModelType,
        object? viewKey = null,
        object? viewModelKey = null,
        NavigableContext? context = null);

    /// <summary>
    /// Navigates to the view associated with an existing view model instance.
    /// </summary>
    Task<(IViewFor? view, TViewModel viewmodel)> GoTo<TViewModel>(
        TViewModel viewModel,
        object? viewKey = null,
        object? viewModelKey = null,
        NavigableContext? context = null)
        where TViewModel : class;

    /// <summary>
    /// Navigates to the view associated with the specified view model type.
    /// </summary>
    Task<(IViewFor? view, TViewModel viewmodel)> GoTo<TViewModel>(
        object? viewKey = null,
        object? viewModelKey = null,
        NavigableContext? context = null)
        where TViewModel : class;
}
