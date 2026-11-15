Tools -> NuGet Package Manager -> Package Manager Console
1.Add-Migration [Migration Name] -Context ApplicationDbContext (To undo this action, use Remove-Migration).
2.Update-Database -Context ApplicationDbContext
3.Script-Migration -Output "RagData/MigrationsScript/{lastMigationName}.sql" -Context ApplicationDbContext -From {previousMigrationName}