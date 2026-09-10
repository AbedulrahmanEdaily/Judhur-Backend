# Judhur

Judhur is a smart and secure real-estate platform focused on the Palestinian property market. It connects buyers and sellers through a trusted platform with verified property data, advanced search, interactive maps, and secure communication. It also supports property verification, user management, reports, analytics, and AI-powered valuation tools.

## Getting started

### Prerequisites

- .NET 10 SDK
- SQL Server LocalDB (ships with Visual Studio) or SQL Server Developer Edition

### Database setup

The connection string is not committed. Set it in user secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=(localdb)\\MSSQLLocalDB;Database=Judhur;Trusted_Connection=True;TrustServerCertificate=True" \
  --project src/Judhur.Api
```

Then apply the migrations:

```bash
dotnet ef database update --project src/Judhur.Infrastructure --startup-project src/Judhur.Api
```

### Run

```bash
dotnet run --project src/Judhur.Api
```

- Swagger UI: https://localhost:7000/
- Health check: https://localhost:7000/health

