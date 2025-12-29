using BobCrm.Api.Services.BackgroundJobs;
using FluentAssertions;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// InMemoryBackgroundJobClient 内存后台作业客户端测试
/// </summary>
public class InMemoryBackgroundJobClientTests
{
    private static InMemoryBackgroundJobClient CreateClient()
    {
        return new InMemoryBackgroundJobClient();
    }

    #region StartJob Tests

    [Fact]
    public void StartJob_ShouldReturnNewJobId()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var jobId = client.StartJob("Test Job", "TestCategory", "user1", "Test User", true);

        // Assert
        jobId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task StartJob_ShouldCreateJobWithCorrectProperties()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var jobId = client.StartJob("Test Job", "TestCategory", "user1", "Test User", true);
        var job = await client.GetJobAsync(jobId);

        // Assert
        job.Should().NotBeNull();
        job!.Name.Should().Be("Test Job");
        job.Category.Should().Be("TestCategory");
        job.ActorId.Should().Be("user1");
        job.ActorName.Should().Be("Test User");
        job.CanCancel.Should().BeTrue();
        job.Status.Should().Be("Running");
    }

    [Fact]
    public async Task StartJob_ShouldInitializeProgressToZero()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var jobId = client.StartJob("Test Job", "TestCategory", null, null, false);
        var job = await client.GetJobAsync(jobId);

        // Assert
        job.Should().NotBeNull();
        job!.ProgressPercent.Should().Be(0);
    }

    #endregion

    #region GetJobAsync Tests

    [Fact]
    public async Task GetJobAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var job = await client.GetJobAsync(Guid.NewGuid());

        // Assert
        job.Should().BeNull();
    }

    [Fact]
    public async Task GetJobAsync_WithExistingId_ShouldReturnJob()
    {
        // Arrange
        var client = CreateClient();
        var jobId = client.StartJob("Test", "Cat", null, null, false);

        // Act
        var job = await client.GetJobAsync(jobId);

        // Assert
        job.Should().NotBeNull();
        job!.Id.Should().Be(jobId);
    }

    #endregion

    #region SetProgress Tests

    [Fact]
    public async Task SetProgress_ShouldUpdateProgress()
    {
        // Arrange
        var client = CreateClient();
        var jobId = client.StartJob("Test", "Cat", null, null, false);

        // Act
        client.SetProgress(jobId, 50);
        var job = await client.GetJobAsync(jobId);

        // Assert
        job!.ProgressPercent.Should().Be(50);
    }

    [Fact]
    public async Task SetProgress_ShouldClampToMax100()
    {
        // Arrange
        var client = CreateClient();
        var jobId = client.StartJob("Test", "Cat", null, null, false);

        // Act
        client.SetProgress(jobId, 150);
        var job = await client.GetJobAsync(jobId);

        // Assert
        job!.ProgressPercent.Should().Be(100);
    }

    [Fact]
    public async Task SetProgress_ShouldClampToMin0()
    {
        // Arrange
        var client = CreateClient();
        var jobId = client.StartJob("Test", "Cat", null, null, false);

        // Act
        client.SetProgress(jobId, -10);
        var job = await client.GetJobAsync(jobId);

        // Assert
        job!.ProgressPercent.Should().Be(0);
    }

    #endregion

    #region Complete Tests

    [Fact]
    public async Task Complete_ShouldSetStatusToCompleted()
    {
        // Arrange
        var client = CreateClient();
        var jobId = client.StartJob("Test", "Cat", null, null, false);

        // Act
        client.Complete(jobId);
        var job = await client.GetJobAsync(jobId);

        // Assert
        job!.Status.Should().Be("Completed");
        job.ProgressPercent.Should().Be(100);
        job.FinishedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Complete_CalledTwice_ShouldNotChangeFinishTime()
    {
        // Arrange
        var client = CreateClient();
        var jobId = client.StartJob("Test", "Cat", null, null, false);
        client.Complete(jobId);
        var job1 = await client.GetJobAsync(jobId);
        var firstFinishTime = job1!.FinishedAtUtc;

        // Act
        await Task.Delay(10);
        client.Complete(jobId);
        var job2 = await client.GetJobAsync(jobId);

        // Assert
        job2!.FinishedAtUtc.Should().Be(firstFinishTime);
    }

    #endregion

    #region Fail Tests

    [Fact]
    public async Task Fail_ShouldSetStatusToFailed()
    {
        // Arrange
        var client = CreateClient();
        var jobId = client.StartJob("Test", "Cat", null, null, false);

        // Act
        client.Fail(jobId, "Error occurred");
        var job = await client.GetJobAsync(jobId);

        // Assert
        job!.Status.Should().Be("Failed");
        job.ErrorMessage.Should().Be("Error occurred");
        job.FinishedAtUtc.Should().NotBeNull();
    }

    #endregion

    #region AppendLog Tests

    [Fact]
    public async Task AppendLog_ShouldAddLogEntry()
    {
        // Arrange
        var client = CreateClient();
        var jobId = client.StartJob("Test", "Cat", null, null, false);

        // Act
        client.AppendLog(jobId, "INFO", "Test message");
        var logs = await client.GetJobLogsAsync(jobId);

        // Assert
        logs.Should().HaveCountGreaterThanOrEqualTo(2); // Start log + test message
        logs.Should().Contain(l => l.Message == "Test message");
    }

    [Fact]
    public async Task GetJobLogsAsync_WithLimit_ShouldRespectLimit()
    {
        // Arrange
        var client = CreateClient();
        var jobId = client.StartJob("Test", "Cat", null, null, false);

        for (int i = 0; i < 10; i++)
        {
            client.AppendLog(jobId, "INFO", $"Message {i}");
        }

        // Act
        var logs = await client.GetJobLogsAsync(jobId, 5);

        // Assert
        logs.Should().HaveCount(5);
    }

    #endregion

    #region RequestCancelAsync Tests

    [Fact]
    public async Task RequestCancelAsync_WhenJobCanCancel_ShouldReturnTrue()
    {
        // Arrange
        var client = CreateClient();
        var jobId = client.StartJob("Test", "Cat", null, null, canCancel: true);

        // Act
        var result = await client.RequestCancelAsync(jobId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RequestCancelAsync_WhenJobCannotCancel_ShouldReturnFalse()
    {
        // Arrange
        var client = CreateClient();
        var jobId = client.StartJob("Test", "Cat", null, null, canCancel: false);

        // Act
        var result = await client.RequestCancelAsync(jobId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RequestCancelAsync_WhenJobCompleted_ShouldReturnFalse()
    {
        // Arrange
        var client = CreateClient();
        var jobId = client.StartJob("Test", "Cat", null, null, canCancel: true);
        client.Complete(jobId);

        // Act
        var result = await client.RequestCancelAsync(jobId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RequestCancelAsync_ShouldSetCancelRequestedFlag()
    {
        // Arrange
        var client = CreateClient();
        var jobId = client.StartJob("Test", "Cat", null, null, canCancel: true);

        // Act
        await client.RequestCancelAsync(jobId);
        var job = await client.GetJobAsync(jobId);

        // Assert
        job!.CancelRequested.Should().BeTrue();
        job.Status.Should().Be("CancelRequested");
    }

    #endregion

    #region GetRecentJobsAsync Tests

    [Fact]
    public async Task GetRecentJobsAsync_ShouldReturnJobs()
    {
        // Arrange
        var client = CreateClient();
        client.StartJob("Job1", "Cat", null, null, false);
        client.StartJob("Job2", "Cat", null, null, false);

        // Act
        var result = await client.GetRecentJobsAsync(1, 10);

        // Assert
        result.Data.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetRecentJobsAsync_ShouldSupportPagination()
    {
        // Arrange
        var client = CreateClient();
        for (int i = 0; i < 5; i++)
        {
            client.StartJob($"Job{i}", "Cat", null, null, false);
        }

        // Act
        var page1 = await client.GetRecentJobsAsync(1, 2);
        var page2 = await client.GetRecentJobsAsync(2, 2);

        // Assert
        page1.Data.Should().HaveCount(2);
        page2.Data.Should().HaveCount(2);
        page1.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetRecentJobsAsync_ShouldOrderByStartedAtDescending()
    {
        // Arrange
        var client = CreateClient();
        var id1 = client.StartJob("First", "Cat", null, null, false);
        await Task.Delay(10);
        var id2 = client.StartJob("Second", "Cat", null, null, false);

        // Act
        var result = await client.GetRecentJobsAsync(1, 10);

        // Assert
        result.Data!.First().Name.Should().Be("Second");
    }

    #endregion
}
