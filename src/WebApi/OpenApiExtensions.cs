using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace WebApi
{
    public static class OpenApiExtensions
    {
        public static IServiceCollection AddOpenApi(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "CloudCalculator API",
                    Version = "v1"
                });
            });

            return services;
        }

        public static void MapOpenApi(this WebApplication app)
        {
            // Serve generated Swagger as JSON endpoint(s)
            app.UseSwagger();

            // Serve Swagger UI at /swagger
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "CloudCalculator API v1");
                // RoutePrefix left default ("swagger") so UI is available at /swagger
            });
        }
    }
}