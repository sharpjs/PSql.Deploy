# Diferences between migration and seed code

Maybe these inconsistencies should be fixed.

- SeedConsole does not take a `session` parameter in its methods, but
  `MigrationConsole` does (used only to obtain the `CurrentPhase`).

- `LoadedSeed` is a wrapper around `Seed`.  Migrations are represented by a
  single `Migration` class, but perhaps could be split into `Migration`,
  `LoadedMigration`, `AppliedMigration`, and `MergedMigration` wrappers
  classes.
