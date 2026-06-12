# PRM System — Run Instructions (Simple)

This file explains the minimal steps to build and run the entire solution (API + Console UI) on Windows.

Prerequisites
- .NET SDK 8 installed (dotnet --version should show 8.x)
- Optional: `dotnet-ef` tool for migrations (install if needed)

1) Restore & build the solution

Open a terminal at repository root and run:

```powershell
cd "c:\Users\deepti.dwivedi\OneDrive - InTimeTec Visionsoft Pvt. Ltd.,\Desktop\PRM-system-final\PRM-System"
dotnet restore
dotnet build
```

2) Apply EF Core migrations (create sqlite DB)

If `dotnet ef` is not available, install it:

```powershell
dotnet tool install --global dotnet-ef
# or (if using manifest): dotnet tool restore
```

Run migrations from `PRM.Api`:

```powershell
cd src/PRM.Api
dotnet ef database update
```

This creates `prm.db` in `src/PRM.Api` and runs the seeder (`DbInitializer`). By default the seeder creates an admin user.

3) Run the API server

From `src/PRM.Api` (keep this terminal open):

```powershell
dotnet run
```

By default the API listens on HTTP (http://localhost:5000) and HTTPS (https://localhost:7000). Keep this running.

4) Run the Console Client (UI)

Open a new terminal and run the console client:

```powershell
cd src/PRM.Client
dotnet run
```

The client is a console UI that calls the API. It will prompt for login.

5) Default credentials
- Username: `admin`
- Password: `Admin@1234`

Note: The seeder sets `ForcePasswordChange = true` for admin in the default seeder; you may be prompted to change the password on first login.

6) Quick API test (login with curl or HTTP client)

```powershell
# Replace with the actual API URL if different
curl -X POST http://localhost:5000/api/auth/login -H "Content-Type: application/json" -d '{"username":"admin","password":"Admin@1234"}'
```

7) Reset database (if needed)

Delete the database file and re-run migrations:

```powershell
# Windows
Remove-Item src/PRM.Api\prm.db -Force
# then
cd src/PRM.Api
dotnet ef database update
```

8) Common troubleshooting
- "dotnet: command not found" → install .NET SDK 8
- "dotnet ef not found" → install `dotnet-ef` tool
- Port 5000 in use → stop other app or change launch settings
- "prm.db is locked" → close other processes using the DB and restart

9) Useful file locations
- API entry: `src/PRM.Api/Program.cs`
- Client entry: `src/PRM.Client/Program.cs`
- DbContext: `src/PRM.Infrastructure/Persistence/PrmDbContext.cs`
- Seeder: `src/PRM.Infrastructure/Persistence/DbInitializer.cs`
- App config: `src/PRM.Api/appsettings.json`

If you want, I can create an expanded step-by-step guide with screenshots, or re-seed with richer dummy data. Which would you prefer next?