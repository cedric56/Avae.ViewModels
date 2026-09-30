namespace Avae.ViewModels;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class AvaeEnvironmentAttribute(string environment) : Attribute
{
    public string Environment { get; } = environment;
}
