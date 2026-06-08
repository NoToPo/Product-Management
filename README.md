# Product Management API

A product‑management backend for a retail / e‑commerce app (fashion shop), built to demonstrate a **scalable** and **strongly consistent** database + API design.

> **TL;DR** — .NET 10 Web API · Clean Architecture + CQRS · PostgreSQL (relational core + `JSONB` for flexible attributes) · EF Core 10 · FluentValidation · Redis cache‑aside · optimistic concurrency (ETag/`If‑Match`) · atomic, oversell‑safe inventory.

---

## 1. Approach (development process)

1. **Requirement analysis** — domain = fashion shop (products → variants/SKUs → stock, organised by categories); hard constraints = *scalability* + *strong consistency* + *extensible attributes*.
2. **Database design** — pick the engine, model the aggregates, decide where schema flexibility lives, design indexes + concurrency token (see §2, [`docs/erd.md`](docs/erd.md)).
3. **API contract design** — REST resources, request/response **DTOs**, versioning, error format (RFC 7807), concurrency semantics (ETag / `If‑Match`).
4. **Implementation** — Clean Architecture + CQRS: `Domain → Application → Infrastructure → Api`.
5. **Validation** — two layers: FluentValidation (input shape) + domain invariants (business rules).
6. **Performance** — Redis cache‑aside on reads; optimistic concurrency + atomic stock update on writes.
7. **Testing** — unit (domain) + integration (Testcontainers), incl. a concurrent‑reservation *no‑oversell* test.
8. **Docs & delivery** — this report, Postman collection, env‑var list, limitations & future work.

---

## 2. Database design — *which database, and why*

**Choice: PostgreSQL (relational) with a `JSONB` column for flexible attributes — i.e. SQL, not NoSQL.**

| Requirement | Decision | Why |
|---|---|---|
| **Strong consistency** | Relational, ACID database | Inventory/price/catalogue need transactional guarantees. A document store makes multi‑document/transaction guarantees harder to argue. |
| **Support new product features/attributes** | `JSONB` `attributes` column on products & variants | New merchandising fields (`material`, `fit`, `season`, …) need **no migration**. A `GIN` index keeps containment queries fast. Avoids the EAV anti‑pattern. |
| **No oversell under concurrency** | `stock_quantity` / `reserved_quantity` + atomic guarded `UPDATE` + `CHECK` constraints | The DB evaluates the "enough available" guard and the decrement as one operation. |
| **Lost‑update protection** | `xmin` system column as optimistic concurrency token | Zero extra schema; surfaced to clients as an HTTP `ETag`. |

This is the **best of both worlds**: relational integrity where it matters, schema‑less flexibility where the catalogue needs to evolve.

**Schema (snake_case):** `categories`, `products`, `product_variants`. Money is stored as a value object (`price_amount numeric(18,2)` + `price_currency`). Products are **soft‑deleted** (`is_deleted`) to preserve order/history references. Full ERD + index list in [`docs/erd.md`](docs/erd.md).

---

## 3. Technology stack

| Concern | Choice |
|---|---|
| Runtime / API | **.NET 10**, ASP.NET Core Minimal APIs |
| Architecture | **Clean Architecture + CQRS** (MediatR) — read path is cacheable & independently scalable |
| ORM | **EF Core 10** + Npgsql, snake_case naming, `JSONB` mapping, `xmin` concurrency |
| Validation | **FluentValidation** (input) + domain factory invariants (business rules) |
| Result handling | `Result`/`Result<T>` + `Error` — no exceptions for expected failures |
| Caching | **Redis** via `IDistributedCache` (cache‑aside); no‑op cache when Redis absent |
| Errors | **RFC 7807 ProblemDetails** + global exception handler |
| Docs | OpenAPI / Swagger UI |
| Tests | xUnit, FluentAssertions, Moq, **Testcontainers** (PostgreSQL) |

**Layering & dependency direction:** `Api → Infrastructure → Application → Domain`. The Application layer depends only on abstractions (`IAppDbContext`, `IProductCache`, `IInventoryService`), so it stays free of any provider.

---

## 4. API & data handling

**Input** is bound to request DTOs → FluentValidation → MediatR command → domain invariants. Entities are never bound directly.
**Output** is always a **strongly‑typed response DTO** (`ProductResponse`, `VariantResponse`, `PagedResult<T>`) — internals/`JSONB` are never leaked, and the contract is OpenAPI‑documented.

