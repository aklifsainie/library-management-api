using library.management.application.Interfaces.Services;
using library.management.infrastructure.Persistences.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace library.management.infrastructure.Contexts;

/// <summary>Creates the application context for EF tooling without starting the API or its role seeder.</summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Migration scaffolding only builds the model; this connection is never opened.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(local);Database=LibraryManagementDesignTime;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new ApplicationDbContext(options, new AuditSaveChangesInterceptor(new DesignTimeCurrentUser()));
    }

    private sealed class DesignTimeCurrentUser : ICurrentUserService
    {
        public string? UserId => null;
        public bool IsAuthenticated => false;
        public string? UserName => null;
        public string? UserRole => null;
        public string? IpAddress => null;
        public string? UserAgent => null;
        public string? CorrelationId => null;
    }
}
