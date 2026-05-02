namespace Pastas.Application.Services;

public sealed class ClipboardRetryPolicy
{
    private static readonly int[] RetryDelaysMs = [30, 80, 150];

    public async Task<T?> ExecuteAsync<T>(Func<T?> operation, CancellationToken cancellationToken = default)
        where T : class
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt <= RetryDelaysMs.Length; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = operation();
                if (result is not null)
                {
                    return result;
                }
            }
            catch (Exception ex) when (IsClipboardBusyException(ex))
            {
                lastException = ex;
            }

            if (attempt < RetryDelaysMs.Length)
            {
                await Task.Delay(RetryDelaysMs[attempt], cancellationToken);
            }
        }

        if (lastException is not null)
        {
            return null;
        }

        return null;
    }

    public async Task<bool> ExecuteAsync(Action operation, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt <= RetryDelaysMs.Length; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                operation();
                return true;
            }
            catch (Exception ex) when (IsClipboardBusyException(ex))
            {
                if (attempt < RetryDelaysMs.Length)
                {
                    await Task.Delay(RetryDelaysMs[attempt], cancellationToken);
                    continue;
                }

                return false;
            }
        }

        return false;
    }

    private static bool IsClipboardBusyException(Exception exception)
    {
        return exception is System.Runtime.InteropServices.COMException
            || exception is InvalidOperationException
            || exception is System.Runtime.InteropServices.ExternalException;
    }
}
