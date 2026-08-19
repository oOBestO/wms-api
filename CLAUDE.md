# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

ASP.NET Core Web API (VB.NET) backing the `wms` Vue frontend (sibling project at `../wms`). Implements two resources: `Material` (raw-material inventory / คลังวัตถุดิบ) and `StockTransaction` (an append-only log of every stock adjustment, backing the frontend's "ประวัติการเบิก-เข้าสต๊อก" history page), matching the API contracts already defined in the frontend's `src/api/` and `src/types/`.

## Commands

- Build: `dotnet build`
- Run (dev, port 3000, matches frontend's `VITE_API_BASE_URL`): `dotnet run`
- Swagger UI (Development env only): `http://localhost:3000/swagger`
- No tests exist yet. Once a test project is added, document how to run the full suite and a single test here.

## Stack

- Language: VB.NET
- Target framework: net8.0
- ASP.NET Core Web API (`Microsoft.NET.Sdk.Web`), Kestrel
- EF Core 8 (`Microsoft.EntityFrameworkCore.SqlServer`) against SQL Server
- Swashbuckle for Swagger/OpenAPI in Development

## Architecture

- `Program.vb` — app bootstrap: configures EF Core (`WmsDbContext`) with the `WmsDatabase` connection string, CORS (policy `VueFrontend`, origins from config `Cors:AllowedOrigins`, defaults to the Vite dev ports 5173/4173), controllers, and Swagger. Calls `db.Database.EnsureCreated()` on startup instead of running migrations, followed by a guarded raw-SQL `CREATE TABLE IF NOT EXISTS`-equivalent for `StockTransactions` (see below) — needed because `EnsureCreated()` was already a no-op against the pre-existing `wms` database by the time that table was added.
- `Models/Material.vb` — EF Core entity: `Id` (Guid PK), `Name`, `Unit`, `Quantity`, `MinQuantity` (nullable), `Note` (nullable), `CreatedAt`, `UpdatedAt`. `Quantity`/`MinQuantity` are mapped to `decimal(18,3)` in `Data/WmsDbContext.vb`.
- `Models/StockTransaction.vb` — EF Core entity logging one stock adjustment: `Id` (Guid PK), `MaterialId`, `MaterialName`/`Unit` (denormalized at write time, so history stays readable even if a material is later renamed or deleted — there's no FK/navigation to `Material`), `Delta`, `QuantityBefore`, `QuantityAfter`, `CreatedAt`. Rows are only ever inserted, never updated or deleted, by `MaterialsController.AdjustStock`.
- `Data/WmsDbContext.vb` — the single `DbContext`, exposes `Materials` and `StockTransactions`.
- `Dtos/MaterialDtos.vb` — `CreateMaterialDto`, `UpdateMaterialDto` (no `Quantity` — stock changes go through adjust-stock, not update), `AdjustStockDto` (`Delta`).
- `Controllers/MaterialsController.vb` — REST endpoints under `api/materials`: `GET` (list), `GET {id}`, `POST` (create), `PATCH {id}` (update metadata), `POST {id}/adjust-stock` (relative stock change, rejects results < 0, and inserts a `StockTransaction` row in the same request before saving), `DELETE {id}`.
- `Controllers/StockTransactionsController.vb` — read-only endpoint `GET api/stock-transactions`, returns all transactions newest-first. No create/update/delete endpoints exist for this resource on purpose — rows are only ever written as a side effect of `AdjustStock`.
- JSON is serialized camelCase by ASP.NET Core's default `System.Text.Json` settings, matching the frontend's TypeScript shapes exactly — no custom serializer config needed.

### Why `EnsureCreated()` instead of EF Core Migrations

EF Core's migrations code generator only ships a C# implementation; it does not support scaffolding migration files for VB.NET projects (`dotnet ef migrations add` fails with "The project language 'VB' isn't supported..."). Since there's no VB-compatible generator, this project uses `EnsureCreated()` to build the schema from the current model at startup. This means schema changes are not tracked/versioned — if a model changes or a new entity is added, the table must be altered/created manually (or the project moved to a C# class library just for the DbContext/migrations, referenced from here, if migration history becomes necessary). `StockTransaction` is the first concrete example of this: since `EnsureCreated()` had already created the `wms` database (with just `Materials`) in an earlier session, adding the `StockTransactions` `DbSet` to the model did nothing on its own — `EnsureCreated()` only builds schema for a database that doesn't exist yet, it never diffs an existing one. The fix was a guarded `IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'StockTransactions') BEGIN CREATE TABLE ... END` run via `db.Database.ExecuteSqlRaw(...)` right after `EnsureCreated()` in `Program.vb`. It's idempotent (safe to run every startup) and correct for both a fresh database (where `EnsureCreated()` already created the table and this is a no-op) and the pre-existing one. Any future new table added the same way should follow this same guarded-raw-SQL pattern.

## Database

- SQL Server, default (`MSSQLSERVER`) local instance, `wms` database, SQL login `wms` (mixed-mode auth). Connection string lives in `appsettings.json` under `ConnectionStrings:WmsDatabase`.
- The local SQL Server instance was originally Windows-Authentication-only with TCP/IP disabled; it was reconfigured (mixed-mode auth enabled, TCP/1433 enabled, `wms` login + database created, `db_owner` granted) to match the credentials the user already had saved in their DB client. If this API is ever pointed at a different machine/instance, replicate that setup or adjust the connection string accordingly.

## Frontend integration

- Sibling project `../wms` (Vue 3 + TypeScript + Vite) is the consumer. Its `.env` sets `VITE_API_BASE_URL=http://localhost:3000/api`, which is why this API's dev profile (`My Project/launchSettings.json`) is pinned to port 3000. Note: VB.NET SDK-style projects read launch profiles from `My Project/launchSettings.json`, not `Properties/launchSettings.json` (the C# convention) — `dotnet run` silently falls back to Kestrel's default port/environment if the file isn't in `My Project/`.
- CORS is required (frontend and backend run on different ports in dev) and is already configured for the Vite dev server origins.
