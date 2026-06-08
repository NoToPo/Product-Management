# Plan: Product Management API for E-commerce (Assessment)

## Context

This is a take-home assessment: build **product management endpoints** for a retail/e-commerce app (fashion shop) that demonstrate **scalable** and **strongly consistent** database + API design. The working directory `C:\Projects\P\ST` is empty (greenfield). Deliverables required by the brief: code (GitHub), a short report/docs, a Postman collection, env-var list, limitations, and future improvements.

Each major decision below has a **Why** section — those rationales feed directly into the assessment's evaluation criteria (DB design, tech stack, API/data handling, performance).

---

## Development Process (the "Approach" criterion)

Documented explicitly in README so a reviewer sees the *process*, not just the result:

1. **Requirement analysis** — identify the domain (fashion shop: products, variants/SKUs, stock, categories) and the two hard constraints: *scalability* + *strong consistency*.
2. **Database design** — choose engine (SQL vs NoSQL), model aggregates, decide where flexibility lives (JSONB), design indexes & concurrency token. (ERD in `docs/erd.md`.)
3. **API contract design** — define REST resources, request/response **DTOs**, versioning, error format (ProblemDetails), concurrency semantics (ETag/If-Match) — contract-first.
4. **Implementation** — Clean Architecture + CQRS: Domain → Application (handlers/validators) → Infrastructure (EF/Redis) → Api (endpoints).
5. **Validation** — two-layer input validation; strongly-typed output DTOs.
6. **Performance** — Redis cache-aside on reads; optimistic concurrency + atomic stock update on writes.
7. **Testing** — unit (domain/validators) + integration (Testcontainers), incl. a concurrent-reserve/no-oversell test.
8. **Documentation & delivery** — README report, Postman collection, `.env.example`, limitations & future work; `dotnet format`; push to GitHub.

---

## Decisions & Rationale

### 1. Database: **PostgreSQL (relational) + JSONB for flexible attributes**
**Why:**
- The brief explicitly demands **strong consistency**. A relational, ACID database is the natural fit; a document store (MongoDB) makes multi-document transactional guarantees harder to argue. So **SQL over NoSQL**.
- "How does your design support new product features/attributes?" → A pure relational schema is rigid; EAV is a known anti-pattern (slow, hard to query). **PostgreSQL `JSONB`** gives schema-flexible attributes (the NoSQL upside) *inside* a transactional relational DB — best of both worlds. New attributes (e.g. "material", "fit") need **no migration**.
- PostgreSQL JSONB + **GIN index** allows efficient querying/filtering on dynamic attributes — stronger than SQL Server's JSON story, and free/Docker-friendly.
- Concurrency token: Postgres system column **`xmin`** (`UseXminAsConcurrencyToken`) gives optimistic concurrency with zero extra schema.

