namespace Avae.ViewModels;

public interface IRuntime
{
    Runtime Current { get; }
}

internal class RuntimeImplementation(Runtime runtime) : IRuntime
{
    public Runtime Current { get; } = runtime;
}

public readonly struct Runtime : IEquatable<Runtime>
{
    readonly string distribution;

    /// <summary>
    /// Gets an instance of <see cref="DevicePlatform"/> that represents Android.
    /// </summary>
    public static Runtime Avalonia { get; } = new Runtime(nameof(Avalonia));
    public static Runtime Maui { get; } = new Runtime(nameof(Maui));
    public static Runtime MauiHybrid { get; } = new Runtime(nameof(MauiHybrid));
    public static Runtime BlazorServer { get; } = new Runtime(nameof(BlazorServer));
    public static Runtime BlazorWebAssembly { get; } = new Runtime(nameof(BlazorWebAssembly));
    public static Runtime Unknown { get; } = new Runtime(nameof(Unknown));

    internal Runtime(string distribution)
    {
        if (distribution == null)
            throw new ArgumentNullException(nameof(distribution));

        if (distribution.Length == 0)
            throw new ArgumentException(nameof(distribution));

        this.distribution = distribution;
    }

    /// <summary>
    /// Creates a new device platform instance. This can be used to define your custom platforms.
    /// </summary>
    /// <param name="devicePlatform">The device platform identifier.</param>
    /// <returns>A new instance of <see cref="DevicePlatform"/> with the specified platform identifier.</returns>
    public static Runtime Create(string distribution) =>
        new Runtime(distribution);

    /// <summary>
    /// Compares the underlying <see cref="DevicePlatform"/> instances.
    /// </summary>
    /// <param name="other"><see cref="DevicePlatform"/> object to compare with.</param>
    /// <returns><see langword="true"/> if they are equal, otherwise <see langword="false"/>.</returns>
    public bool Equals(Runtime other) =>
        Equals(other.distribution);

    internal bool Equals(string other) =>
        string.Equals(distribution, other, StringComparison.Ordinal);

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public override bool Equals(object? obj) =>
        obj is Runtime && Equals((Runtime)obj);

    /// <summary>
    /// Gets the hash code for this platform instance.
    /// </summary>
    /// <returns>The computed hash code for this device platform or <c>0</c> when the device platform is <see langword="null"/>.</returns>
    public override int GetHashCode() =>
        distribution == null ? 0 : distribution.GetHashCode(
#if !NETSTANDARD2_0
                StringComparison.Ordinal
#endif
            );

    /// <summary>
    /// Returns a string representation of the current value of <see cref="distribution"/>.
    /// </summary>
    /// <returns>A string representation of this instance in the format of <c>{device platform}</c> or an empty string when no device platform is set.</returns>
    public override string ToString() =>
        distribution ?? string.Empty;

    /// <summary>
    ///	Equality operator for equals.
    /// </summary>
    /// <param name="left">Left to compare.</param>
    /// <param name="right">Right to compare.</param>
    /// <returns><see langword="true"/> if objects are equal, otherwise <see langword="false"/>.</returns>
    public static bool operator ==(Runtime left, Runtime right) =>
        left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    /// <param name="left">Left to compare.</param>
    /// <param name="right">Right to compare.</param>
    /// <returns><see langword="true"/> if objects are not equal, otherwise <see langword="false"/>.</returns>
    public static bool operator !=(Runtime left, Runtime right) =>
        !left.Equals(right);
}

