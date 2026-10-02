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
        await LeaveAsync(leaving).ConfigureAwait(false);

        try
        {
            await EnterAsync(entering).ConfigureAwait(false);
        }
        catch
        {
            // `entering` failed partway through OnNavigatedTo. `leaving` has already torn
            // down, but Router does not advance history on a failed transition, so the
            // router's history still points at `leaving`. Re-run `leaving`'s OnNavigatedTo
            // so its lifecycle state matches "still current" again, consistent with history.
            // This assumes OnNavigatedFrom/OnNavigatedTo are safe to re-enter without a
            // matching OnNavigatedFrom in between — if that's ever not true for a given
            // view/view model, this will surface as a bug there, not silently here.
            try
            {
                await EnterAsync(leaving).ConfigureAwait(false);
            }
            catch
            {
                // Recovery itself failed — leaving is now in an unknown state and history
                // still points at it. Don't let the recovery failure hide the original
                // exception; that one is what actually caused navigation to fail.
            }

            throw;
        }
    }

    private static async Task LeaveAsync(NavigationEntry entry)
    {
        if (entry.ViewModel is INavigable vm) await vm.OnNavigatedFrom(entry.Context).ConfigureAwait(false);
        if (entry.View is INavigable v) await v.OnNavigatedFrom(entry.Context).ConfigureAwait(false);
    }

    private static async Task EnterAsync(NavigationEntry entry)
    {
        if (entry.ViewModel is INavigable vm) await vm.OnNavigatedTo(entry.Context).ConfigureAwait(false);
        if (entry.View is INavigable v) await v.OnNavigatedTo(entry.Context).ConfigureAwait(false);
    }
}