// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using PSql.Deploy.Internal;

[assembly: Parallelizable(ParallelScope.All)]
[assembly: FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[assembly: SetCulture("en-US")]

namespace PSql.Deploy;

[SetUpFixture]
public class TestSuite
{
    [OneTimeSetUp]
    public static void SetUp()
    {
        // Ensure that PSql.Deploy.Engine.dll and its dependencies load correctly
        new ModuleLifecycleEvents().OnImport();
    }

    [OneTimeTearDown]
    public static void TearDown()
    {
        // Cover removal method
        new ModuleLifecycleEvents().OnRemove(null!); // arg unused
    }
}
