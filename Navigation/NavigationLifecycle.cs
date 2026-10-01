namespace Avae.ViewModels;

internal sealed class NavigationLifecycle
{
    public async Task<bool> CanLeaveAsync(NavigationEntry entry)
    {
        if (entry.ViewModel is INavigable vm && !await vm.CanNavigateAsync()) return false;
        if (entry.View is INavigable v && !await v.CanNavigateAsync()) return false;
        return true;
    }

    public async Task TransitionAsync(NavigationEntry leaving, NavigationEntry entering)
    {
        if (leaving.ViewModel is INavigable lvm) await lvm.OnNavigatedFrom(leaving.Context);
        if (leaving.View is INavigable lv) await lv.OnNavigatedFrom(leaving.Context);
        if (entering.ViewModel is INavigable evm) await evm.OnNavigatedTo(entering.Context);
        if (entering.View is INavigable ev) await ev.OnNavigatedTo(entering.Context);
    }
}
