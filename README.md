# Mess Demand Forecasting & Food-Waste Management

ASP.NET Core MVC MVP for INT402: C#, Razor views, EF Core, SQL Server, Identity authentication/roles, and REST APIs.

## Run in Visual Studio Community

1. Install the .NET 10 SDK (the current environment does not have the .NET 8 SDK; the project uses the same ASP.NET Core MVC and EF Core architecture and can be retargeted to `net8.0` once the .NET 8 SDK is installed).
2. Install SQL Server LocalDB or change `ConnectionStrings:DefaultConnection` in `appsettings.json`.
3. Open `web_ca_3.csproj`, restore packages, and run with IIS Express or `dotnet run`.
4. The database is created and seeded on first run. Demo login: `admin@mess.local` / `Admin123!`.

## Included MVP flows

- Identity login plus Admin, Manager, and Staff roles.
- Dish, recipe, ingredient, unit-cost, and stock entities.
- Attendance history and event adjustments.
- Weighted moving-average demand forecast with confidence score.
- Ingredient planning primitives through `PlanningService`.
- Post-service actuals and waste quantity/cost entities.
- Dashboard with forecast, stock value, monthly waste cost, and waste capture.
- REST endpoints: `GET /api/forecast`, `GET /api/dishes`, and `GET /api/waste-summary`.

For production, add migrations (`dotnet ef migrations add InitialCreate`) and use a managed SQL Server connection string.
