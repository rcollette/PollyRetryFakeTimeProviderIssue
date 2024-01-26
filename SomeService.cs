using Polly;
using Polly.Retry;

namespace TimeProviderTest;

public class SomeService(TimeProvider timeProvider)
{
    // Don't do this in real life, not thread safe
    public int Tries { get; private set; }
    
    private readonly ResiliencePipeline _retryPipeline = new ResiliencePipelineBuilder { TimeProvider = timeProvider }
        .AddRetry(
            new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
                Delay = TimeSpan.FromSeconds(1),
                MaxRetryAttempts = 2,
                BackoffType = DelayBackoffType.Linear,
            })
        .Build();
    
    public async Task<bool> TimeoutWithCancellationToken(double delaySeconds, double cancellationSeconds)
    {
            CancellationTokenSource cts = new( TimeSpan.FromSeconds(cancellationSeconds), timeProvider);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), timeProvider, cts.Token);
            return true;
    }
    
    public async Task<int> PollyRetry(double taskDelay, double cancellationSeconds)
    {
        CancellationTokenSource cts = new( TimeSpan.FromSeconds(cancellationSeconds), timeProvider);
        Tries = 0;
        return  await _retryPipeline.ExecuteAsync(
            async _ =>
            {
                Tries++;
                // Simulate a task that takes some time to complete
                await Task.Delay(TimeSpan.FromSeconds(taskDelay), timeProvider,CancellationToken.None);
                if (Tries < 2)
                {
                    throw new InvalidOperationException();
                }
                return Tries;
            },
            cts.Token);
    }
}
