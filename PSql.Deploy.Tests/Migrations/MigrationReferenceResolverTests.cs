// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationReferenceResolverTests
{
    [Test]
    public void Resolve_Empty()
    {
        MigrationReferenceResolver.Resolve(default);
    }

    [Test]
    public void Resolve_NoReferences()
    {
        var a = new Migration("a");
        var b = new Migration("b");

        MigrationReferenceResolver.Resolve(new[] { a, b });

        a.DependsOn.Should().BeEmpty();
        b.DependsOn.Should().BeEmpty();
    }

    [Test]
    public void Resolve_BackwardReference()
    {
        var a = new Migration("a");
        var b = new Migration("b") { DependsOn = Refs("a") };

        MigrationReferenceResolver.Resolve(new[] { a, b });

        a.DependsOn.Should().BeEmpty();
        b.DependsOn.Should().BeEquivalentTo(new[] { Resolved(a) });
    }

    [Test]
    public void Resolve_ForwardReference()
    {
        var a = new Migration("a") { DependsOn = Refs("b") };
        var b = new Migration("b");

        MigrationReferenceResolver.Resolve(new[] { a, b });

        a.DependsOn.Should().BeEquivalentTo(new[] { Unresolved("b") });
        b.DependsOn.Should().BeEmpty();
    }

    [Test]
    public void Resolve_SelfReference()
    {
        var a = new Migration("a") { DependsOn = Refs("a") };

        MigrationReferenceResolver.Resolve(new[] { a });

        a.DependsOn.Should().BeEquivalentTo(new[] { Unresolved("a") });
    }

    [Test]
    public void Resolve_UnknownReference()
    {
        var a = new Migration("a") { DependsOn = Refs("x") };

        MigrationReferenceResolver.Resolve(new[] { a });

        a.DependsOn.Should().BeEquivalentTo(new[] { Unresolved("x") });
    }

    [Test]
    public void Resolve_Pseudo()
    {
        const string
            A = Migration.BeginPseudoMigrationName,
            B = "b",
            C = Migration.EndPseudoMigrationName;

        var a = new Migration(A);
        var b = new Migration(B) { DependsOn = Refs(A) };
        var c = new Migration(C) { DependsOn = Refs(B) };

        MigrationReferenceResolver.Resolve(new[] { a, b, c });

        a.DependsOn.Should().BeEmpty();
        b.DependsOn.Should().BeEquivalentTo(new[] { Unresolved(A) });
        c.DependsOn.Should().BeEquivalentTo(new[] { Unresolved(B) });
    }

    private static ImmutableArray<MigrationReference> Refs(params string[] names)
        => names.AsReadOnlySpan().SelectImmutable(Unresolved);

    private static MigrationReference Unresolved(string name)
        => new(name);

    private static MigrationReference Resolved(Migration migration)
        => new(migration.Name) { Migration = migration };
}
