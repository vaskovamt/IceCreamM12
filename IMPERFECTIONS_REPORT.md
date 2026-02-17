# Project Check Report (IceCreamM12)

## Executed checks
- `dotnet --info` *(failed: .NET SDK is not installed in this environment)*
- `git ls-files | rg '(/obj/|app\.db$|project\.assets\.json$)'`
- `rg "class OrderService" -n src`
- `rg "class (AuditService|InventoryService|ProductionService)" -n src`
- Manual code review of `src/` and `tests/`

## Imperfections found

### 1) Build risk: ambiguous DI registrations due duplicate service types
`Program.cs` imports both `IceCreamM12.Application.Services` and `IceCreamM12.Infrastructure.Services`, while registering unqualified types like `OrderService`, `InventoryService`, `AuditService`, and `ProductionService`. The same class names exist in both layers, creating ambiguity and high compile risk.

### 2) Architectural duplication
Core services are duplicated almost 1:1 in two projects (`Application` and `Infrastructure`) for the same interfaces (`IOrderService`, `IInventoryService`, `IProductionService`, `IAuditService`). This increases maintenance cost and defect risk (fixes can land in only one copy).

### 3) Domain logic smell: rejection reason is merged into status string
`RejectOrderAsync` stores the reason inside `Status` (e.g., `"Rejected: {reason}"`). Elsewhere, filtering depends on `StartsWith("Pending")`/`StartsWith(status)`. Using free-form status text makes state transitions fragile and hard to query reliably.

### 4) Security/data integrity issue: customer email can be user-controlled
In both `ClientController` and `WorkerController`, order creation uses `model.CustomerEmail` when provided, instead of always binding to the authenticated identity email. This can allow order attribution spoofing.

### 5) Seed data inconsistency
`DbInitializer` seeds all `0.300kg` products with `0.00` price and marks descriptions with `PRICE_TBD`, indicating unfinished production data in default seed.

### 6) Daily inventory check does not reconcile stock
`ExecuteDailyCheckAsync` records audit entries with delta `0` when mismatch is detected, but does not adjust inventory quantities to counted values. If reconciliation is intended, this is a logical gap.

### 7) Repository hygiene issue
Generated build outputs (`obj/**`, including binaries and caches) and `src/Web/app.db` are committed in version control. This bloats the repo and increases merge conflicts.

### 8) Test coverage gap
Only one unit test exists, and it covers `FlavorService` happy path only. Critical flows (ordering, approval/rejection, inventory load/scrap/swap, production, daily check) currently have no automated tests.
