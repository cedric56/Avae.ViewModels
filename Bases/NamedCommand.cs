using System.Windows.Input;

namespace Avae.ViewModels;

public class NamedCommand
{
    public required string Name { get; set; }
    public required ICommand Command { get; set; }
}
