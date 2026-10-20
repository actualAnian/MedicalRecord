using System;
using System.Collections.Generic;
using System.Text;

namespace Register
{
    public class Retry
    {
        private const int MaxAttempts = 3;
        private const int DelayMs = 100;
        public static T Execute<T>(Func<T> call, Func<T, bool> shouldRetry)
        {
            var result = call();
            for (var attempt = 2; attempt <= MaxAttempts; attempt++)
            {
                if (!shouldRetry(result)) return result;
                Thread.Sleep(DelayMs);
                result = call();
            }
            return result;
        }

        public static async Task<T> ExecuteAsync<T>(
            Func<Task<T>> call,
            Func<T, bool> shouldRetry,
            CancellationToken cancellationToken = default)
        {
            var result = await call();
            for (var attempt = 2; attempt <= MaxAttempts; attempt++)
            {
                if (!shouldRetry(result)) return result;
                await Task.Delay(DelayMs, cancellationToken);
                result = await call();
            }
            return result;
        }
    }
}
