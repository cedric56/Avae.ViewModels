namespace Avae.ViewModels;

/// <summary>
/// Carries the parameters needed to construct a view, its view model, and its factory during navigation,
/// grouped by which stage of construction they apply to.
/// </summary>
public class NavigableContext
{
    /// <summary>
    /// Gets the combined set of all parameters, in order: <see cref="ViewParameters"/>, followed by <see cref="ViewModelParameters"/>.
    /// </summary>
    public object[] Parameters
    {
        get
        {
            var parameters = new List<object>();
            parameters.AddRange(ViewParameters);
            parameters.AddRange(ViewModelParameters);
            return [.. parameters];
        }
    }

    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the parameters passed to the view.
    /// </summary>
    public object[] ViewParameters { get; set; } = [];

    /// <summary>
    /// Gets or sets the parameters passed to the view model.
    /// </summary>
    public object[] ViewModelParameters { get; set; } = [];

    /// <summary>
    /// Gets the parameter at the specified index within the combined <see cref="Parameters"/> list, cast to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected type of the parameter.</typeparam>
    /// <param name="index">The zero-based index of the parameter within the combined <see cref="Parameters"/> list.</param>
    /// <returns>The parameter at <paramref name="index"/>, cast to <typeparamref name="T"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or beyond the number of available parameters.</exception>
    /// <exception cref="InvalidCastException">Thrown if the parameter at <paramref name="index"/> is not of type <typeparamref name="T"/>.</exception>
    public T Get<T>(int index)
    {
        var parameters = Parameters;
        if (index < 0 || index >= parameters.Length)
            throw new ArgumentOutOfRangeException(nameof(index),
                $"Expected parameter at index {index}, but only {parameters.Length} parameter(s) were provided.");

        if (parameters[index] is not T typed)
            throw new InvalidCastException(
                $"Parameter at index {index} is of type '{parameters[index]?.GetType().Name ?? "null"}', but '{typeof(T).Name}' was expected.");

        return typed;
    }

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
    public NavigableContext WithViewParameters(params object[] parameters)
    {
        ViewParameters = parameters;
        return this;
    }

    /// <summary>
    /// Sets <see cref="ViewModelParameters"/> and returns this instance, for fluent chaining.
    /// </summary>
    /// <param name="parameters">The parameters to pass to the view model.</param>
    /// <returns>This <see cref="NavigableContext"/> instance.</returns>
    public NavigableContext WithViewModelParameters(params object[] parameters)
    {
        ViewModelParameters = parameters;
        return this;
    }
}