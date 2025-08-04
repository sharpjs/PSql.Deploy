// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace PSql.Deploy;

/// <summary>
///   Math helpers.
/// </summary>
internal static class MathEx
{
    /// <summary>
    ///   Rounds the specified value up to a power of two, saturating at
    ///   <c>int.MaxValue</c>.
    /// </summary>
    /// <param name="value">
    ///   The value to round up to a power of two.
    /// </param>
    /// <returns>
    ///   The smallest power of two that is greater than or equal to
    ///   <paramref name="value"/>.  If <paramref name="value"/> is less than
    ///   <c>1</c>, this method returns <c>1</c>.  If the result would
    ///   overflow, this method returns <c>int.MaxValue</c>.
    /// </returns>
    internal static int RoundUpToPowerOf2Saturating(int value)
    {
        if (value <= 0)
            return 1;

        return (int) Math.Min(
            BitOperations.RoundUpToPowerOf2((uint) value),
            int.MaxValue
        );
    }
}
