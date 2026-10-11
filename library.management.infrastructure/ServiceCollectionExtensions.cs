using library.management.application.Interfaces.Commons;
using library.management.application.Interfaces.Repositories;
using library.management.application.Interfaces.Services;
using library.management.application.Interfaces.Validators;
using library.management.application.Shared.Mediator;
using library.management.domain.SharedKernel;
using library.management.infrastructure.ApiClientAuthentication;
using library.management.infrastructure.Persistences.Interceptors;
using library.management.infrastructure.Repositories;
using library.management.infrastructure.Services;
using library.management.infrastructure.SharedKernel;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace library.management.infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {

            // Api Key Related
            services.AddScoped<IApiKeyValidator, ApiKeyValidator>();
            services.AddScoped<IApiClientService, ApiClientService>();

            // Register UnitOfWork (fully-qualified to avoid namespace/type name collision)
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register repositories
            services.AddScoped<ICountryRepository, CountryRepository>();
            services.AddScoped<IBookRepository, BookRepository>();
            services.AddScoped<IBookLoanRepository, BookLoanRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();

            // Audit Trail
            services.AddScoped<AuditSaveChangesInterceptor>();
            services.AddScoped<IAuditLogger, AuditLogger>(); // View / Login / Export

            // Register custom Mediator
            services.AddScoped<IMediator, SimpleMediator>();

            // Register FluentValidation validators from the application assembly
            var applicationAssembly = Assembly.Load("library.management.application");
            services.AddValidatorsFromAssembly(applicationAssembly);

            // Automatically register all IRequestHandler<,> handlers (exclude the ValidationBehavior itself)
            var handlerType = typeof(IRequestHandler<,>);
            var validationBehaviorGeneric = typeof(library.management.application.Shared.Mediator.ValidationBehavior<,>);

            foreach (var type in applicationAssembly.GetTypes())
            {
                // Skip the ValidationBehavior generic definition to avoid registering it as a handler implementation
                if (type.IsGenericType && type.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == validationBehaviorGeneric)
                    continue;

                var interfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType)
                    .ToList();

                foreach (var @interface in interfaces)
                {
                    services.AddScoped(@interface, type);
                }
            }

            return services;
        }
    }
}