### Endpoints (`/api/v1`)

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/categories` | Create category |
| `GET` | `/categories` | List categories |
| `POST` | `/products` | Create product |
| `GET` | `/products/{id}` | Get product (returns `ETag`) |
| `GET` | `/products` | List — `page`, `pageSize` (max 100), `search`, `categoryId`, `status`, `minPrice`, `maxPrice`, `sort` |
| `PUT` | `/products/{id}` | Update — **requires `If‑Match`** |
| `DELETE` | `/products/{id}` | Soft‑delete |
| `POST` | `/products/{id}/variants` | Add a variant (SKU) |
| `PUT` | `/variants/{id}` | Update variant price/stock — **requires `If‑Match`** |
| `POST` | `/variants/{id}/reserve` | Reserve stock (atomic, oversell‑safe) |
| `POST` | `/variants/{id}/release` | Release a reservation |

**Conventions:** URL versioning (`/api/v1`); `201 Created` + `Location` + `ETag` on create; `204` on delete; consistent ProblemDetails for every error; optional `X‑User‑Id` header populates audit fields.

---

## 5. Performance — caching & concurrency

**Caching (cache‑aside).** `GET /products/{id}` checks Redis first; on a miss it queries the DB, maps to the DTO, and stores it (bounded TTL + sliding window). Every write (`update`, `add variant`, `reserve`, `release`, `delete`) **invalidates** the product key. Reads use `AsNoTracking`. If Redis isn't configured the app falls back to a no‑op cache and still runs.

**Concurrency / strong consistency.**
- **Optimistic concurrency** on updates: the `xmin` token is exposed as an `ETag`; clients send it back via `If‑Match`. A stale token → **409 Conflict** (missing token → **428**). A fast pre‑check plus EF's concurrency token together close the lost‑update window.
- **Oversell prevention** on stock: reservations run as a single atomic statement —
  `UPDATE product_variants SET reserved_quantity = reserved_quantity + n WHERE id = … AND (stock_quantity − reserved_quantity) >= n`.
  Concurrent requests serialise on the row, so the available quantity can never go negative. `CHECK` constraints are a final backstop. *Proven by the `Concurrent_reservations_never_oversell` integration test: 20 simultaneous reservations against stock 10 → exactly 10 succeed, 10 get 409.*

---

## 6. Edge cases covered

- Duplicate **SKU** / **slug** → `409` (app pre‑check + unique index backstop).
- Stale / missing **ETag** on update → `409` / `428`.
- **Oversell** attempt → `409`; over‑release → `409`.
- Validation: empty name, negative price/stock, bad currency, bad enum, `pageSize` over cap, `minPrice > maxPrice` → `400` with per‑field details.
- **Soft‑deleted** products are hidden from reads (global query filter) and not mutable.
- Setting on‑hand stock below already‑reserved → `409`.
- Activating a product with no variants → `409`.
- Slugs normalise unicode/diacritics (e.g. `Áo Khoác` → `ao-khoac`).
- Unhandled DB unique violations mapped to `409` (not `500`).

---

## 7. Getting started

### Option A — everything in Docker (one command)
```bash
cp .env.example .env        # optional; defaults work
docker compose up --build   # postgres + redis + api
# API:     http://localhost:8080
# Swagger: http://localhost:8080/swagger
```

### Option B — infra in Docker, API on the host
```bash
docker compose up -d postgres redis
dotnet run --project src/ProductManagement.Api
# Swagger: http://localhost:5080/swagger  (or the printed port)
```

On startup the API **applies migrations and seeds** a small fashion catalogue (idempotent). Run tests with `dotnet test` (integration tests start their own throwaway PostgreSQL via Testcontainers — Docker must be running).

A ready‑to‑use **Postman collection** is in [`docs/ProductManagement.postman_collection.json`](docs/ProductManagement.postman_collection.json) — it chains requests (capturing ids/ETags automatically) and includes the concurrency and oversell scenarios.

---

## 8. Environment variables

| Variable | Default | Used by |
|---|---|---|
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | `postgres` / `postgres` / `productdb` | docker‑compose (Postgres) |
| `POSTGRES_PORT` | `5432` | docker‑compose |
| `REDIS_PORT` | `6379` | docker‑compose |
| `API_PORT` | `8080` | docker‑compose (API) |
| `ASPNETCORE_ENVIRONMENT` | `Development` | API |
| `ConnectionStrings__Database` | see `appsettings.json` | API (overrides config) |
| `ConnectionStrings__Redis` | `localhost:6379` | API (empty ⇒ no‑op cache) |
| `Database__AutoMigrate` | `true` | API (migrate + seed on startup) |

---

## 9. Limitations

- **Single database** — no read replicas / sharding (see §10).
- **No authentication / multi‑tenancy** — endpoints are open; caller identity is a simple `X‑User‑Id` header for audit only.
- **Attribute querying** — `attributes` are stored/indexed as `JSONB` (GIN), but the list endpoint does not yet expose attribute‑containment filters (kept provider‑agnostic in the Application layer).
- **Search** is `LOWER(...) LIKE` on name/brand — fine at this scale, not a full‑text engine.
- **Reservation lifecycle** is manual (reserve/release); no automatic expiry of abandoned reservations.

---

## 10. Future improvements

- **Attribute filters** on the list endpoint via PostgreSQL `@>` containment (the GIN index already supports it).
- **Idempotency‑Key** on create to make POSTs safely retryable.
- **Cursor/keyset pagination** for very large catalogues.
- **Outbox + events** (e.g. `ProductPublished`, `StockReserved`) for search indexing / cross‑service sync.
- **Full‑text search** (PostgreSQL `tsvector` or Elasticsearch) and faceted browsing.
- **Read replicas** for the read path; **partitioning/sharding** by category or tenant for write scale.
- **AuthN/Z** (JWT + roles) and rate limiting.
- **Reservation TTL** with a background job to auto‑release expired holds.

---

## Project layout
```
src/
  ProductManagement.Domain/         # entities, value objects, Result/Error, enums
  ProductManagement.Application/    # CQRS handlers, DTOs, validators, behaviors, abstractions
  ProductManagement.Infrastructure/ # EF Core, JSONB, migrations, Redis, atomic inventory, seed
  ProductManagement.Api/            # Minimal API endpoints, ProblemDetails, Swagger
tests/
  ProductManagement.UnitTests/          # domain factories + invariants
  ProductManagement.IntegrationTests/   # Testcontainers: endpoints + concurrency/oversell
docs/                                # ERD + Postman collection
```
