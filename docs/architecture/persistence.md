# Persistence

`ApplicationDbContext` in Infrastructure owns Country, API-client, and audit data. It discovers entity configurations and installs `AuditSaveChangesInterceptor`. `IdentityContext` in Identity owns ASP.NET Core Identity data and `RefreshTokens`. Both use SQL Server and the same configured connection-string key, but have separate migration histories and snapshots.

`GenericRepository<T>` supports integer-key `BaseEntity` records; `GenericGuidRepository<T>` supports `BaseGuidEntity` records and exposes `IQueryable` for audit pagination. Generic reads normally filter `IsDelete`; Country handlers add `IsActive`. Repository-pattern mutations use `IUnitOfWork`; Identity services work through Identity managers/context/unit of work.

No global EF query filter is configured. Soft-deletion behavior is enforced by repository predicates, and Country's unfiltered unique indexes retain soft-deleted values. Treat the resulting pre-check/database conflict as an observed risk.

Application migrations belong to `library.management.infrastructure/Migrations` with `ApplicationDbContext`; Identity migrations belong to `library.management.identity/Migrations` with `IdentityContext`. Generate migrations only through the repository migration skill and an approved migration blueprint. Never apply one without separately explicit authority.

Known observations: the initial Identity migration contains default and renamed role-table artifacts; Country's database-generated ID may be unavailable to the audit interceptor before save; and audit query pagination leaks EF work into Application.

Role names are runtime data seeded through ASP.NET Core Identity's `RoleManager<IdentityRole>` into its active `AspNetRoles` mapping. Registration and role changes update `IdentityUserRoles` through `UserManager<ApplicationUser>` in an Identity transaction. No schema migration was required for the three roles.
