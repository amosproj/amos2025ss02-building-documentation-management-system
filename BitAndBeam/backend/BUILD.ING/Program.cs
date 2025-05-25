using BUILD.ING.Data;
using BUILD.ING.Models;
using BUILD.ING.Swagger;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"⛳ Connection String: {conn ?? "null"}");

builder.Services.AddControllers();

// ✅ Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        builder =>
        {
            builder.WithOrigins("http://localhost:8080") // Your Angular frontend URL
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

// Add DB context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(conn)
           .EnableSensitiveDataLogging()
           .LogTo(Console.WriteLine, LogLevel.Information));

// Swagger setup
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "BUILD.ING API", Version = "v1" });
    options.SchemaFilter<BuildingRequestExampleSchemaFilter>();
});

// Health checks and HttpClient
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient();

var app = builder.Build();

// ✅ Enable CORS before routing and endpoints
app.UseCors("AllowAngularApp");

// Run migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Static files
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider("/app/documents"),
    RequestPath = "/documents"
});

app.MapControllers();

// Custom endpoints
app.MapGet("/weatherforecast", () =>
{
    var summaries = new[] {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        )).ToArray();

    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapHealthChecks("/healthz");
app.MapGet("/", () => "🚀 API is running! Visit /swagger , /weatherforecast or /healthz.");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
