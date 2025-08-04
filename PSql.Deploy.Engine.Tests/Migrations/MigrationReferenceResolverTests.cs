// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationReferenceResolverTests
{
    [Test]
    public void Resolve_Empty()
    {
        MigrationReferenceResolver.Resolve(migrations: default);
    }

    [Test]
    public void Resolve_NoReferences()
    {
        var a = new Migration("a");
        var b = new Migration("b");

        MigrationReferenceResolver.Resolve([a, b]);

        a.DependsOn.ShouldBeEmpty();
        b.DependsOn.ShouldBeEmpty();
    }

    [Test]
    public void Resolve_BackwardReference()
    {
        var a = new Migration("a");
        var b = new Migration("b") { DependsOn = Refs("a") };

        MigrationReferenceResolver.Resolve([a, b]);

        a.DependsOn.ShouldBeEmpty();
        b.DependsOn.ShouldBeEquivalentTo(ImmutableArray.Create(Resolved(a)));
    }

    [Test]
    public void Resolve_ForwardReference()
    {
        var a = new Migration("a") { DependsOn = Refs("b") };
        var b = new Migration("b");

        MigrationReferenceResolver.Resolve([a, b]);

        a.DependsOn.ShouldBeEquivalentTo(ImmutableArray.Create(Unresolved("b")));
        b.DependsOn.ShouldBeEmpty();
    }

    [Test]
    public void Resolve_SelfReference()
    {
        var a = new Migration("a") { DependsOn = Refs("a") };

        MigrationReferenceResolver.Resolve([a]);

        a.DependsOn.ShouldBeEquivalentTo(ImmutableArray.Create(Unresolved("a")));
    }

    [Test]
    public void Resolve_UnknownReference()
    {
        var a = new Migration("a") { DependsOn = Refs("x") };

        MigrationReferenceResolver.Resolve([a]);

        a.DependsOn.ShouldBeEquivalentTo(ImmutableArray.Create(Unresolved("x")));
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

        MigrationReferenceResolver.Resolve([a, b, c]);

        a.DependsOn.ShouldBeEmpty();
        b.DependsOn.ShouldBeEquivalentTo(ImmutableArray.Create(Unresolved(A)));
        c.DependsOn.ShouldBeEquivalentTo(ImmutableArray.Create(Unresolved(B)));
    }

    private static ImmutableArray<MigrationReference> Refs(params string[] names)
        => names.AsReadOnlySpan().SelectImmutable(Unresolved);

    private static MigrationReference Unresolved(string name)
        => new(name);

    private static MigrationReference Resolved(Migration migration)
        => new(migration.Name) { Migration = migration };
}
