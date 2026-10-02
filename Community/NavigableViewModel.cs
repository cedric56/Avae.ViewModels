using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Avae.ViewModels;

[INotifyPropertyChanged]
public abstract partial class CloseableViewModel<TResult>
    : CloseableViewModelBase<TResult>
{
    private IAsyncRelayCommand? _closeCommand = null;
    public override ICommand CloseCommand => _closeCommand ??= new AsyncRelayCommand(async () =>
    {
        if (await CanClose())
            await Close(default);
    });
}

[INotifyPropertyChanged]
public abstract partial class NavigableViewModel(IRouter router, bool initialize = true) :
NavigableViewModelBase(router, initialize)
{
    [RelayCommand]
    public override Task GoBack()
    {
        return base.GoBack();
    }

    [RelayCommand]
    public override Task GoForward()
    {
        return base.GoForward();
    }

    protected override void RaiseCanExecutesChanged()
    {
        GoBackCommand.NotifyCanExecuteChanged();
        GoForwardCommand.NotifyCanExecuteChanged();
    }

    protected override void NotifyPropertyChanged(string propertyName)
    {
        OnPropertyChanged(propertyName);
    }
}

[INotifyPropertyChanged]
public abstract partial class NavigableViewModel<TResult>(IRouter router, bool initialize = true) :
    NavigableViewModelBase<TResult>(router, initialize)
{
    private IAsyncRelayCommand? _closeCommand = null;
    public override ICommand CloseCommand => _closeCommand ??= new AsyncRelayCommand(async () =>
    {
        if (await CanClose())
            await Close(default);
    });

    [RelayCommand]
    public override Task GoBack()
    {
       return base.GoBack();
    }

    [RelayCommand]
    public override Task GoForward()
    {
        return base.GoForward();
    }

    protected override void RaiseCanExecutesChanged()
    {
        GoBackCommand.NotifyCanExecuteChanged();
        GoForwardCommand.NotifyCanExecuteChanged();
    }

    protected override void NotifyPropertyChanged(string propertyName)
    {
        OnPropertyChanged(propertyName);
    }
}