### 2. Architecture: **Clean Architecture + CQRS (MediatR)**
**Why:**
- Matches the house `dotnet` skill style and is what a reviewer in a .NET shop expects.
- CQRS separates the **read path** (cacheable, `AsNoTracking`, can scale independently) from the **write path** (validated, transactional) — directly supports the *scalability* criterion.
- Result pattern + FluentValidation + pipeline behaviors = clean, testable validation/error handling without throwing for control flow.
- Kept **self-contained** (no proprietary `brenock.*` abstractions, since they're unavailable here) and not over-split — 4 projects only.

### 3. Runnable scope: **Full `docker-compose` + EF migrations + seed**
**Why:** A reviewer should `clone → docker compose up → run` and hit endpoints end-to-end. Demonstrates operational maturity and makes the Postman collection immediately usable.

### 4. Feature scope: **Products + Categories + Variants(SKU) + Inventory + Redis cache**
**Why:** A fashion shop realistically has **variants** (size/color), each a sellable **SKU** with its own price/stock. **Inventory** with a concurrency-safe decrement is the cleanest demonstration of *strong consistency* (no overselling). **Redis cache-aside** demonstrates the *performance/caching* criterion. **Categories** for organization. Each maps to an explicit evaluation criterion — no gratuitous scope.

---

## Target Structure

```
ST/
  ProductManagement.sln
  docker-compose.yml                 # postgres + redis (+ pgadmin optional)
  .env.example                       # documented env vars
  README.md                          # the short report (approach/design/perf/limitations/future)
  docs/
    ProductManagement.postman_collection.json
    erd.md                           # schema + ERD (mermaid)
  src/
    ProductManagement.Domain/        # entities, value objects, Result/Error, enums
    ProductManagement.Application/   # CQRS handlers, DTOs, validators, behaviors, abstractions
    ProductManagement.Infrastructure/# DbContext, EF configs, migrations, repos, Redis cache, seed
    ProductManagement.Api/           # Minimal API endpoints, Program.cs, ProblemDetails, OpenAPI
  tests/
    ProductManagement.UnitTests/         # domain factories + validators
    ProductManagement.IntegrationTests/  # Testcontainers(Postgres): endpoints + concurrency/oversell
```

## Domain Model

- **Category**: Id (Guid v7), Name, Slug (unique), ParentId? (hierarchy), timestamps.
- **Product**: Id, Name, Slug (unique), Description, Brand, `Status` enum (Draft/Active/Archived), CategoryId(FK), **`Attributes` JSONB** (`Dictionary<string,object>`), audit fields, `IsDeleted` (soft delete), `xmin` concurrency token.
- **ProductVariant**: Id, ProductId(FK), **Sku (unique)**, `Money Price` (Amount decimal(18,2) + Currency), **`Attributes` JSONB** (e.g. `{size, color}`), `StockQuantity`, `ReservedQuantity`, concurrency token.
- **Money** value object; **Slug** generated/validated.
- Entity pattern: private ctors, static `Create(...) -> Result<T>` enforcing invariants, private setters, Guid v7 ids, read-only collections.

## Application Layer (CQRS, feature folders)

- Commands: `CreateProduct`, `UpdateProduct`, `SoftDeleteProduct`, `CreateCategory`, `AddVariant`, `UpdateVariant`, `AdjustStock` / `ReserveStock`.
- Queries: `GetProductById`, `ListProducts` (paging + filter + sort), `GetCategoryTree`.
- `Result`/`Result<T>` + `Error.NotFound/Validation/Conflict/...`; `ToHttpResponse()` extensions.
- `ValidationBehavior` (FluentValidation) + `LoggingBehavior` pipeline behaviors.
- Validation in **two layers**: FluentValidation (input shape: required, ranges, non-negative price/stock) + domain invariants in `Create`.

## Infrastructure

- `AppDbContext` with EF Core 10 + Npgsql; JSONB mapping for `Attributes`; `UseXminAsConcurrencyToken`; unique indexes on Sku/Slug; **GIN index** on JSONB; global query filter for `IsDeleted`.
- **Concurrency-safe stock**: `ReserveStock` via atomic guarded update (`UPDATE ... SET reserved = reserved + @q WHERE stock - reserved >= @q`) inside a transaction → prevents overselling; concurrency conflicts → `409`.
- **Redis cache-aside** (`IProductCache` over `IDistributedCache`): cache `GetProductById`, invalidate on write; bounded TTL.
- Migrations + idempotent **seed** (categories, sample fashion products with variants).

## API Layer

- Minimal API, URL-versioned `/api/v1`. Endpoints as static partial classes grouped per aggregate.
- REST: `POST/GET/PUT/DELETE /api/v1/products`, `GET /api/v1/products` (page/pageSize **capped**, `q`, category, status, price range, sort), `POST /api/v1/products/{id}/variants`, `PATCH /api/v1/variants/{id}/stock`, `GET/POST /api/v1/categories`.
- **Optimistic concurrency over HTTP**: `ETag` on GET, `If-Match` required on PUT → mismatch = `412/409`.
- Global exception handler → **RFC 7807 ProblemDetails**; validation errors → `400` with per-field details. OpenAPI/Swagger UI.

**Input & output handling (the "process input / validate output" criterion):**
- **Input**: bound to request DTOs → FluentValidation (shape/range) → MediatR command → domain invariants. Never bind directly to entities.
- **Output**: responses are **strongly-typed response DTOs** (`ProductResponse`, `VariantResponse`, `PagedResult<T>`) mapped from entities — entities/JSONB internals are never leaked; consistent envelope, stable contract, OpenAPI-documented. Paged responses carry total count + paging metadata.

## Edge Cases Covered

Duplicate SKU/slug → 409; concurrent update → 409/412 (ETag); not found → 404; validation (negative price/stock, bad enum, oversized payload) → 400; **oversell prevention** on reserve; soft-deleted items hidden + not mutable; pagination `pageSize` hard cap; empty/whitespace slugs normalized. (Idempotency-Key for create noted as future improvement.)

## Documentation Deliverables

- **README.md** as the report: approach/process, DB design + why SQL+JSONB, tech stack, API design, performance (cache + concurrency), edge cases, env vars, **limitations**, **future improvements**, run instructions.
- **docs/erd.md**: mermaid ERD + index notes.
- **Postman collection** covering all endpoints incl. concurrency (If-Match) and oversell scenarios.
- **.env.example** listing all env vars.

## Verification

1. `docker compose up -d` (postgres + redis) → `dotnet run --project src/ProductManagement.Api` applies migrations + seed.
2. Swagger UI / Postman: create product → add variants → list with filters → update with `If-Match` (happy + stale ETag → 409) → reserve stock past available (→ oversell rejected) → soft delete (→ hidden from list).
3. `dotnet test` — unit (domain/validators) + integration (Testcontainers Postgres) incl. a **concurrent reserve** test proving no oversell.
4. Per global rule: run **`dotnet format`** before declaring done.

## Notes / Limitations (to document)

- Single-DB; read replicas / sharding noted as future scale-out.
- No auth/multi-tenant (out of scope) — endpoints left open with a note.
- Search is DB `ILIKE`/JSONB; full-text/Elastic noted as future.
