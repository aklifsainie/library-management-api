using library.management.application.Interfaces.Repositories;
using library.management.domain.Models.Entities;
using library.management.infrastructure.Contexts;

namespace library.management.infrastructure.Repositories;

public sealed class BookRepository : GenericRepository<Book>, IBookRepository
{
    public BookRepository(ApplicationDbContext context) : base(context)
    {
    }
}
