// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

/// <summary>
///   Options for a deployment session in which content seeds are applied to
///   target databases.
/// </summary>
public class SeedSessionOptions : DeploymentSessionOptions
{
    /// <summary>
    ///   Gets or sets preprocessor variable definitions for the session.  Each
    ///   definition is a tuple of the variable name and its value.  The
    ///   default value is <see langword="null"/>.
    /// </summary>
    public IEnumerable<(string Name, string Value)>? Defines { get; set; }
}
