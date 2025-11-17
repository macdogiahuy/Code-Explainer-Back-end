# Code-Explainer-Back-end

Back-end of Code Explainer project. AI for SE

## Table of content

- [Installation](#installation)
- [Usage](#usage)
- [Structure](#project-structure)
- [Environment](#environment)
- [License](#license)

## Installation

The project required [.NET](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) SDK 9

**NOTE:** Install dotnet ef core (If already installed, skip this section)

```csharp
dotnet tool install --global dotnet-ef
```

## Usage

- Clone the repository

```bash
git clone https://github.com/kleqing/Code-Explainer-Back-end.git
```

- Build

```bash
dotnet build
```

- Create database from model (recommended: use the local tool manifest to avoid global installs)

```bash
# (recommended) create a local tool manifest and install dotnet-ef as a local tool
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 9.0.10
dotnet tool restore

# add a migration (run from repo root)
dotnet tool run dotnet-ef migrations add "Initial" --project CodeExplainer.BusinessObject --startup-project CodeExplainer.WebApi --context ApplicationDbContext

# update database (example using environment variable to provide the connection string)
# PowerShell
$env:ConnectionStrings__DefaultConnection="Host=...;Port=5432;Database=...;Username=...;Password=...;Ssl Mode=Require;Trust Server Certificate=true"
dotnet tool run dotnet-ef database update --project CodeExplainer.BusinessObject --startup-project CodeExplainer.WebApi --context ApplicationDbContext --connection "$env:ConnectionStrings__DefaultConnection"

# or using the environment variable name the app expects (when running the API)
$env:SQL_CONNECTION_STRING="Host=...;Port=5432;Database=...;Username=...;Password=...;Ssl Mode=Require;Trust Server Certificate=true"
# then run the API which will apply migrations on startup (Program.cs contains db.Database.Migrate())
dotnet run --project CodeExplainer.WebApi/CodeExplainer.WebApi.csproj -c Debug
```

Notes:

- Do NOT commit secrets into repository files. Use environment variables or `dotnet user-secrets` for development:

```bash
# from CodeExplainer.WebApi folder:
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Port=5432;Database=...;Username=...;Password=...;Ssl Mode=Require;Trust Server Certificate=true"
```

- After running migrations, verify the schema and the `__EFMigrationsHistory` table in your PostgreSQL/Neon instance.
- Rotate any credentials that were exposed.

_API Endpoint:_ https://localhost:7077/swagger/index.html

## User Data Example in Database

```json
[
  {
    "UserId": "fbaad9a2-b90b-4d01-9c28-4916559868f3",
    "UserName": "kleqing",
    "Email": "nhan@gmail.com",
    "PasswordHash": "$2a$12$XwDGzci2f28o4TknNwYV.ehtYyCGYjCcleG/BshDlFoAK2ntgcELW", (@Test is the password)
    "EmailConfirmed": true,
    "UserRole": 1,
    "ProfilePictureUrl": "string",
    "RefreshToken": "",
    "RefreshTokenExpiryTime": "",
    "CreatedAt": "2025-10-31 21:32:01.896944",
    "UpdatedAt": "2025-10-31 21:32:01.896944"
  }
]
```

## Environment

- PostgreSQL (Database)
- Jetbrains Rider (Other IDE still work)
- TablePlus (SQL Management)

## License

This project is licensed under the [MIT License](https://github.com/kleqing/Code-Explainer-Back-end/blob/main/LICENSE)
