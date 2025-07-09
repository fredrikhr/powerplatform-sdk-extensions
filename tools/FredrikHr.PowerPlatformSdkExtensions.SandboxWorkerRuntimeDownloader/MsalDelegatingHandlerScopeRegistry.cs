namespace FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerRuntimeDownloader;

internal sealed class MsalDelegatingHandlerScopeRegistry() : IDisposable
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly List<MsalDelegatingHandlerScopeEntry> _entries =
        new(capacity: 8);

    public IEnumerable<string>? GetScopes(Uri requestUri)
    {
        _lock.EnterReadLock();
        try
        {
            foreach (var (entryUri, entryScopes) in _entries)
            {
                if (!entryUri.IsBaseOf(requestUri)) continue;
                return entryScopes;
            }
        }
        finally { _lock.ExitReadLock(); }

        return null;
    }

    public void AddEntry(Uri uri, IEnumerable<string> scopes)
    {
        _lock.EnterWriteLock();
        try
        {
            int index = 0;
            foreach (var (existingEntryUri, _) in _entries)
            {
                if (existingEntryUri.IsBaseOf(uri))
                {
                    _entries.Insert(index, new(uri, [.. scopes]));
                    return;
                }

                index++;
            }
            _entries.Add(new(uri, [.. scopes]));
        }
        finally { _lock.ExitWriteLock(); }
    }

    public void RemoveClosestEntry(Uri requestUri)
    {
        RemoveEntryCore(requestUri, multiple: false);
    }

    public void RemoveMatchingEntries(Uri requestUri)
    {
        RemoveEntryCore(requestUri, multiple: true);
    }

    private void RemoveEntryCore(Uri requestUri, bool multiple)
    {
        _lock.EnterUpgradeableReadLock();
        try
        {
            for (int index = 0; index < _entries.Count; index++)
            {
                var (entryUri, _) = _entries[index];
                if (entryUri.IsBaseOf(requestUri))
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        _entries.RemoveAt(index);
                        if (!multiple) return;
                    }
                    finally { _lock.ExitWriteLock(); }
                }
            }
        }
        finally { _lock.ExitUpgradeableReadLock(); }
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}