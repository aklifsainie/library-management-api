# Persistence

`ApplicationDbContext` in Infrastructure owns Country, Book, Member, BookLoan, API-client, and audit data. It discovers entity configurations and installs `AuditSaveChangesInterceptor`. `IdentityContext` in Identity owns staff Identity data and `RefreshTokens`. Both use SQL Server and the same configured connection-string key, but have separate migration histories and snapshots.

`GenericRepository<T>` supports integer-key `BaseEntity` records; `GenericGuidRepository<T>` supports `BaseGuidEntity` records and exposes `IQueryable` for audit pagination. Generic reads normally filter `IsDelete`; Country handlers add `IsActive`. Repository-pattern mutations use `IUnitOfWork`; Identity services work through Identity managers/context/unit of work.

No global EF query filter is configured. Soft-deletion behavior is enforced by repository predicates, and Country's unfiltered unique indexes retain soft-deleted values. Treat the resulting pre-check/database conflict as an observed risk.

Application migrations belong to `library.management.infrastructure/Migrations` with `ApplicationDbContext`; Identity migrations belong to `library.management.identity/Migrations` with `IdentityContext`. Generate migrations only through the repository migration skill and an approved migration blueprint. Never apply one without separately explicit authority.

Known observations: the initial Identity migration contains default and renamed role-table artifacts; Country's database-generated ID may be unavailable to the audit interceptor before save; and audit query pagination leaks EF work into Application.

Role names are runtime data seeded through ASP.NET Core Identity's `RoleManager<IdentityRole>` into its active `AspNetRoles` mapping. Superadmin-created staff accounts and role changes update `IdentityUserRoles` through `UserManager<ApplicationUser>` in an Identity transaction. The membership migration removes legacy Member assignments and the old role after loan backfill.

`BookLoan` is application-owned and now records `BookId`, `MemberId`, optional historical checkout Officer ID, optional return Officer ID, borrowing time, and optional return time. `ReplaceIdentityMembersWithMemberships` backfills `Member` from historical Member-role Identity users and loan borrowers, maps historical loans, revokes Member sessions, and removes the former loan-to-Identity FK. The filtered unique index still allows one open loan per Book. This migration requires Identity tables in the same database and has no safe automatic down path. EF tooling now uses the configured API startup path for `ApplicationDbContext`; that path includes startup role seeding, so migration commands must be aimed deliberately at the intended database. No migration is applied during generation.
