namespace Avae.ViewModels;

public interface IDistribution
{
    Distribution Current { get; }
}

internal class DistributionImplementation : IDistribution
{
    private readonly Lazy<Distribution> _current = new(Detect);

    public Distribution Current => _current.Value;

    public static Distribution Detect()
    {
        if(OperatingSystem.IsAndroid())
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(36)) return Distribution.Android16;
            if (OperatingSystem.IsAndroidVersionAtLeast(35)) return Distribution.Android15;
            if (OperatingSystem.IsAndroidVersionAtLeast(34)) return Distribution.Android14;
            if (OperatingSystem.IsAndroidVersionAtLeast(33)) return Distribution.Android13;
            if (OperatingSystem.IsAndroidVersionAtLeast(32)) return Distribution.Android12L;
            if (OperatingSystem.IsAndroidVersionAtLeast(31)) return Distribution.Android12;
            if (OperatingSystem.IsAndroidVersionAtLeast(30)) return Distribution.Android11;
            if (OperatingSystem.IsAndroidVersionAtLeast(29)) return Distribution.Android10;
            if (OperatingSystem.IsAndroidVersionAtLeast(28)) return Distribution.Android9;
            if (OperatingSystem.IsAndroidVersionAtLeast(27)) return Distribution.Android8_1;
            if (OperatingSystem.IsAndroidVersionAtLeast(26)) return Distribution.Android8;
            if (OperatingSystem.IsAndroidVersionAtLeast(25)) return Distribution.Android7_1;
            if (OperatingSystem.IsAndroidVersionAtLeast(24)) return Distribution.Android7;
            if (OperatingSystem.IsAndroidVersionAtLeast(23)) return Distribution.Android6;
            if (OperatingSystem.IsAndroidVersionAtLeast(22)) return Distribution.Android5_1;
            if (OperatingSystem.IsAndroidVersionAtLeast(21)) return Distribution.Android5;
        }

        if (OperatingSystem.IsWindows())
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return Distribution.Windows11;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) return Distribution.Windows10;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240)) return Distribution.Windows10;
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 3, 9600)) return Distribution.Windows81;
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 2, 9200)) return Distribution.Windows8;
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1, 7600)) return Distribution.Windows7;
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 0)) return Distribution.Vista;
            if (OperatingSystem.IsWindowsVersionAtLeast(5, 1)) return Distribution.WindowsXP;
        }

        if (OperatingSystem.IsLinux())
        {
            var osRelease = ReadOsRelease();
            var id = Get(osRelease, "ID");
            var idLike = Get(osRelease, "ID_LIKE");
            if (Matches(id, idLike, "ubuntu")) return Distribution.Ubuntu;
            if (Matches(id, idLike, "fedora")) return Distribution.Fedora;
            if (Matches(id, idLike, "arch")) return Distribution.ArchLinux;
            if (Matches(id, idLike, "debian")) return Distribution.Debian;
            if (Matches(id, idLike, "opensuse", "opensuse-leap", "opensuse-tumbleweed", "suse")) return Distribution.OpenSUSE;
            if (Matches(id, idLike, "linuxmint", "mint")) return Distribution.LinuxMint;
            if (Matches(id, idLike, "manjaro")) return Distribution.Manjaro;
            if (Matches(id, idLike, "pop", "pop_os")) return Distribution.PopOS;
        }

        return Distribution.Unknown;
    }

    private static Dictionary<string, string> ReadOsRelease()
    {
        const string path = "/etc/os-release";

        if (!File.Exists(path))
            return new Dictionary<string, string>();

        return File.ReadAllLines(path)
            .Select(ParseLine)
            .Where(x => x is not null)
            .ToDictionary(x => x!.Value.Key, x => x!.Value.Value);
    }

    private static KeyValuePair<string, string>? ParseLine(string line)
    {
        var separator = line.IndexOf('=');

        if (separator <= 0)
            return null;

        var key = line[..separator];
        var value = line[(separator + 1)..].Trim();

        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') ||
             (value[0] == '\'' && value[^1] == '\'')))
        {
            value = value[1..^1];
        }

        return new(key, value);
    }

    private static string Get(
        Dictionary<string, string> values,
        string key)
    {
        return values.TryGetValue(key, out var value)
            ? value.ToLowerInvariant()
            : string.Empty;
    }

    private static bool Matches(
        string id,
        string idLike,
        params string[] values)
    {
        var identifiers = $"{id} {idLike}"
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return values.Any(value =>
            identifiers.Contains(
                value,
                StringComparer.OrdinalIgnoreCase));
    }
}

