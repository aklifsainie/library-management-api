using library.management.identity.Contexts;
using library.management.identity.Interfaces;
using library.management.identity.Models;
using library.management.identity.Services;
using library.management.identity.SharedKernel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace library.management.identity
{
    public static class IdentityServiceRegistration
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services, string connectionString)
        {
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IdentityUnitOfWork>();
           

            services.AddDbContext<IdentityContext>(
                options => options.UseSqlServer(connectionString)
                );

            return services;
        }
    }
}
