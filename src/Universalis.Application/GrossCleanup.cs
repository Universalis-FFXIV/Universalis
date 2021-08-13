using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Universalis.Application
{
    /// <summary>
    /// Called such because it calls GC.Collect() periodically. This shouldn't be done in production applications, but it works.
    /// </summary>
    public class GrossCleanup : IAsyncDisposable
    {
        private static readonly TimeSpan CleanupInterval = new(1, 0, 0);

        private readonly ILogger<GrossCleanup> _logger;
        private readonly Timer _cleanupTimer;

        public GrossCleanup(ILogger<GrossCleanup> logger)
        {
            _logger = logger;
            _cleanupTimer = new Timer(Cleanup, null, CleanupInterval, CleanupInterval);
        }

        private void Cleanup(object o)
        {
            try
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to execute garbage collector.");
            }
        }

        public ValueTask DisposeAsync()
        {
            return _cleanupTimer.DisposeAsync();
        }
    }
}