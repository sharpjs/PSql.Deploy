// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

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
        DbProviderFactories.RegisterFactory(
            typeof(SqlClientFactory).Namespace!,
            SqlClientFactory.Instance
        );
    }

    [OneTimeTearDown]
    public static void TearDown()
    {
    }
}
