using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace TimeProviderTest;

public class SomeServiceTests
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly SomeService _someService;

    public SomeServiceTests()
    {
        _someService = new SomeService(_timeProvider);
    }
    
    [Fact]
    public async Task TimeoutWithCancellationToken_BeforeDelayResolves_TaskCompletedShouldBeFalse()
    {
        // Act
        var func = () =>
        {
            var result =  _someService.TimeoutWithCancellationToken(delaySeconds: 1, cancellationSeconds: 2);
            _timeProvider.Advance(TimeSpan.FromMilliseconds(500));
            return result;
        };
        var task = func.Invoke();
        
        // Assert
        await Task.Delay(2);
        // Even after a delay of 2 seconds, it should not have completed because we are using FakeTimeProvider
        task.IsCompleted.Should().BeFalse();
    }
    
    
    [Fact]
    public async Task TimeoutWithCancellationToken_AfterDelayBeforeCancellation_DoesNotThrow()
    {
        // Act
        var task =  _someService.TimeoutWithCancellationToken(delaySeconds: 1, cancellationSeconds: 2);
        _timeProvider.Advance(TimeSpan.FromMilliseconds(1500));
        bool result = await task;
                
        // Assert
       result.Should().BeTrue();
    }
    
    [Fact]
    public async Task TimeoutWithCancellationToken_AfterCancellation_Throws()
    {
        // Act
        var func = () =>
        {
            var result =  _someService.TimeoutWithCancellationToken(delaySeconds: 3, cancellationSeconds: 2);
            _timeProvider.Advance(TimeSpan.FromMilliseconds(2500));
            return result;
        };
                
        // Assert
        await func.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public void PollyRetry_WhenTaskDelayLessThanCancellationAndBelowRetryDelay_ShouldHave1Try()
    {
        // Act
        var result = _someService.PollyRetry(taskDelay: 1, cancellationSeconds: 3);
        _timeProvider.Advance(TimeSpan.FromMilliseconds(500));
        
        // Assert
        result.IsCompleted.Should().BeFalse();
        _someService.Tries.Should().Be(1);
    }
    
    /// <summary>
    /// This test fails
    /// </summary>
    [Fact]
    public void PollyRetry_WhenTaskDelayLessThanCancellationAndAboveRetryDelay_ShouldHave2Tries()
    {
        // Act
        var result = _someService.PollyRetry(taskDelay: 1, cancellationSeconds: 6);
        // Advancing the time more than one second should resolves the first execution delay.
        _timeProvider.Advance(TimeSpan.FromMilliseconds(1001));
        // Advancing the time more than the retry delay time of 1s,
        // and less then the task execution delay should start the second try, but it doesn't
        _timeProvider.Advance(TimeSpan.FromMilliseconds(1050));
        
        // Assert
        result.IsCompleted.Should().BeFalse();
        _someService.Tries.Should().Be(2);
    }
}
