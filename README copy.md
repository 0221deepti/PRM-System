# PRM Solution

## What this project is

PRM is a .NET-based Project Resource Management system with:
- `PRM.Api` — ASP.NET Core Web API backend
- `PRM.Application` — business logic and service interfaces
- `PRM.Infrastructure` — persistence, scheduling, AI provider integration, authentication
- `PRM.Domain` — core entities, enums, and exceptions
- `PRM.Client` — console application client for role-based access
- `tests/PRM.Tests` — placeholder test project

## Key features

- JWT-based authentication and role-based authorization
- User and employee management
- Projects, milestones, allocations, and timesheets
- AI-assisted skill matching and project risk summaries
- Background scheduler for employee/project health status updates
- SQLite persistence with EF Core

## Quick start

### 1. Start the API

Open the solution in Visual Studio or use the .NET CLI:

```powershell
cd src/PRM.Api
dotnet run
```

The API runs on `http://localhost:5000/` by default.

### 2. Run the console client

In a second terminal:

```powershell
cd src/PRM.Client
dotnet run
```

### 3. Log in

Default seeded admin credentials:
- username: `admin`
- password: `Admin@1234`

The seeded admin account is set to require a password change on first login.

## Notes

- The client app currently supports login and role menu navigation.
- Most menu items are placeholders and show a "not yet implemented" warning.
- System configuration includes AI provider settings, but a valid API key is required for AI features.
- The backend automatically applies database migrations and seeds initial data on startup.

## Recommended next steps

- Add client screens for user, employee, project, allocation, and timesheet management.
- Add unit and integration tests for services and controllers.
- Configure and verify AI provider keys in the system configuration.
- Improve skill management and allocation validation flows.
