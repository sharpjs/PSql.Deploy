// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

[assembly: Parallelizable(ParallelScope.All)]
[assembly: SetCulture("en-US")]

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

    [OneTimeTearDown]
    public static void TearDown()
    {
        // Cover removal method
        new PSql.Deploy.Internal.ModuleLifecycleEvents().OnRemove(null!); // arg unused
    }
}
