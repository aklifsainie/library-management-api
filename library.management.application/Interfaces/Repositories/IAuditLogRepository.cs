using library.management.domain.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace library.management.application.Interfaces.Repositories
{
    public interface IAuditLogRepository : IGenericGuidRepository<AuditLog>
    {
    }
}
