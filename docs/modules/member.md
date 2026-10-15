# Membership

Members are application-owned library records, not Identity accounts. Only bearer-authenticated Officers can use `api/v1/members` to create, list, get, update, and deactivate memberships. A membership has a unique member number, full name, email and/or phone contact, and active status. New records require at least one contact method. Member numbers remain reserved after soft deletion. Deactivation is refused while the member has an open loan.

The flow is `MembersController -> custom mediator -> Member handler -> IMemberRepository -> ApplicationDbContext`; writes save through `IUnitOfWork`. Email and phone are marked sensitive for automatic audit masking, while successful reads attempt explicit View audits. The current list is unpaged; scale should be reviewed before a large catalog is introduced.

Legacy Identity users with the old Member role, plus Identity users referenced by historical loans, are backfilled into `Member` by the migration. Historical contact details may be incomplete and require Officer review. Legacy Identity account IDs are retained only for migration traceability; new members never receive login accounts.
