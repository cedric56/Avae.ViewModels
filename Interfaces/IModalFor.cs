namespace Avae.ViewModels;

public interface IModalFor<T, TResult> : IViewFor where T : ICloseableViewModel<TResult>
{
    Task<TResult?> ShowModalAsync();
}