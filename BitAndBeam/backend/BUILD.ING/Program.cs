// ----------------- SETUP -----------------
// Using directives and configuration setup
using Microsoft.AspNetCore.Authentication.JwtBearer;
using BitAndBeam.Tika;

using BUILD.ING.Data;
using BUILD.ING.Data.Seed;
using BUILD.ING.Models;
using BUILD.ING.Services;
using BUILD.ING.Swagger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;

using Serilog;                      // Added: Serilog namespace
using Serilog.Context;              // Added: for log context enrichment
using System.Diagnostics;          // Added: for Activity (trace IDs)

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

using Serilog;                      // Added: Serilog namespace
using Serilog.Context;              // Added: for log context enrichment
using System.Diagnostics;          // Added: for Activity (trace IDs)

var builder = WebApplication.CreateBuilder(args);

#region ---------- SERILOG CONFIGURATION ----------
// Configure Serilog as the logging provider for the application
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()      // Enrich logs with contextual info
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter()) // Log JSON to console
    .WriteTo.File(
        new Serilog.Formatting.Json.JsonFormatter(),
        "Logs/log-.json",         // Path with rolling files by date
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Tell ASP.NET Core to use Serilog instead of the default logger
builder.Host.UseSerilog();

// ----------------- CONNECTION -----------------
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"⛳ Connection String: {conn ?? "null"}");

// ----------------- CORS POLICY -----------------
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          //policy.WithOrigins("http://localhost:8080") // <-- Angular dev server
                                policy.AllowAnyOrigin() // Allow requests from any origin - only for development
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });


// ----------------- GLOBAL AUTHORIZATION POLICY -----------------
builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.Filters.Add(new AuthorizeFilter(policy));
});
// ----------------- DATABASE & SERVICES -----------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .EnableSensitiveDataLogging()
           .LogTo(Log.Information, LogLevel.Information));

// ----------------- SWAGGER CONFIGURATION -----------------
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    options.SwaggerDoc("v1", new OpenApiInfo { Title = "BUILD.ING API", Version = "v1" });
    options.SchemaFilter<BuildingRequestExampleSchemaFilter>();

    // 🔐 Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and your token.\n\nExample: Bearer abc123"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Add health check services
builder.Services.AddHealthChecks()
    .AddCheck<TikaHealthCheck>("tika_health_check",
        failureStatus: HealthStatus.Degraded, // Don't fail app startup if Tika is down
        tags: new[] { "ready", "tika" }, // Tag for grouping
        timeout: TimeSpan.FromSeconds(5));

builder.Services.AddHttpClient();

// ---------- JWT AUTHENTICATION CONFIGURATION ----------
// Configure JWT Bearer Authentication to secure the API endpoints
var jwtSecret = builder.Configuration["JwtSecret"];
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new Exception("JwtSecret is not configured in appsettings.json or environment variables.");
}

var key = System.Text.Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true; // Set to false if testing without HTTPS
    options.SaveToken = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
        ValidateIssuer = false, // set to true and specify valid issuer in production
        ValidateAudience = false, // set to true and specify valid audience in production
        ClockSkew = TimeSpan.Zero // remove default 5 min buffer for token expiration
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"❌ JWT error: {context.Exception.Message}");
            return Task.CompletedTask;
        }
    };
});

// Register Tika service
builder.Services.AddSingleton<TikaService>();
builder.Services.AddSingleton<TikaHealthCheck>();

var app = builder.Build();

// ---------- ADD AUTHENTICATION MIDDLEWARE ----------
// This middleware will authenticate the JWT token in incoming requests

// ---------- DATABASE MIGRATION & SEEDING ----------
// This runs at app startup and ensures database is migrated,
// and seeds a default organization and test user if they don't exist.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    db.Database.Migrate(); // applies any pending migrations

    // 🌱 Seed a default organization if none exists
    if (!db.Organizations.Any())
    {
        var defaultOrg = new Organization
        {
            Name = "Default Organization",
            CreatedAt = DateTime.UtcNow
        };
        db.Organizations.Add(defaultOrg);
        db.SaveChanges();
        Console.WriteLine("✅ Default organization created.");
    }

    // ✅ Get the first available organization ID
    var orgId = db.Organizations.First().OrganizationId;

    // 🌱 Seed a test user if none exists
    if (!db.Users.Any())
    {
        var testUser = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"), // plain-text: password123
            FirstName = "Test",
            LastName = "User",
            Role = "admin",
            CreatedAt = DateTime.UtcNow,
            OrganizationId = orgId
        };
        db.Users.Add(testUser);
        db.SaveChanges();
        Console.WriteLine("✅ Test user created.");
    }
}

// ---------- MIDDLEWARE TO ADD TRACE ID TO LOG CONTEXT ----------
app.Use(async (context, next) =>
{
    // Check if incoming request already has a trace ID header
    const string traceIdHeaderName = "X-Correlation-ID";
    string traceId = context.Request.Headers.ContainsKey(traceIdHeaderName)
        ? context.Request.Headers[traceIdHeaderName].ToString()
        : Guid.NewGuid().ToString();

    // Add trace ID to response headers so clients can see it
    context.Response.OnStarting(() =>
    {
        context.Response.Headers[traceIdHeaderName] = traceId;
        return Task.CompletedTask;
    });
    // Push TraceId into Serilog’s LogContext so all logs within this request include it
    using (Serilog.Context.LogContext.PushProperty("TraceId", traceId))
    {
        await next.Invoke(); // Call the next middleware in the pipeline
    }
});

// ----------------- MIDDLEWARE PIPELINE -----------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthorization();
app.UseHttpsRedirection();

app.MapControllers();

// Run health checks during startup to verify Tika connectivity
var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();
await healthCheckService.CheckHealthOnStartupAsync(scope.ServiceProvider).ConfigureAwait(false);

// Configure the HTTP request pipeline.
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BUILD.ING API v1");
    c.RoutePrefix = "swagger";
    c.EnableDeepLinking();
    c.DefaultModelExpandDepth(2);
    c.DefaultModelsExpandDepth(1);
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
});
app.UseCors(MyAllowSpecificOrigins);
app.UseHttpsRedirection();

app.UseAuthentication();  // must be before Authorization

app.UseAuthorization();

app.MapControllers();

// ----------------- PUBLIC ROUTES -----------------
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

//Adds health check endpoint that returns HTTP 200
app.MapHealthChecks("/healthz").AllowAnonymous();

// Detailed health check endpoint for Tika
app.MapHealthChecks("/healthz/tika", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("tika"),
    ResponseWriter = HealthCheckExtensions.WriteDetailedJsonResponse,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK, // Still return 200 but with degraded status in body
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    },
    AllowCachingResponses = false
});

// Ready check endpoint that includes Tika
app.MapHealthChecks("/healthz/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckExtensions.WriteDetailedJsonResponse,
    AllowCachingResponses = false
});

// Liveness check endpoint
app.MapHealthChecks("/healthz/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

//Just to set a route at /
app.MapGet("/", () => "🚀 API is running! Visit /swagger , /weatherforecast or /healthz.");

// Ensure the documents folder exists
//app.UseStaticFiles(new StaticFileOptions
//{
//    FileProvider = new PhysicalFileProvider("/app/documents"),
//    RequestPath = "/documents"
//});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int) (TemperatureC / 0.5556);
}
