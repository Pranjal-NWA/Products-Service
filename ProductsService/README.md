# Products Service 

A microservice for Nautical Wings' product catalog.

## Tech stack

- ASP.NET Core 8 (Web API, controllers)
- Entity Framework Core 8 + Npgsql (PostgreSQL)
- JWT Bearer authentication (validation only — no Identity, no `UserManager`/`SignInManager`)
- AWS Secrets Manager for production secrets; `dotnet user-secrets` for local development

## Prerequisites

Install these before starting — check each with the command shown:

| Tool | Check with | Get it from |
|---|---|---|
| .NET 8 SDK | `dotnet --version` (should show `8.x`) | https://dotnet.microsoft.com/download |
| PostgreSQL | running locally, or a connection string to a remote instance | https://www.postgresql.org/download |
| EF Core CLI tool | `dotnet ef --version` | install below if missing |

If `dotnet ef --version` fails:

```
dotnet tool install --global dotnet-ef

```

## Setup

1. **Clone the repo, then move into the API project folder:**

cd ProductsService


2. **Restore dependencies** — downloads every NuGet package the project needs:

dotnet restore


3. **Set up your local database.** In PostgreSQL (via `psql` or pgAdmin), create a database and user for this project if you haven't already:
```sql
   CREATE DATABASE productsdb;
   CREATE USER app_user WITH PASSWORD 'app_password';
   GRANT ALL PRIVILEGES ON DATABASE productsdb TO app_user;
```

4. **Configure your local secrets.** These are never stored in the repo — `dotnet user-secrets` keeps them in a file on your own machine, outside the project folder entirely, so they can never accidentally get committed:

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=productsdb;Username=app_user;Password=app_password"
dotnet user-secrets set "Jwt:SecretKey" "<same exact value as the Auth service's Jwt:SecretKey>"

   This must match the Auth service's key exactly — copy it from there rather than generating a new one, or tokens issued by Auth won't validate here.

5. **Create the database tables.** This reads the model classes and generates the actual SQL to build the schema:

dotnet ef migrations add InitialCreate
dotnet ef database update

   You should see a `Products` table in `productsdb` afterward.

6. **Run the project:**

dotnet run

   Watch the terminal output for a line like `Now listening on: http://localhost:5000` — note that port number.

7. **Confirm it's actually working.** Open a browser:

http://localhost:<port from step 6>/healthz

   Expected response:
```json
   {"status":"ok","db":"ok"}
```
   If you see this, the API is running and successfully talking to the database — setup is complete.

## Day-to-day development

Once the initial setup is complete, you do not need to repeat the database and dependency setup every time.

Run the API with:

```bash
cd ProductsService
dotnet run
```

Or use the HTTPS profile:

```bash
dotnet run --launch-profile https
```

To automatically restart the API whenever code changes:

```bash
dotnet watch run
```

## Testing the cross-service auth flow

Admin-only routes (`POST`/`PUT`/`DELETE` on `/api/v1/products`) require a valid `access_token` cookie issued by the Auth service, from a user with the `Admin` role.

1. Log in via the Auth service and capture the `access_token` cookie.
2. Send it on a request here, e.g. `POST /api/v1/products` with `Cookie: access_token=<token>`.
3. `401` on a route that should work usually means the two services' `Jwt:SecretKey` values are out of sync, or this service wasn't restarted after a key change.