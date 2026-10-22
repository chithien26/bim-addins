namespace AddinStore;

internal static class StoreLock
{
    public static void Run(Action action)
    {
        using var mutex = new Mutex(false, @"Local\AddinStore.Cache");
        var acquired = false;
        try
        {
            try
            {
                acquired = mutex.WaitOne(TimeSpan.FromSeconds(5));
            }
            catch (AbandonedMutexException)
            {
                acquired = true;
            }

            action();
        }
        finally
        {
            if (acquired)
            {
                try
                {
                    mutex.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                }
            }
        }
    }
}
