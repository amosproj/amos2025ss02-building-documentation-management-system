/*
 * PROGRAM STRUCTURE:
 * -----------------------------------------------------
 * 1. Setup & Logging (Serilog)
 * 2. CORS Policy
 * 3. Controllers & Global Authorization
 * 4. Database Connection & Services
 * 5. Swagger & API Docs
 * 6. JWT Authentication Configuration
 * 7. Middleware (Trace ID, Swagger, Auth)
 * 8. Routing & Health Checks
 */

// ================================================== //
// 🔧 SETUP & LOGGING
// ================================================== //
using System.Diagnostics;          // Added: for Activity (trace IDs)
using BUILD.ING.Data;
using BUILD.ING.Data.Seed;
using BUILD.ING.Models;
using BUILD.ING.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Serilog;                      // Added: Serilog namespace
using Serilog.Context;              // Added: for log context enrichment

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
#endregion

// ----------------- CONNECTION -----------------
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"⛳ Connection String: {conn ?? "null"}");

// ================================================== //
// 🌐 CORS POLICY
// ================================================== //
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
});



// ================================================== //
// 🔒 GLOBAL AUTHORIZATION FOR CONTROLLERS
// ================================================== //
builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.Filters.Add(new AuthorizeFilter(policy));
});

// ================================================== //
// 🗃️ DATABASE & CORE SERVICES
// ================================================== //
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
    options.SchemaFilter<DocumentMetadataPatchRequestExampleSchemaFilter>();

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

/*
// Configure Kestrel to use HTTPS with certificate
var certificatePath = builder.Configuration["ASPNETCORE_Kestrel__Certificates__Default__Path"];
var certificatePassword = builder.Configuration["ASPNETCORE_Kestrel__Certificates__Default__Password"];
*/

/*
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.UseHttps(certificatePath, certificatePassword);  // Use the provided certificate and password
    });
});
*/

// Add health check service
builder.Services.AddHealthChecks()
    .AddCheck<BUILD.ING.HealthChecks.TikaHealthCheck>("tika_health_check", tags: new[] { "tika", "ready" });

// Register HttpClient and TikaService
builder.Services.AddHttpClient();
builder.Services.AddScoped<BUILD.ING.Services.TikaService>();

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
    options.RequireHttpsMetadata = false; // Set to false if testing without HTTPS
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

builder.WebHost.UseUrls("http://0.0.0.0:5000");
var app = builder.Build();

/*
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
*/

// ---------- ADD AUTHENTICATION MIDDLEWARE ----------
// This middleware will authenticate the JWT token in incoming requests

// ---------- DATABASE MIGRATION & SEEDING ----------
// This runs at app startup and ensures database is migrated,
// and seeds a default organization and test user if they don't exist.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate(); //führt Migration beim Start automatisch aus

    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseSeeder.SeedAsync(context).ConfigureAwait(false);
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
        await next.Invoke().ConfigureAwait(false); // Call the next middleware in the pipeline
    }
});

// ----------------- MIDDLEWARE PIPELINE -----------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(MyAllowSpecificOrigins);

//app.UseHttpsRedirection();

app.UseAuthentication();  // must be before Authorization

app.UseAuthorization();

app.MapControllers();

// ================================================== //
// 🌤️ SAMPLE ROUTES & HEALTH ENDPOINTS
// ================================================== //
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


