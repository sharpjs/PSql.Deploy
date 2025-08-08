// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Text;

namespace PSql.Deploy;

internal static class ExceptionHelpers
{
    public static string GetCompoundMessage(string message, Exception? innerException)
    {
        if (innerException is null)
            return message;

        return innerException
            .AggregateInnermost(
                new StringBuilder(message),
                (e, builder) => builder.Append(' ').Append(e.Message)
            )
            .ToString();
    }

    public static TAccumulator AggregateInnermost<TAccumulator>(
        this Exception                              exception,
        TAccumulator                                accumulator,
        Func<Exception, TAccumulator, TAccumulator> action)
    {
        switch (exception)
        {
            case AggregateException { InnerExceptions: { Count: > 0 } inners }:
                foreach (var inner in inners)
                    accumulator = AggregateInnermost(inner, accumulator, action);
                return accumulator;

            case Exception { InnerException: { } inner }:
                return AggregateInnermost(inner, accumulator, action);

            default:
                return action(exception, accumulator);
        }
    }
}
