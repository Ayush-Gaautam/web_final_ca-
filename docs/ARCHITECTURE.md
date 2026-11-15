# Architecture and source map

```mermaid
flowchart LR
  Browser[Tablet/browser Razor UI] --> MVC[ASP.NET Core MVC controllers]
  Client[Postman/mobile client] --> API[Authorized REST API]
  MVC --> Services[ForecastingService and PlanningService]
  API --> Services
  MVC --> EF[EF Core ApplicationDbContext]
  API --> EF
  EF --> SQLite[(Existing app.db)]
  EF --> SQL[(SQL Server / Docker Compose)]
```

`Program.cs` selects SQLite by default and SQL Server when `DatabaseProvider=SqlServer`. `DbInitializer` preserves SQLite data with `EnsureCreated` plus idempotent indexes, and uses the EF migration history for SQL Server. Domain rules live in `Models`, persistence in `Data`, business calculations in `Services`, and transport validation in `Models/ApiDtos.cs`.
