# TodoWebApp

Simple ASP.NET Core Razor Pages To-Do app (.NET 10), with SQLite and xUnit tests.

## Structure
- `TodoWebApp.csproj` — main web app project (root)
- `TodoWebApp.Tests/` — separate unit tests project
- No `.sln` file: `dotnet build` from the root targets the web app directly.

## Run the website
From the main `TodoWebApp` folder:

```powershell
dotnet restore
dotnet build
dotnet run
```

The running app will print its local URL. To use the HTTP launch profile:

```powershell
dotnet run --launch-profile http
```

## Run tests

```powershell
dotnet test TodoWebApp.Tests/TodoWebApp.Tests.csproj
```

## GitHub Actions
Your existing workflow command `dotnet build --configuration Release` works from the repository root, since there is only one `.csproj` directly in that folder. The tests remain in their own folder; to run tests in CI, target their project explicitly.

The app uses SQLite local storage. For production deployment to Azure App Service, use managed persistent storage rather than depending on the instance-local SQLite file.
