# EF Core migration policy

The application never deletes or resets the existing `app.db`. SQLite remains the default provider so the checked-out local database is preserved. SQL Server deployments select `DatabaseProvider=SqlServer` and use the repeatable migration history table.

For a new SQL Server database:

```powershell
dotnet ef database update
```

For an existing database, back it up before applying migrations. Do not run `database drop`, `EnsureDeleted`, or destructive custom SQL. If a schema conflict is reported, stop and review the migration history rather than deleting data.
