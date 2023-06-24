// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationPlannerTests
{
    [Test, Ignore("Not ready yet.")]
    public void Simple()
    {
        var migrations = new Migration[]
        {
            new() { Name = "0", PreSql = "Pre0", CoreSql = "Core0", PostSql = "Post0" }
        };

        var plan = new MigrationPlanner(migrations).CreatePlan();

        plan.Should().NotBeNull();
    }
}
