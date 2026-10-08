using BuildingBlocks.Infrastructure.Logging;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Web;
using Inventory.Api.Endpoints;
using Inventory.Api.Grpc;
using Inventory.Api.Subscribers;
using Inventory.Application;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging("Inventory.Api");

builder.Services.AddTransient<OrderEventsSubscriber>();

builder.Services
    .AddInventoryApplication()
    .AddInventoryInfrastructure(builder.Configuration);

// gRPC: ChaosInterceptor, istemci tarafındaki retry politikasını gözlemlemek için rastgele Unavailable üretir.
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ChaosInterceptor>();
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});
builder.Services.AddGrpcReflection(); // Postman / grpcurl ile proto dosyası olmadan keşif

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

// REST: 5002 (HTTP/1.1), gRPC: 5102 (HTTP/2 - h2c). Bkz. appsettings.json > Kestrel
app.MapInventoryEndpoints();
app.MapGrpcService<InventoryGrpcService>().RequireHost("*:5102");
app.MapGrpcReflectionService().RequireHost("*:5102");
app.MapHealthChecks("/health");

await app.Services.EnsureDatabaseCreatedAsync<InventoryDbContext>(InventorySeeder.SeedAsync);

await app.RunAsync();
