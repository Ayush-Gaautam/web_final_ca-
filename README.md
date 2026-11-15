# Mess Demand Forecasting & Food-Waste Management

INT402-aligned ASP.NET Core MVC application using C#, Razor views, EF Core, SQLite/SQL Server, Identity roles, and REST APIs. It avoids ML and unrelated technologies.

## Safe local run

SQLite is the default so the existing `app.db` and its data are preserved:

```powershell
dotnet restore
dotnet run
```

Demo logins:

- Admin: `admin@mess.local` / `Admin123!`
- Manager: `manager@mess.local` / `Manager123!`
- Kitchen staff: `kitchen@mess.local` / `Kitchen123!`
- Student: `student@mess.local` / `Student123!`

After startup: dashboard `/`, operations `/Operations`, admin `/Admin`, Identity login `/Identity/Account/Login`, forecast API `/api/forecast`, and Swagger-free API collection examples in `postman/MessForecast.postman_collection.json`.

The app never runs `EnsureDeleted`, `database drop`, or a destructive reset. Existing SQLite data is retained; startup only creates missing objects and idempotent indexes.

## SQL Server with Docker Compose

```powershell
docker compose up --build
```

The web container selects SQL Server through `DatabaseProvider=SqlServer`, waits for the SQL Server health check, and applies the repeatable EF migration history. For Visual Studio/LocalDB, set `DatabaseProvider=SqlServer` and configure `ConnectionStrings:SqlServer` in user secrets or environment variables.

## Features

- Identity authentication plus Admin, Manager, and Staff authorization.
- Normalized dishes, ingredients, recipes, attendance, calendar events, forecasts, service actuals, and waste records.
- Unique constraints, non-negative quantity/cost validation, duplicate conflict responses, and safe soft-delete for dishes.
- Statistical forecast using weighted moving average, weekday adjustment, calendar-event adjustment, MAE, MAPE, and confidence.
- Responsive dashboard and operations tables designed for tablet service capture.
- Students can book/cancel meals before the 10:00 same-day cutoff and submit feedback through REST endpoints; kitchen staff can capture service data; managers can review forecast metrics; admins can manage users and halls.
- Authorized REST CRUD: `/api/dishes`, `/api/ingredients`, `/api/attendance`, `/api/events`, `/api/actuals`, `/api/waste`; forecast and reporting endpoints are also available.
- Postman examples in `postman/MessForecast.postman_collection.json`.
- Source documentation and syllabus mapping in `docs/`.

## Database migrations

The migration policy is documented in `Data/Migrations/README.md`. Before applying migrations to an existing SQL Server database, take a backup and inspect migration history. Do not delete the database to resolve a migration conflict.

## Validation

```powershell
dotnet build web_ca_3.csproj
dotnet test Tests\Tests.csproj
```

See `docs/ARCHITECTURE.md` and `docs/SYLLABUS-MAPPING.md` for the source map and coverage evidence.
