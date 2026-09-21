namespace Avae.ViewModels;

public interface INavigable
{
    Task<bool> CanNavigateAsync();
    Task OnNavigatedTo(NavigableContext context);
    Task OnNavigatedFrom(NavigableContext context);
}
