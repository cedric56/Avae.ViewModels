namespace Avae.ViewModels;

public interface IDesktop
{
    Desktop Current { get; }
}

internal class DesktopImplementation : IDesktop
{
    private readonly Lazy<Desktop> _current = new(Detect);

    public Desktop Current => _current.Value;

    private static Desktop Detect()
    {
        if (OperatingSystem.IsAndroid()) return Desktop.Android;
        if (OperatingSystem.IsIOS()) return Desktop.iOS;
        if (OperatingSystem.IsTvOS()) return Desktop.TvOS;
        if (OperatingSystem.IsWatchOS()) return Desktop.WatchOS;
        if (OperatingSystem.IsWindows()) return Desktop.Windows;
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst()) return Desktop.Macos;
        if (OperatingSystem.IsLinux())
        {
            var xdgDesktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "";
            var desktopSession = Environment.GetEnvironmentVariable("DESKTOP_SESSION") ?? "";
            var combined = $"{xdgDesktop} {desktopSession}".ToUpperInvariant();
            if (combined.Contains("GNOME")) return Desktop.Gnome;
            if (combined.Contains("KDE")) return Desktop.KDE;
            if (combined.Contains("XFCE")) return Desktop.Xfce;
            if (combined.Contains("MATE")) return Desktop.Mate;
            if (combined.Contains("CINNAMON")) return Desktop.Cinnamon;
            if (combined.Contains("UNITY")) return Desktop.Unity;
            if (combined.Contains("LXDE")) return Desktop.LXDE;
        }
        return Desktop.Unknown;
    }
}

public readonly struct Desktop : IEquatable<Desktop>
{
    readonly string environment;

    /// <summary>
    /// Gets an instance of <see cref="DevicePlatform"/> that represents Android.
    /// </summary>
    public static Desktop Gnome { get; } = new Desktop(nameof(Gnome));
    public static Desktop Unknown { get; } = new Desktop(nameof(Unknown));
    public static Desktop KDE { get; } = new Desktop(nameof(KDE));
    public static Desktop Xfce { get; } = new Desktop(nameof(Xfce));
    public static Desktop Mate { get; } = new Desktop(nameof(Mate));
    public static Desktop Cinnamon { get; } = new Desktop(nameof(Cinnamon));
    public static Desktop WSL { get; } = new Desktop(nameof(WSL));
    public static Desktop Macos { get; } = new Desktop(nameof(Macos));
    public static Desktop Windows { get; } = new Desktop(nameof(Windows));
    public static Desktop iOS { get; } = new Desktop(nameof(iOS));
    public static Desktop Android { get; } = new Desktop(nameof(Android));
    public static Desktop Unity { get; } = new Desktop(nameof(Unity));
    public static Desktop LXDE { get; } = new Desktop(nameof(LXDE));
    public static Desktop TvOS { get; } = new Desktop(nameof(TvOS));
    public static Desktop WatchOS { get; } = new Desktop(nameof(WatchOS));

    internal Desktop(string environment)
    {
        if (environment == null)
            throw new ArgumentNullException(nameof(environment));

        if (environment.Length == 0)
            throw new ArgumentException(nameof(environment));

        this.environment = environment;
    }

    /// <summary>
    /// Creates a new device platform instance. This can be used to define your custom platforms.
    /// </summary>
    /// <param name="devicePlatform">The device platform identifier.</param>
    /// <returns>A new instance of <see cref="DevicePlatform"/> with the specified platform identifier.</returns>
    public static Desktop Create(string distribution) =>
        new Desktop(distribution);

    /// <summary>
    /// Compares the underlying <see cref="DevicePlatform"/> instances.
    /// </summary>
    /// <param name="other"><see cref="DevicePlatform"/> object to compare with.</param>
    /// <returns><see langword="true"/> if they are equal, otherwise <see langword="false"/>.</returns>
    public bool Equals(Desktop other) =>
        Equals(other.environment);

    internal bool Equals(string other) =>
        string.Equals(environment, other, StringComparison.Ordinal);

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public override bool Equals(object? obj) =>
        obj is Desktop && Equals((Desktop)obj);

    /// <summary>
    /// Gets the hash code for this platform instance.
    /// </summary>
    /// <returns>The computed hash code for this device platform or <c>0</c> when the device platform is <see langword="null"/>.</returns>
    public override int GetHashCode() =>
        environment == null ? 0 : environment.GetHashCode(
#if !NETSTANDARD2_0
                StringComparison.Ordinal
#endif
            );

    /// <summary>
    /// Returns a string representation of the current value of <see cref="distribution"/>.
    /// </summary>
    /// <returns>A string representation of this instance in the format of <c>{device platform}</c> or an empty string when no device platform is set.</returns>
    public override string ToString() =>
        environment ?? string.Empty;

    /// <summary>
    ///	Equality operator for equals.
    /// </summary>
    /// <param name="left">Left to compare.</param>
    /// <param name="right">Right to compare.</param>
    /// <returns><see langword="true"/> if objects are equal, otherwise <see langword="false"/>.</returns>
    public static bool operator ==(Desktop left, Desktop right) =>
        left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    /// <param name="left">Left to compare.</param>
    /// <param name="right">Right to compare.</param>
    /// <returns><see langword="true"/> if objects are not equal, otherwise <see langword="false"/>.</returns>
    public static bool operator !=(Desktop left, Desktop right) =>
        !left.Equals(right);
}
