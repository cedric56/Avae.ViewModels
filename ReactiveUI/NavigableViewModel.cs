using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Windows.Input;

namespace Avae.ViewModels;

[IReactiveObject]
public abstract partial class CloseableViewModel<TResult>
    : CloseableViewModelBase<TResult>
{
    private ICommand? _closeCommand = null;
    public override ICommand CloseCommand => _closeCommand ??= 
       ReactiveCommand.Create(async () =>
        {
            if (await CanClose())
                await Close(default);
        });
}

[IReactiveObject]
public abstract partial class NavigableViewModel(Router router, bool initialize = true) :
NavigableViewModelBase(router, initialize)
{
    [ReactiveCommand(CanExecute = nameof(CanGoBack))]
    public override Task GoBack()
    {
        return base.GoBack();
    }

    [ReactiveCommand(CanExecute = nameof(CanGoForward))]
    public override Task GoForward()
    {
        return base.GoForward();
    }

    protected override void NotifyPropertyChanged(string propertyName)
    {
        this.RaisePropertyChanged(propertyName);
    }
}

[IReactiveObject]
public abstract partial class NavigableViewModel<TResult>(Router router, bool initialize = true) :
    NavigableViewModelBase<TResult>(router, initialize)
{
    private ICommand? _closeCommand = null;
    public override ICommand CloseCommand => _closeCommand ??= ReactiveCommand.Create(async () =>
    {
        if (await CanClose())
            await Close(default);
    });

    [ReactiveCommand(CanExecute =nameof(CanGoBack))]
    public override Task GoBack()
    {
       return base.GoBack();
    }

    [ReactiveCommand(CanExecute = nameof(CanGoForward))]
    public override Task GoForward()
    {
        return base.GoForward();
    }

    protected override void NotifyPropertyChanged(string propertyName)
    {
        this.RaisePropertyChanged(propertyName);
    }
}
