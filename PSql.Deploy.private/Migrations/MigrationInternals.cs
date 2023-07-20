namespace PSql.Deploy.Migrations;

/// <summary>
///   Internals of the migration system.
/// </summary>
internal sealed class MigrationInternals : IMigrationInternals
{
    private MigrationInternals() { }

    /// <summary>
    ///   Gets the singleton <see cref="MigrationInternals"/> instance.
    /// </summary>
    public static MigrationInternals Instance { get; } = new();

    /// <inheritdoc/>
    public void LoadContent(Migration migration)
        => MigrationLoader.LoadContent(migration);
}
