// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Migrations;

namespace PSql.Deploy;

/// <summary>
///   Module initialization handler.
/// </summary>
public class ModuleInitializer : IModuleAssemblyInitializer
{
    /// <summary>
    ///   Invoked by PowerShell when the module is imported into a runspace.
    /// </summary>
    public void OnImport()
    {
        // Do something to force loading of PSql.Deploy.Core now, so that it
        // loads into the default assembly load context.  Otherwise it might
        // load into PSql's private context along with PSql.Deploy.private.
        typeof(Migration).Equals(default);
    }
}
