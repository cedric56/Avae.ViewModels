using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Avae.ViewModels;

public interface ICloseableViewModel<TResult>
{
    string Title { get; }
    ObservableCollection<NamedCommand> Commands { get; }
    ICommand? CloseCommand { get; }
    event EventHandler<TResult?>? CloseRequested;
    Task Close(TResult? value);
}