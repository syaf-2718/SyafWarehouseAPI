using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. ADD CONTROLLER AND OPENAPI SERVICES
builder.Services.AddControllers();
builder.Services.AddOpenApi(); // Natively maps OpenAPI endpoints
builder.Services.AddSingleton<SyafWarehouseAPI.Services.CustomerDataStore>(); // Register our in-memory data store as a Singleton service

var app = builder.Build();

// 2. MAP THE ROUTE ENDPOINTS
app.MapOpenApi();
app.MapControllers(); // <-- MAKE SURE THIS LINE IS PRESENT HERE

// 3. MAP SCALAR INTERFACE
app.MapScalarApiReference(options =>
{
    options.WithTitle("Syaf Warehouse API Dashboard")
           .WithTheme(ScalarTheme.DeepSpace);
});

app.UseAuthorization();
app.Run();