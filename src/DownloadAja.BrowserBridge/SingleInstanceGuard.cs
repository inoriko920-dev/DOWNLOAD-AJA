namespace DownloadAja.BrowserBridge;

/// <summary>
/// RECONSTRUCTED per-user single-instance guard. Secondary processes do not
/// initialize WPF/aria2; they forward a request to the primary process instead.
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;
    private bool _disposed;

    private SingleInstanceGuard(Mutex mutex, bool isPrimary)
    {
        _mutex = mutex;
        IsPrimary = isPrimary;
    }

    public bool IsPrimary { get; }

    public static SingleInstanceGuard Acquire(string mutexName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mutexName);

        var mutex = new Mutex(
            initiallyOwned: true,
            name: mutexName,
            createdNew: out var createdNew);

        return new SingleInstanceGuard(mutex, createdNew);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (IsPrimary)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Ownership may already have been released during abnormal shutdown.
            }
        }

        _mutex.Dispose();
    }
}
