// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

using static MigrationPhase;

[TestFixture]
public class MigrationValidatorTests : TestHarnessBase
{
    private readonly Mock<IMigrationApplication> _context;
    private readonly Mock<IMigrationSession>     _session;

    public MigrationValidatorTests()
    {
        var target = new Target(
            connectionString:    "Server=example.com",
            serverDisplayName:   "s",
            databaseDisplayName: "d"
        );

        _session = Mocks.Create<IMigrationSession>();
        _session.Setup(c => c.CurrentPhase).Returns(Post);

        _context = Mocks.Create<IMigrationApplication>();
        _context.Setup(c => c.Session).Returns(_session.Object);
        _context.Setup(c => c.Target ).Returns(target);
    }

    [Test]
    public void Construct_NullContext()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new MigrationValidator(null!);
        });
    }

    [Test]
    public void Validate_NullPlan()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new MigrationValidator(_context.Object).Validate(null!);
        });
    }

    [Test]
    public void Validate_EmptyPlan()
    {
        Validate().ShouldBeTrue();
    }

    [Test]
    public void Validate_PseudoOnlyPlan()
    {
        var migration = new Migration(Migration.BeginPseudoMigrationName);

        Validate(migration).ShouldBeTrue();
    }

    [Test]
    public void Validate_Changed_NotApplied()
    {
        var migration = new Migration("m")
        {
            Path       = @"/test/m/_Main.sql",
            State      = MigrationState.NotApplied,
            HasChanged = true,
        };

        Validate(migration).ShouldBeTrue();

        migration.Diagnostics.ShouldBeEmpty();
    }

    [Test]
    public void Validate_Changed_Applied()
    {
        var migration = new Migration("m")
        {
            Path       = @"/test/m/_Main.sql",
            State      = MigrationState.AppliedPre,
            HasChanged = true,
            Hash       = "h",
        };

        Validate(migration).ShouldBeFalse();

        migration.Diagnostics.ShouldBeEquivalentTo(new[]
        {
            MakeError(
                "Migration 'm' has been applied to database [s].[d] through the " +
                "Pre phase, but the migration's code in the source directory "    +
                "does not match the code previously used. To resolve, revert "    +
                "any accidental changes to this migration. To ignore, update "    +
                "the hash in the _deploy.Migration table to match the hash of "   +
                "the code in the source directory (h)."
            )
        });
    }

    [Test]
    public void Validate_DependsOn_Resolved()
    {
        var migration = new Migration("m1")
        {
            Path      = @"/test/m1/_Main.sql",
            DependsOn = ImmutableArray.Create(MakeReference(new Migration("m0")))
        };

        Validate(migration).ShouldBeTrue();

        migration.Diagnostics.ShouldBeEmpty();
    }

    [Test]
    public void Validate_DependsOn_EarlierThanMinimumDefinedMigration()
    {
        var migration = new Migration("m5")
        {
            Path      = @"/test/m5/_Main.sql",
            DependsOn = ImmutableArray.Create(MakeReference("m2"))
        };

        _session.Setup(s => s.EarliestDefinedMigrationName).Returns("m3");

        Validate(migration).ShouldBeTrue();

        migration.Diagnostics.ShouldBeEquivalentTo(new[]
        {
            MakeWarning(
                "Ignoring migration 'm5' dependency on migration 'm2', " +
                "which is older than the earliest migration on disk."
            )
        });
    }

    [Test]
    public void Validate_DependsOn_Missing()
    {
        var migration = new Migration("m5")
        {
            Path      = @"/test/m5/_Main.sql",
            DependsOn = ImmutableArray.Create(MakeReference("m3"))
        };

        _session.Setup(s => s.EarliestDefinedMigrationName).Returns("m3");

        Validate(migration).ShouldBeFalse();

        migration.Diagnostics.ShouldBeEquivalentTo(new[]
        {
            MakeError(
                "Migration 'm5' declares a dependency on migration 'm3', " +
                "which was not found. " +
                "The dependency cannot be satisfied."
            )
        });
    }

    [Test]
    public void Validate_DependsOn_Self()
    {
        var migration = new Migration("m5")
        {
            Path      = @"/test/m5/_Main.sql",
            DependsOn = ImmutableArray.Create(MakeReference("M5"))
            //        to verify case-insensitive comparison: ^
        };

        Validate(migration).ShouldBeFalse();

        migration.Diagnostics.ShouldBeEquivalentTo(new[]
        {
            MakeError(
                "Migration 'm5' declares a dependency on itself. " +
                "The dependency cannot be satisfied."
            )
        });
    }

    [Test]
    public void Validate_DependsOn_LaterThanSelf()
    {
        var migration = new Migration("m5")
        {
            Path      = @"/test/m5/_Main.sql",
            DependsOn = ImmutableArray.Create(MakeReference("m6"))
        };

        Validate(migration).ShouldBeFalse();

        migration.Diagnostics.ShouldBeEquivalentTo(new[]
        {
            MakeError(
                "Migration 'm5' declares a dependency on migration 'm6', " +
                "which must run later in the sequence. " +
                "The dependency cannot be satisfied."
            )
        });
    }

    [Test]
    // Validating for Pre
    [TestCase(Pre,  Pre,  Pre )]
    [TestCase(Pre,  Core, Pre )]
    [TestCase(Core, Core, Pre )]
    [TestCase(Post, Core, Pre )]
    [TestCase(Post, Post, Pre )]
    // Validating for Core
    [TestCase(Pre,  Core, Core)]
    [TestCase(Core, Core, Core)]
    [TestCase(Post, Core, Core)]
    [TestCase(Post, Post, Core)]
    // Validating for Post
    [TestCase(Post, Post, Post)]
    public void Validate_Applicability_Allowed(
        MigrationPhase authoredPhase,   // the phase the developer wrote it for
        MigrationPhase plannedPhase,    // the phase the engine actually scheduled it for
        MigrationPhase validatingPhase) // the phase someone wants to run
    {
        _session.Setup(s => s.CurrentPhase).Returns(validatingPhase);

        var migration = new Migration("m") { Path = @"/test/m/_Main.sql", };

        migration[authoredPhase].IsRequired   = true;
        migration[authoredPhase].Sql          = "EXAMPLE";
        migration[authoredPhase].PlannedPhase = plannedPhase;

        Validate(migration).ShouldBeTrue();

        migration.Diagnostics.ShouldBeEmpty();
    }

    [Test]
    // Validating for Core
    [TestCase(Pre,  Pre,  Core)]
    // Validating for Post
    [TestCase(Pre,  Pre,  Post)]
    [TestCase(Pre,  Core, Post)]
    [TestCase(Core, Core, Post)]
    [TestCase(Post, Core, Post)]
    public void Validate_Applicability_BlockedByEarlierPhase(
        MigrationPhase authoredPhase,   // the phase the developer wrote it for
        MigrationPhase plannedPhase,    // the phase the engine actually scheduled it for
        MigrationPhase validatingPhase) // the phase someone wants to run
    {
        _session.Setup(s => s.CurrentPhase).Returns(validatingPhase);

        var migration = new Migration("m") { Path = @"/test/m/_Main.sql", };

        migration[authoredPhase].IsRequired   = true;
        migration[authoredPhase].Sql          = "EXAMPLE";
        migration[authoredPhase].PlannedPhase = plannedPhase;

        Validate(migration).ShouldBeFalse();

        migration.Diagnostics.ShouldBeEquivalentTo(new[]
        {
            MakeError(string.Format(
                "Cannot apply migration 'm' to database [s].[d] in the {0} " +
                "phase because the migration has code that must be applied " +
                "in an earlier phase first.",
                validatingPhase
            ))
        });
    }

    [Test]
    public void Validate_Missing_Partial()
    {
        var migration = new Migration("m")
        {
            State = MigrationState.AppliedCore,
            Post  = { PlannedPhase = Post }
        };

        Validate(migration).ShouldBeFalse();

        migration.Diagnostics.ShouldBeEquivalentTo(new[]
        {
            MakeError(
                "Migration 'm' is only partially applied to database [s].[d] "  +
                "(through the Core phase), but the code for the migration was " +
                "not found in the source directory. It is not possible to "     +
                "complete this migration."
            )
        });
    }

    [Test]
    public void Validate_Missing_NotApplied()
    {
        var migration = new Migration("m")
        {
            State = MigrationState.NotApplied,
            Post  = { PlannedPhase = Post }
        };

        Validate(migration).ShouldBeFalse();

        migration.Diagnostics.ShouldBeEquivalentTo(new[]
        {
            MakeError(
                "Migration 'm' is registered in database [s].[d] but is not "    +
                "applied in any phase, and the code for the migration was not "  +
                "found in the source directory. It is not possible to complete " +
                "this migration."
            )
        });
    }

    private bool Validate(params Migration[] migrations)
        => new MigrationValidator(_context.Object)
            .Validate(new MigrationPlan(ImmutableArray.Create(migrations)));

    private static MigrationReference MakeReference(string name)
        => new(name);

    private static MigrationReference MakeReference(Migration migration)
        => new(migration.Name) { Migration = migration };

    private static MigrationDiagnostic MakeWarning(string message)
        => new(isError: false, message);

    private static MigrationDiagnostic MakeError(string message)
        => new(isError: true, message);
}
