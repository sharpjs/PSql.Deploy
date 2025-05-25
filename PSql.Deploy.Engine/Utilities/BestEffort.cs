// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   Methods to perform actions on a best-effort basis.
/// </summary>
internal static class BestEffort
{
    /// <summary>
    ///   Invokes the specified action, ignoring any exception it throws.
    /// </summary>
    /// <typeparam name="TArg">
    ///   The type of <paramref name="arg"/>.
    /// </typeparam>
    /// <param name="action">
    ///   The action to invoke.
    /// </param>
    /// <param name="arg">
    ///   The argument to pass to <paramref name="action"/>.
    /// </param>
    public static void Do<TArg>(Action<TArg> action, TArg arg)
    {
        try
        {
            if (action is not null)
                action(arg);
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    ///   Invokes the specified action asynchronously, ignoring any exception
    ///   it throws.
    /// </summary>
    /// <typeparam name="TArg">
    ///   The type of <paramref name="arg"/>.
    /// </typeparam>
    /// <param name="action">
    ///   The action to invoke.
    /// </param>
    /// <param name="arg">
    ///   The argument to pass to <paramref name="action"/>.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous operation.
    /// </returns>
    public static async ValueTask DoAsync<TArg>(Func<TArg, ValueTask> action, TArg arg)
    {
        try
        {
            if (action is not null)
                await action(arg);
        }
        catch
        {
            // ignore
        }
    }
}
