using BuildingBlocks.Infrastructure.Logging;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Web;
using Ordering.Api.Endpoints;
using Ordering.Api.Subscribers;
using Ordering.Application;
using Ordering.Infrastructure;
using Ordering.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging("Ordering.Api");

// CAP subscriber'ları (driving adapter). CAP, ICapSubscribe uygulayan kayıtlı servisleri tarar.
builder.Services.AddTransient<InventoryEventsSubscriber>();

builder.Services
    .AddOrderingApplication()
    .AddOrderingInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();

app.MapOrderEndpoints();
app.MapCustomerEndpoints();
app.MapHealthChecks("/health");

await app.Services.EnsureDatabaseCreatedAsync<OrderingDbContext>();

await app.RunAsync();
