using Application.Models;
using Application.Ports;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            var description = errors.Length > 0
                ? string.Join(" | ", errors)
                : "One or more validation errors occurred.";

            var problem = new ErrorResponse
            {
                Type = "https://httpstatuses.com/400",
                Description = description
            };

            var result = new BadRequestObjectResult(problem);
            result.ContentTypes.Add("application/problem+json");
            return result;
        };
    });

builder.Services.AddOpenApi();

builder.Services.AddScoped<ICloudPricingRepository, CloudPricingRepository>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }