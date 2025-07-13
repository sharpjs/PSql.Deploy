// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace PSql.Deploy.Migrations;

using static BitOperations;

/// <summary>
///   A sorted set of <see cref="MigrationPhase"/> values.
/// </summary>
public readonly struct MigrationPhaseSet : IReadOnlyCollection<MigrationPhase>
{
    private const uint AllBits = 0b111;

    private readonly uint _bits;

    /// <summary>
    ///   Initializes a new <see cref="MigrationPhaseSet"/> instance containing
    ///   the specified migration phases.
    /// </summary>
    /// <param name="phases">
    ///   The migration phases to include in the set, or <see langword="null"/>
    ///   to include all phases.
    /// </param>
    public MigrationPhaseSet(IEnumerable<MigrationPhase>? phases)
    {
        _bits = Bits(phases);
    }

    /// <inheritdoc/>
    public int Count
        => PopCount(_bits);

    /// <summary>
    ///   Checks whether the set contains the specified migration phase.
    /// </summary>
    /// <param name="phase">
    ///   The migration phase to check.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if the set contains <paramref name="phase"/>;
    ///   <see langword="false"/> otherwise.
    /// </returns>
    public bool Contains(MigrationPhase phase)
        => (_bits & Bit(phase)) is not 0;

    /// <summary>
    ///   Gets the first migration phase in the set.
    /// </summary>
    /// <returns>
    ///   The first migration phase in the set, if the set is not empty;
    ///   otherwise, an invalid value.
    /// </returns>
    public MigrationPhase First()
        => (MigrationPhase) TrailingZeroCount(_bits);

    /// <summary>
    ///   Gets an enumerator that iterates through the set.
    /// </summary>
    public Enumerator GetEnumerator()
    {
        return new(this);
    }

    /// <inheritdoc/>
    IEnumerator<MigrationPhase> IEnumerable<MigrationPhase>.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private uint Bits(IEnumerable<MigrationPhase>? phases)
    {
        if (phases is null)
            return AllBits;

        var bits = 0U;

        foreach (var phase in phases)
            bits |= Bit(phase);

        return bits & AllBits;
    }

    private static uint Bit(MigrationPhase phase)
        => 1U << (int) phase;

    /// <summary>
    ///   An enumerator over a <see cref="MigrationPhaseSet"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<MigrationPhase>
    {
        private const MigrationPhase
            Initial = (MigrationPhase) (-1);

        private readonly MigrationPhaseSet _set;
        private          MigrationPhase    _current;

        internal Enumerator(MigrationPhaseSet set)
        {
            _set     = set;
            _current = Initial;
        }

        /// <inheritdoc/>
        public MigrationPhase Current => _current;

        /// <inheritdoc/>
        object IEnumerator.Current => _current;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            while (_current < MigrationPhase.Post)
                if (_set.Contains(++_current))
                    return true;

            return false;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            _current = Initial;
        }

        /// <inheritdoc/>
        void IDisposable.Dispose() { }
    }
}
