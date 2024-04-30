namespace PlaygroundApi.Services
{
    internal abstract class TimedBackgroundService : BackgroundService
    {
        private readonly ILogger _logger;

        public TimedBackgroundService(
            ILogger<TimedBackgroundService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected abstract Task<BackgroundTaskResult> InvokeAsync(CancellationToken stoppingToken);

        protected override sealed async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var className = GetType().Name;
            _logger.LogInformation("{ClassName}: Starting Background Service", className);

            TimeSpan delay;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await InvokeAsync(stoppingToken);
                    delay = result.Delay;
                }
                catch (Exception e)
                {
                    // If an exception is thrown out of ExecuteAsync in a background service,
                    // the entire service will crash. Sometimes this is the desired behavior
                    // so we'll log and rethrow any exceptions from the RunAsync method.
                    _logger.LogError(e, "{ClassName}: the InvokeAsync() method has thrown an exception.", className);
                    throw;
                }

                // Enforce waiting at a minimum 1 millisecond, because Task.Delay(0) is synchronous.
                if (delay == TimeSpan.Zero)
                {
                    delay = TimeSpan.FromMilliseconds(1);
                }

                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
