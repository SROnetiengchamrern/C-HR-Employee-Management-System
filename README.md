# CoreHR Employee Management System

Full-stack HR desk for people, attendance, leave, and reports.

- React, TypeScript, and Vite
- Bootstrap 5
- ASP.NET Core Web API
- Entity Framework Core
- Microsoft SQL Server
- ASP.NET Core Identity and JWT bearer tokens
- OpenAPI at `/openapi/v1.json` and Swagger UI at `/swagger`
- xUnit and Vitest
- Docker Compose for SQL Server, the API, and the web app

## Run it locally

SQL Server needs to be running on this machine. The API uses this connection string and creates `HrEmployeeManagementSystem` on startup:

```text
Server=.;Database=HrEmployeeManagementSystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

```powershell
dotnet run --project src/HrEmployeeManagementSystem.Api --launch-profile http
```

In a second terminal:

```powershell
cd frontend
npm install
npm run dev
```

Open http://localhost:5173. The API listens on http://localhost:5080, and the Vite dev server proxies `/api` to it. Swagger UI is at http://localhost:5080/swagger.

## Demo logins

| Role | Email | Password |
| --- | --- | --- |
| Admin | amila@corehr.lk | Admin@123456 |
| Admin | chamrern@gmail.com | Ch1234567890x@ |
| HR | hr@corehr.lk | Hr@123456 |
| Manager | manager@corehr.lk | Manager@123456 |
| Employee | employee@corehr.lk | Employee@123456 |

The database is seeded with 154 employees, departments, attendance history, leave requests, openings, and training. Sign in as Amila to see the admin dashboard.

## Tests

```powershell
dotnet test
cd frontend
npm test
```

API tests use EF Core InMemory, so they do not need SQL Server.

## Docker

```powershell
docker compose up --build
```

The site is at http://localhost:8088 and the API at http://localhost:8080. Compose uses SQL authentication inside the container. The local `appsettings.json` connection string stays on Windows integrated security.
