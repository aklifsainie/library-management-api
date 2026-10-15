# Book loans

`LoansController` exposes `api/v1/loans` to bearer-authenticated Officers. An Officer records an active membership and available Book at checkout; that action approves the loan immediately. The Officer verifies the member and book against an open loan at return. Admin and Superadmin do not implicitly have lending access.

`BookLoan` belongs to the application database. `BookId` references `Book.Id`; `MemberId` references the application-owned `Member` table. Checkout and return Officer IDs refer to Identity users as strings. Historical loans have no checkout Officer ID. One Book is one lendable copy. The filtered unique index on `BookId` prevents concurrent open loans. A return sets `ReturnedAtUtc`; existing loans remain returnable if their Book is later soft-deleted.

The Application handlers use `IBookLoanRepository`, `IBookRepository`, `IMemberRepository`, `IUnitOfWork`, and `ICurrentUserService`. Checkout requires an active, non-deleted membership. Reads use no-tracking EF queries and write View audit entries; writes use the save interceptor.

`GET api/v1/loans` uses `pageNumber` and `pageSize` (defaults 1 and 20, maximum size 100). `POST api/v1/loans` accepts `bookId` and `memberId`; `POST api/v1/loans/{id}/return` accepts `bookId` and `memberId` for counter verification. The `ReplaceIdentityMembersWithMemberships` application migration backfills memberships and loan references before removing the old migration-only Identity FK. It has no safe automatic down migration and requires Identity tables in the same database.
