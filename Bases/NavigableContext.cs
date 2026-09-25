namespace Avae.ViewModels;

/// <summary>
/// Carries the parameters needed to construct a view, its view model, and its factory during navigation,
/// grouped by which stage of construction they apply to.
/// </summary>
public class NavigableContext
{
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the parameters passed to the view.
    /// </summary>
    public (string key, object value)[] ViewParameters { get; set; } = [];

    /// <summary>
    /// Gets or sets the parameters passed to the view model.
    /// </summary>
    public (string key, object value)[] ViewModelParameters { get; set; } = [];


    public (string key, object value)[] Parameters { get; set; } = [];

    /// <summary>
    /// Creates a new, empty <see cref="NavigableContext"/>.
    /// </summary>
    /// <returns>A new <see cref="NavigableContext"/> instance.</returns>
    public static NavigableContext Create() => new();

    public NavigableContext WithKey(string key)
    {
        Key = key;
        return this;
    }

    /// <summary>
    /// Sets <see cref="ViewParameters"/> and returns this instance, for fluent chaining.
    /// </summary>
    /// <param name="parameters">The parameters to pass to the view.</param>
    /// <returns>This <see cref="NavigableContext"/> instance.</returns>
    public NavigableContext WithViewParameters(params (string key, object value)[] parameters)
    {
        ViewParameters = parameters;
        return this;
    }

    /// <summary>
    /// Sets <see cref="ViewModelParameters"/> and returns this instance, for fluent chaining.
    /// </summary>
    /// <param name="parameters">The parameters to pass to the view model.</param>
    /// <returns>This <see cref="NavigableContext"/> instance.</returns>
    public NavigableContext WithViewModelParameters(params (string key, object value)[] parameters)
    {
        ViewModelParameters = parameters;
        return this;
    }

    public NavigableContext WithAdditionalParameters(params (string key, object value)[] parameters)
    {
        Parameters = parameters;
        return this;
    }

    /// <summary>
    /// Sets <see cref="ViewParameters"/> and returns this instance, for fluent chaining.
    /// </summary>
    /// <param name="parameters">The parameters to pass to the view.</param>
    /// <returns>This <see cref="NavigableContext"/> instance.</returns>
    public NavigableContext WithViewParameters(params object[] parameters)
    {
        ViewParameters = parameters.Select(p => (p.GetType().Name, p)).ToArray();
        return this;
    }

    /// <summary>
    /// Sets <see cref="ViewModelParameters"/> and returns this instance, for fluent chaining.
    /// </summary>
    /// <param name="parameters">The parameters to pass to the view model.</param>
    /// <returns>This <see cref="NavigableContext"/> instance.</returns>
    public NavigableContext WithViewModelParameters(params object[] parameters)
    {
        ViewModelParameters = parameters.Select(p => (p.GetType().Name, p)).ToArray();
        return this;
    }

    public NavigableContext WithAdditionalParameters(params object[] parameters)
    {
        Parameters = parameters.Select(p => (p.GetType().Name, p)).ToArray();
        return this;
    }
}