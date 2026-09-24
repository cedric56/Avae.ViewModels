namespace Avae.ViewModels;

/// <summary>
/// Represents a navigable page in the application, pairing a view model type with the display
/// information needed to present it (name, icon/source, and launch behavior).
/// </summary>
/// <param name="viewModelType">The type of the view model associated with this navigable page.</param>
/// <param name="displayName">The display name shown for this page, e.g. in a navigation menu.</param>
/// <param name="path">
/// An optional path used to resolve the page's icon and source via <see cref="IconResolver"/>.
/// If <see langword="null"/> or whitespace, <see cref="Icon"/> and <see cref="Source"/> return <see langword="null"/>.
/// </param>
public class NavigableView(Type viewModelType, string displayName, string? path = null)
{
    /// <summary>
    /// Gets the view model instance associated with this page, if one has been explicitly assigned.
    /// </summary>
    public object? ViewModel { get; protected set; }

    /// <summary>
    /// Gets the type of the view model associated with this page.
    /// </summary>
    public Type ViewModelType { get; } = viewModelType;

    /// <summary>
    /// Gets the display name for this page.
    /// </summary>
    public string DisplayName { get; } = displayName;

    /// <summary>
    /// Gets the path used to resolve this page's icon and source.
    /// </summary>
    public string? Path { get; } = path;

    /// <summary>
    /// Gets the icon resolved from <see cref="Path"/>, or <see langword="null"/> if no path is set.
    /// </summary>
    public object? Icon
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Path)) return null;
            return IconResolver.GetIcon(Path);
        }
    }

    /// <summary>
    /// Gets the source resolved from <see cref="Path"/>, or <see langword="null"/> if no path is set.
    /// </summary>
    public object? Source
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Path)) return null;
            return IconResolver.GetSource(Path);
        }
    }

    /// <summary>
    /// Gets or sets contextual data passed along when navigating to this page.
    /// </summary>
    public NavigableContext Context { get; set; } = new NavigableContext();
}

/// <summary>
/// Strongly typed variant of <see cref="NavigableView"/> whose view model is known to be of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The view model type associated with this page.</typeparam>
public class NavigableView<T> : NavigableView //where T : IViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NavigableView{T}"/> class with no pre-existing view model.
    /// </summary>
    /// <param name="displayName">The display name shown for this page.</param>
    /// <param name="icon">
    /// An optional path used to resolve the page's icon and source via <see cref="IconResolver"/>.
    /// </param>
    public NavigableView(string displayName, string? icon = null)
        : base(typeof(T), displayName, icon)
    {

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigableView{T}"/> class using an existing view model.
    /// </summary>
    /// <param name="viewModel">The existing view model instance to associate with this page.</param>
    /// <param name="displayName">The display name shown for this page.</param>
    /// <param name="icon">
    /// An optional path used to resolve the page's icon and source via <see cref="IconResolver"/>.
    /// </param>
    public NavigableView(T viewModel, string displayName, string? icon = null)
        : base(typeof(T), displayName, icon)
    {
        ViewModel = viewModel;
    }
}