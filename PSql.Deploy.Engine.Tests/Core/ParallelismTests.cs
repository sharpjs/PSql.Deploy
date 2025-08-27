// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
public class ParallelismTests
{
    [Test]
    public void Construct()
    {
        var parallelism = new Parallelism(
            maxParallelTargets:   4,
            maxParallelCommands:  8,
            maxCommandsPerTarget: 2
        );

        parallelism.MaxParallelTargets  .ShouldBe(4);
        parallelism.MaxParallelCommands .ShouldBe(8);
        parallelism.MaxCommandsPerTarget.ShouldBe(2);
    }

    [Test]
    public void Construct_InvalidMaxParallelTargets()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            _ = new Parallelism(0, 8, 2);
        });
    }

    [Test]
    public void Construct_InvalidMaxParallelCommands()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            _ = new Parallelism(4, 0, 2);
        });
    }

    [Test]
    public void Construct_InvalidMaxCommandsPerTarget()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            _ = new Parallelism(4, 8, 0);
        });
    }

    [Test]
    public async Task UseTargetScopeAsync_ReturnsDisposableObject()
    {
        // Arrange
        var parallelism = new Parallelism(4, 8, 2);

        // Act
        var scope = await parallelism.UseTargetScopeAsync();

        // Assert
        scope.ShouldNotBeNull();
        scope.ShouldBeAssignableTo<IDisposable>();
    }

    [Test]
    public async Task UseCommandScopeAsync_ReturnsDisposableObject()
    {
        // Arrange
        var parallelism = new Parallelism(4, 8, 2);

        // Act
        var scope = await parallelism.UseCommandScopeAsync();

        // Assert
        scope.ShouldNotBeNull();
        scope.ShouldBeAssignableTo<IDisposable>();
    }

    [Test]
    public async Task UseTargetScopeAsync()
    {
        using var parallelism = new Parallelism(maxParallelTargets: 2, 8, 2);

        using var scope0 = await parallelism.UseTargetScopeAsync();
        using var scope1 = await parallelism.UseTargetScopeAsync();

        var scopeTask = parallelism.UseTargetScopeAsync();

        await ShouldBeWaitingAsync(scopeTask);

        scope0.Dispose();

        using var scope2 = await scopeTask;

        // scope0 will be disposed again here, testing multiple disposal of scope
    }

    [Test]
    public async Task UseCommandScopeAsync()
    {
        using var parallelism = new Parallelism(4, maxParallelCommands: 2, 2);

        using var scope0 = await parallelism.UseCommandScopeAsync();
        using var scope1 = await parallelism.UseCommandScopeAsync();

        var scopeTask = parallelism.UseCommandScopeAsync();

        await ShouldBeWaitingAsync(scopeTask);

        scope0.Dispose();

        using var scope2 = await scopeTask;

        // scope0 will be disposed again here, testing multiple disposal of scope
    }

    [Test]
    public async Task UseTargetScopeAsync_Cancellation()
    {
        using var parallelism = new Parallelism(maxParallelTargets: 1, 8, 2);

        using var scope = await parallelism.UseTargetScopeAsync();

        using var cancellation = new CancellationTokenSource();

        var task = parallelism.UseTargetScopeAsync(cancellation.Token); // will wait
        
        cancellation.Cancel();
        
        await Should.ThrowAsync<OperationCanceledException>(() => task);
    }

    [Test]
    public async Task UseCommandScopeAsync_HonorsCancellation()
    {
        using var parallelism = new Parallelism(4, maxParallelCommands: 1, 1);

        using var scope = await parallelism.UseCommandScopeAsync();

        using var cancellation = new CancellationTokenSource();

        var task = parallelism.UseCommandScopeAsync(cancellation.Token); // will wait
        
        cancellation.Cancel();
        
        await Should.ThrowAsync<OperationCanceledException>(() => task);
    }

    [Test]
    public void Dispose_Multiple()
    {
        using var parallelism = new Parallelism(4, 8, 2);
        
        parallelism.Dispose();

        Should.ThrowAsync<ObjectDisposedException>(() => parallelism.UseTargetScopeAsync());
        Should.ThrowAsync<ObjectDisposedException>(() => parallelism.UseCommandScopeAsync());

        // Not affected by disposal
        parallelism.MaxParallelTargets  .ShouldBe(4);
        parallelism.MaxParallelCommands .ShouldBe(8);
        parallelism.MaxCommandsPerTarget.ShouldBe(2);

        // will be disposed again here, testing multiple disposal
    }

    private static async Task ShouldBeWaitingAsync(Task task)
    {
        (await Task.WhenAny(task, Task.Delay(10))).ShouldNotBeSameAs(task);
    }
}