public readonly struct Distribution : IEquatable<Distribution>
{
    readonly string distribution;

    /// <summary>
    /// Gets an instance of <see cref="DevicePlatform"/> that represents Android.
    /// </summary>
    public static Distribution Ubuntu { get; } = new Distribution(nameof(Ubuntu));
    public static Distribution Fedora { get; } = new Distribution(nameof(Fedora));
    public static Distribution ArchLinux { get; } = new Distribution(nameof(ArchLinux));
    public static Distribution Debian { get; } = new Distribution(nameof(Debian));
    public static Distribution OpenSUSE { get; } = new Distribution(nameof(OpenSUSE));
    public static Distribution LinuxMint { get; } = new Distribution(nameof(LinuxMint));
    public static Distribution Manjaro { get; } = new Distribution(nameof(Manjaro));
    public static Distribution PopOS { get; } = new Distribution(nameof(PopOS));
    public static Distribution Unknown { get; } = new Distribution(nameof(Unknown));
    public static Distribution Windows11 { get; } = new Distribution(nameof(Windows11));
    public static Distribution Windows10 { get; } = new Distribution(nameof(Windows10));
    public static Distribution Vista { get; } = new Distribution(nameof(Vista));
    public static Distribution WindowsXP { get; } = new Distribution(nameof(WindowsXP));
    public static Distribution Windows81 { get; } = new Distribution(nameof(Windows81));
    public static Distribution Windows8 { get; } = new Distribution(nameof(Windows8));
    public static Distribution Windows7 { get; } = new Distribution(nameof(Windows7));
    public static Distribution Android16 { get; } = new Distribution(nameof(Android16));
    public static Distribution Android15 { get; } = new Distribution(nameof(Android15));
    public static Distribution Android14 { get; } = new Distribution(nameof(Android14));
    public static Distribution Android13 { get; } = new Distribution(nameof(Android13));
    public static Distribution Android12L { get; } = new Distribution(nameof(Android12L));
    public static Distribution Android12 { get; } = new Distribution(nameof(Android12));
    public static Distribution Android11 { get; } = new Distribution(nameof(Android11));
    public static Distribution Android10 { get; } = new Distribution(nameof(Android10));
    public static Distribution Android9 { get; } = new Distribution(nameof(Android9));
    public static Distribution Android8_1 { get; } = new Distribution(nameof(Android8_1));
    public static Distribution Android8 { get; } = new Distribution(nameof(Android8));
    public static Distribution Android7_1 { get; } = new Distribution(nameof(Android7_1));
    public static Distribution Android7 { get; } = new Distribution(nameof(Android7));
    public static Distribution Android6 { get; } = new Distribution(nameof(Android6));
    public static Distribution Android5_1 { get; } = new Distribution(nameof(Android5_1));
    public static Distribution Android5 { get; } = new Distribution(nameof(Android5));

    internal Distribution(string distribution)
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
    public static Distribution Create(string distribution) =>
        new Distribution(distribution);

    /// <summary>
    /// Compares the underlying <see cref="DevicePlatform"/> instances.
    /// </summary>
    /// <param name="other"><see cref="DevicePlatform"/> object to compare with.</param>
    /// <returns><see langword="true"/> if they are equal, otherwise <see langword="false"/>.</returns>
    public bool Equals(Distribution other) =>
        Equals(other.distribution);

    internal bool Equals(string other) =>
        string.Equals(distribution, other, StringComparison.Ordinal);

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public override bool Equals(object? obj) =>
        obj is Distribution && Equals((Distribution)obj);

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
    public static bool operator ==(Distribution left, Distribution right) =>
        left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    /// <param name="left">Left to compare.</param>
    /// <param name="right">Right to compare.</param>
    /// <returns><see langword="true"/> if objects are not equal, otherwise <see langword="false"/>.</returns>
    public static bool operator !=(Distribution left, Distribution right) =>
        !left.Equals(right);
}
