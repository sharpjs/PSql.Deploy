// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

extern alias Engine;

using M = Engine::PSql.Deploy.Migrations;

namespace PSql.Deploy.Migrations;

internal class Migration : IMigration
{
    private readonly M.Migration _inner;

    public Migration(M.Migration inner)
    {
        if (inner is null)
            throw new ArgumentNullException(nameof(inner));

        _inner = inner;
    }

    /// <inheritdoc/>
    public string Name => _inner.Name;

    /// <inheritdoc/>
    public bool IsPseudo => _inner.IsPseudo;

    /// <inheritdoc/>
    public string? Path => _inner.Path;

    /// <inheritdoc/>
    public string Hash => _inner.Hash;

    /// <inheritdoc/>
    public MigrationState State => (MigrationState) _inner.State;

    /// <inheritdoc/>
    public bool HasChanged => _inner.HasChanged;
}
