// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Reflection;
using System.Runtime.Loader;

namespace PSql.Deploy;

[SetUpFixture]
public class TestSuite
{
    [OneTimeSetUp]
    public static void SetUp()
    {
        // Ensure that PSql.private.dll and its dependencies load correctly
        new PSql.Internal.ModuleLifecycleEvents().OnImport();

        // Ensure that PSql.Deploy.private.dll and its dependencies load correctly
        new PSql.Deploy.Internal.ModuleLifecycleEvents().OnImport();
    }
}
