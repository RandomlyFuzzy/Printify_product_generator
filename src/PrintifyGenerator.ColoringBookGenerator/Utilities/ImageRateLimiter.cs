using System;
using System.Threading;
using System.Threading.Tasks;

namespace PrintifyGenerator.ColoringBookGenerator.Utilities
{
    /// <summary>
    /// Simple token-bucket rate limiter for image generation.
    /// Limits to 100 tokens (images) per 1 hour (rolling refill).
    /// </summary>
    public sealed class ImageRateLimiter
    {
        private static readonly Lazy<ImageRateLimiter> _lazy = new(() => new ImageRateLimiter());
        public static ImageRateLimiter Instance => _lazy.Value;

        private readonly object _sync = new();
        private double _tokens;
        private readonly double _capacity = 100.0;
        private readonly double _refillPerSecond;
        private DateTime _lastRefillUtc;

        internal ImageRateLimiter()
        {
            _tokens = _capacity; // start full
            _refillPerSecond = 100.0 / 3600.0; // tokens per second
            _lastRefillUtc = DateTime.UtcNow;
        }

        private void Refill()
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastRefillUtc).TotalSeconds;
            if (elapsed <= 0) return;
            var add = elapsed * _refillPerSecond;
            if (add > 0)
            {
                _tokens = Math.Min(_capacity, _tokens + add);
                _lastRefillUtc = now;
            }
        }

        /// <summary>
        /// Acquire a single image-generation token. Awaits until a token is available.
        /// </summary>
        public async Task AcquireAsync(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double waitSeconds;
                lock (_sync)
                {
                    Refill();
                    if (_tokens >= 1.0)
                    {
                        _tokens -= 1.0;
                        return;
                    }

                    var missing = 1.0 - _tokens;
                    waitSeconds = missing / _refillPerSecond;
                    if (waitSeconds < 0.1) waitSeconds = 0.1;
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(waitSeconds, 3600)), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        /// <summary>
        /// Diagnostic snapshot of limiter state.
        /// </summary>
        public (double tokens, double capacity, double refillPerSecond, DateTime lastRefillUtc) GetStatus()
        {
            lock (_sync)
            {
                Refill();
                return (_tokens, _capacity, _refillPerSecond, _lastRefillUtc);
            }
        }
    }
}
