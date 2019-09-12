﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Storefront.Menu.API.Authorization;
using Storefront.Menu.API.Filters;
using Storefront.Menu.API.Models.DataModel;
using Storefront.Menu.API.Models.IntegrationModel.EventBus;
using Storefront.Menu.API.Models.IntegrationModel.EventBus.RabbitMQ;

namespace Storefront.Menu.API
{
    [ExcludeFromCodeCoverage]
    public sealed class Startup
    {
        public IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApiDbContext>(options =>
            {
                options.UseNpgsql(_configuration["ConnectionString:PostgreSQL"], pgsql =>
                {
                    pgsql.MigrationsHistoryTable(tableName: "__migration_history", schema: ApiDbContext.Schema);
                });
            });

            services.AddMvc(options =>
            {
                options.Filters.Add(new RequestBodyValidationFilter());
            });

            services.AddDefaultCorsPolicy();
            services.AddJwtAuthentication(_configuration.GetSection("Auth"));

            services.AddScoped<IEventBus, RabbitMQEventBus>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors();
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
