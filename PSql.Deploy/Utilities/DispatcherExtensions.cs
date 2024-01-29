// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using Subatomix.PowerShell.TaskHost;

namespace PSql.Deploy.Utilities;

using Void = ValueTuple;

/// <summary>
///   Extension methods for <see cref="IDispatcher"/>.
/// </summary>
internal static class DispatcherExtensions
{
    /// <summary>
    ///   Invokes the specified action using the dispatcher and waits for the
    ///   action to complete.
    /// </summary>
    /// <param name="dispatcher">
    ///   The dispatcher to use to invoke <paramref name="action"/>.
    /// </param>
    /// <param name="action">
    ///   The action to invoke using <paramref name="dispatcher"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="dispatcher"/> and/or
    ///   <paramref name="action"/> is <see langword="null"/>.
    /// </exception>
    public static void Invoke(this IDispatcher dispatcher, Action action)
    {
        if (dispatcher is null)
            throw new ArgumentNullException(nameof(dispatcher));
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        var info = TaskInfo.Current;
        var task = new TaskCompletionSource<Void>();

        void Invoke()
        {
            using var _ = info.Use();
            action();
            task.SetResult(default);
        }

        dispatcher.Post(Invoke);

        task.Task.GetAwaiter().GetResult();
    }

    /// <summary>
    ///   Invokes the specified action using the dispatcher and waits for the
    ///   action to complete.
    /// </summary>
    /// <param name="dispatcher">
    ///   The dispatcher to use to invoke <paramref name="action"/>.
    /// </param>
    /// <param name="action">
    ///   The action to invoke using <paramref name="dispatcher"/>.
    /// </param>
    /// <param name="arg">
    ///   An argument provided to <paramref name="action"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="dispatcher"/> and/or
    ///   <paramref name="action"/> is <see langword="null"/>.
    /// </exception>
    public static void Invoke<TArg>(this IDispatcher dispatcher, Action<TArg> action, TArg arg)
    {
        if (dispatcher is null)
            throw new ArgumentNullException(nameof(dispatcher));
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        var info = TaskInfo.Current;
        var task = new TaskCompletionSource<Void>();

        void Invoke()
        {
            using var _ = info.Use();
            action(arg);
            task.SetResult(default);
        }

        dispatcher.Post(Invoke);

        task.Task.GetAwaiter().GetResult();
    }

    /// <summary>
    ///   Invokes the specified action using the dispatcher and waits for the
    ///   action to complete.
    /// </summary>
    /// <param name="dispatcher">
    ///   The dispatcher to use to invoke <paramref name="action"/>.
    /// </param>
    /// <param name="action">
    ///   The action to invoke using <paramref name="dispatcher"/>.
    /// </param>
    /// <returns>
    ///   The result of <paramref name="action"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="dispatcher"/> and/or
    ///   <paramref name="action"/> is <see langword="null"/>.
    /// </exception>
    public static TResult Invoke<TResult>(this IDispatcher dispatcher, Func<TResult> action)
    {
        if (dispatcher is null)
            throw new ArgumentNullException(nameof(dispatcher));
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        var info = TaskInfo.Current;
        var task = new TaskCompletionSource<TResult>();

        void Invoke()
        {
            using var _ = info.Use();
            task.SetResult(action());
        }

        dispatcher.Post(Invoke);

        return task.Task.GetAwaiter().GetResult();
    }

    /// <summary>
    ///   Invokes the specified action using the dispatcher and waits for the
    ///   action to complete.
    /// </summary>
    /// <param name="dispatcher">
    ///   The dispatcher to use to invoke <paramref name="action"/>.
    /// </param>
    /// <param name="action">
    ///   The action to invoke using <paramref name="dispatcher"/>.
    /// </param>
    /// <param name="arg">
    ///   An argument provided to <paramref name="action"/>.
    /// </param>
    /// <returns>
    ///   The result of <paramref name="action"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="dispatcher"/> and/or
    ///   <paramref name="action"/> is <see langword="null"/>.
    /// </exception>
    public static TResult Invoke<TArg, TResult>(this IDispatcher dispatcher, Func<TArg, TResult> action, TArg arg)
    {
        if (dispatcher is null)
            throw new ArgumentNullException(nameof(dispatcher));
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        var info = TaskInfo.Current;
        var task = new TaskCompletionSource<TResult>();

        void Invoke()
        {
            using var _ = info.Use();
            task.SetResult(action(arg));
        }

        dispatcher.Post(Invoke);

        return task.Task.GetAwaiter().GetResult();
    }

    private static TaskScope? Use(this TaskInfo? info)
    {
        return info is null ? null : new TaskScope(info);
    }
}
