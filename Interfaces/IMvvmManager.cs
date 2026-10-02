using System.Collections.ObjectModel;

namespace Avae.ViewModels;

public interface IMvvmManager
{
    ObservableCollection<NavigableView> Navigables { get; }

    NavigableView SelectedNavigable { get; set; }

    IViewFor? CurrentView { get; set; }

    EventHandler<IViewFor?>? CurrentViewChanged { get; set; }

    Task OnNavigableChanged(NavigableView? value);
}
