using library.management.application.Interfaces.Repositories;
using library.management.domain.Models.Entities;
using library.management.infrastructure.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace library.management.infrastructure.Repositories
{
    public class CountryRepository : GenericRepository<Country>, ICountryRepository
    {

        public CountryRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
