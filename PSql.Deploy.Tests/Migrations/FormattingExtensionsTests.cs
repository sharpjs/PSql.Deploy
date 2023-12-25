// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

[TestFixture]
internal class FormattingExtensionsTests
{
    [Test]
    public void Migration_GetFixedWithStatusString_Missing()
    {
        var migration = new Migration("m");

        migration.GetFixedWidthStatusString().Should().Be("Missing");
    }

    [Test]
    public void Migration_GetFixedWithStatusString_Changed()
    {
        var migration = new Migration("m")
        {
            Path       = "p",
            HasChanged = true,
        };

        migration.GetFixedWidthStatusString().Should().Be("Changed");
    }

    [Test]
    public void Migration_GetFixedWithStatusString_Invalid()
    {
        var migration = new Migration("m")
        {
            Path        = "p",
            Diagnostics = new[] { new MigrationDiagnostic(isError: true, "message") }
        };

        migration.GetFixedWidthStatusString().Should().Be("Invalid");
    }

    [Test]
    public void Migration_GetFixedWithStatusString_Ok()
    {
        var migration = new Migration("m")
        {
            Path = "p",
        };

        migration.GetFixedWidthStatusString().Should().Be("Ok     ");
    }

    [Test]
    [TestCase(MigrationPhase.Pre , "Pre ")]
    [TestCase(MigrationPhase.Core, "Core")]
    [TestCase(MigrationPhase.Post, "Post")]
    [TestCase(-42,                 "Post")]
    public void MigrationPhase_ToFixedWithString(MigrationPhase phase, string expected)
    {
        phase.ToFixedWidthString().Should().Be(expected);
    }

    [Test]
    [TestCase(TargetDisposition.Successful, null)]
    [TestCase(TargetDisposition.Incomplete, " [INCOMPLETE]")]
    [TestCase(TargetDisposition.Failed,     " [EXCEPTION]")]
    [TestCase(-42,                          " [EXCEPTION]")]
    public void MigrationTargetDisposition_ToMarker(TargetDisposition disposition, string? expected)
    {
        disposition.ToMarker().Should().Be(expected);
    }
}
