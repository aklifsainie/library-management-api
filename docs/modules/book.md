# Book Module

## Ownership and flow

Book catalog endpoints use `BooksController -> custom IMediator -> Book command/query handler -> IBookRepository -> ApplicationDbContext`. Mutations save through `IUnitOfWork`. The existing `Book` entity, `BookVM`, EF mapping, and `AddBooks` migration supply the persistence model; this CRUD change does not require a new migration.

## Endpoints and observed behavior

Book endpoints use `/api/v1/books`. An authenticated bearer-token user can list active books and retrieve an active book by ID. Only an `Admin` can add, update, or delete a book. `Superadmin` does not implicitly have the business `Admin` role, and API keys do not authorize Book endpoints.

List/GetById use no-tracking reads and exclude soft-deleted and inactive books. List order is title (case-insensitive), then ID. Update replaces title, author, and optional ISBN; it trims supplied text and keeps the existing 200-character title/author and 17-character ISBN limits. Delete sets `IsDelete = true` and `IsActive = false`; subsequent reads and mutations treat the book as unavailable. Invalid input returns 400, and an unavailable ID returns 404. Mutation auditing uses the EF interceptor; successful reads attempt explicit view audits.

## Boundaries and risks

The Book table must already exist in the target `ApplicationDbContext` database through the existing `AddBooks` migration. Book creation uses a database-generated integer ID, so the automatic create audit may capture a pre-generation ID. Soft deletion appears as an audit update. Catalog listing is unpaged, following the approved endpoint contract; review scale before using it for a large collection. Loan and return behavior belongs to a separate feature.
