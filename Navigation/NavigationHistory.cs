namespace Avae.ViewModels;

internal sealed record NavigationEntry(
    object ViewModel,
    IViewFor View,
    NavigableContext Context);

internal sealed class NavigationHistory
{
    public const int MaxSize = 20;

    private readonly List<NavigationEntry> _entries = [];
    private int _currentIndex = -1;

    public bool CanGoBack => _currentIndex > 0;
    public bool CanGoForward => _currentIndex >= 0 && _currentIndex < _entries.Count - 1;
    public NavigationEntry? Current => _currentIndex < 0 ? null : _entries[_currentIndex];

    public void Push(NavigationEntry entry)
    {
        if (_currentIndex >= 0 && _currentIndex < _entries.Count - 1)
            _entries.RemoveRange(_currentIndex + 1, _entries.Count - _currentIndex - 1);

        _entries.Add(entry);
        if (_entries.Count > MaxSize) _entries.RemoveAt(0);
        _currentIndex = _entries.Count - 1;
    }

    public NavigationEntry? MoveBack()
    {
        if (!CanGoBack) return null;
        _currentIndex--;
        return _entries[_currentIndex];
    }

    public NavigationEntry? MoveForward()
    {
        if (!CanGoForward) return null;
        _currentIndex++;
        return _entries[_currentIndex];
    }

    public void Clear()
    {
        _entries.Clear();
        _currentIndex = -1;
    }

    internal NavigationEntry? PeekBack()
    {
        if (!CanGoBack) return null;
        return _entries[_currentIndex - 1];
    }

    internal NavigationEntry? PeekForward()
    {
        if (!CanGoForward) return null;
        return _entries[_currentIndex + 1];
    }
}
