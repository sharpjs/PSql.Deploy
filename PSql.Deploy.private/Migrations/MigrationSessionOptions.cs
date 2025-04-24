// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

extern alias Engine;

using M = Engine::PSql.Deploy.Migrations;

namespace PSql.Deploy.Migrations;

using Super = M.MigrationSessionOptions;

/// <inheritdoc cref="Super"/>
[Flags]
public enum MigrationSessionOptions
{
    /// <inheritdoc cref="Super.PrePhase"/>
    PrePhase = Super.PrePhase,

    /// <inheritdoc cref="Super.CorePhase"/>
    CorePhase = Super.CorePhase,

    /// <inheritdoc cref="Super.PostPhase"/>
    PostPhase = Super.PostPhase,

    /// <inheritdoc cref="Super.AllPhases"/>
    AllPhases = Super.AllPhases,

    /// <inheritdoc cref="Super.AllowContentInCorePhase"/>
    AllowContentInCorePhase = Super.AllowContentInCorePhase,

    /// <inheritdoc cref="Super.PrePhase"/>
    IsWhatIfMode = Super.IsWhatIfMode,
}
