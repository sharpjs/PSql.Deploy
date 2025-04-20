#if CONVERTED
// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using PSql.Internal;

namespace PSql.Deploy.Internal;

/// <summary>
///   Handlers for module lifecycle events.
/// </summary>
public class ModuleLifecycleEvents : IModuleAssemblyInitializer, IModuleAssemblyCleanup
{
    // PSql.Deploy.private.dll
    private static readonly PrivateDependencyRegistration
        PrivateDependency = new();

    /// <summary>
    ///   Invoked by PowerShell when the module is imported into a runspace.
    /// </summary>
    public void OnImport()
    {
        PrivateDependency.Reference();
    }

    /// <summary>
    ///   Invoked by PowerShell when the module is removed from a runspace.
    /// </summary>
    /// <param name="module">
    ///   The module being removed.
    /// </param>
    public void OnRemove(PSModuleInfo module)
    {
        PrivateDependency.Unreference();
    }
}
#endif
