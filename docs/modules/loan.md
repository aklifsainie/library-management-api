# Book loans

`LoansController` exposes `api/v1/loans` to bearer-authenticated Members and Admins. A Member borrows one available Book for their own Identity user ID, views only their history, and returns only their loans. Admins can view all loan history and process returns. Superadmin has no implicit library lending access.

`BookLoan` belongs to the application database. `BookId` references `Book.Id`; `UserId` references `IdentityUsers.Id` through a migration-only foreign key because Identity has a separate EF context. One Book is one lendable copy. The filtered unique index on `BookId` prevents concurrent open loans. Borrowing activates immediately, and a return sets `ReturnedAtUtc`. Existing loans remain returnable if their Book is later soft-deleted.

The Application handlers use `IBookLoanRepository`, `IBookRepository`, `IUnitOfWork`, and `ICurrentUserService`. `ILoanUserService` in Identity checks that a borrower is active and not deleted. Member ownership is checked before exposing an individual loan or returning it. Reads use no-tracking EF queries and write View audit entries; writes use the save interceptor.

`GET api/v1/loans` uses `pageNumber` and `pageSize` (defaults 1 and 20, maximum size 100). The application migration `AddBookLoans` creates the table, indexes, and both foreign keys. Its migration-only Identity FK does not appear in the application model snapshot. Apply Identity migrations before this migration in an environment where the databases share the configured connection.
