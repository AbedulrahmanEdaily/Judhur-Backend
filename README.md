# Judhur

Judhur is a smart and secure real-estate platform focused on the Palestinian property market. It connects buyers and sellers through a trusted platform with verified property data, advanced search, interactive maps, and secure communication. It also supports property verification, user management, reports, analytics, and AI-powered valuation tools.

## Run with Docker (local development)

1. Copy `.env.example` to `.env` and fill in every value. `.env` is ignored by Git and by the Docker build.
2. Export and trust the HTTPS development certificate once (PowerShell), using the same password as `CERT_PASSWORD` in `.env`:

   ```powershell
   dotnet dev-certs https -ep "$env:USERPROFILE\.aspnet\https\judhur.pfx" -p <CERT_PASSWORD>
   dotnet dev-certs https --trust
   ```

3. Start the API and SQL Server:

   ```bash
   docker compose up --build
   ```

   Compose reads `docker-compose.yml` and `docker-compose.override.yml` together; the override switches to Development, HTTPS on 7000 with the dev certificate and publishes SQL Server on 1433.

4. The API listens on `https://localhost:7000`, the same address as `dotnet run`, so stop one before starting the other. SQL Server is on `localhost,1433` (user `sa`, password from `.env`).

The API waits for SQL Server to be healthy, applies the migrations and seeds the development data on startup.

To start from an empty database: `docker compose down -v`.

## Deploy to a server

1. Copy the repository and a filled `.env` (including the server-only values) to the server.
2. Start without the development override:

   ```bash
   docker compose -f docker-compose.yml up -d --build
   ```

   The API runs in Production on plain HTTP, bound to `127.0.0.1:8080` only. SQL Server is not published.
3. Put a reverse proxy with a real certificate in front of it, for example Caddy:

   ```
   api.example.com {
       reverse_proxy 127.0.0.1:8080
   }
   ```

4. Check `https://api.example.com/health` — it returns `Healthy` when the API can reach the database.

## Tests

```bash
dotnet test
```

The integration and end-to-end tests start SQL Server in Docker through Testcontainers, so Docker must be running.

## Configuration

Secrets are never stored in `appsettings*.json` or `docker-compose.yml`.

| Where it runs | Secrets come from |
|---|---|
| `dotnet run` | user-secrets (`dotnet user-secrets list --project src/Judhur.Api`) |
| Docker Compose | `.env` |
| Production | the hosting platform's environment variables |

Nested settings use a double underscore as environment variables, for example `JwtSettings:Secret` becomes `JwtSettings__Secret`.

Production also needs `ASPNETCORE_ENVIRONMENT=Production`, `Cors__AllowedOrigins__0`, `Frontend__ConfirmEmailUrl`, `Google__ClientId` and, on the first start, the `AdminSeed__*` values.
