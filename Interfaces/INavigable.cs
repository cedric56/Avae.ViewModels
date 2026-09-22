namespace Avae.ViewModels;

public interface INavigable
{
    Task<bool> CanNavigateAsync() => Task.FromResult(true);
    Task OnNavigatedTo(NavigableContext context) => Task.CompletedTask;
    Task OnNavigatedFrom(NavigableContext context) => Task.CompletedTask;
}
