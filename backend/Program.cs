using System.Text;
using System.Security.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using HAMS.API.Data;
using HAMS.API.Data.Repositories;
using HAMS.API.Services.Interfaces;
using HAMS.API.Services;
using HAMS.API.Extensions;
using System.Net.Security;
using HAMS.API.Middleware;
using HAMS.API.Jobs;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using HAMS.API.Models.Entities;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var environment = builder.Environment;

// ── Fail-fast: refuse to start if the JWT secret is missing or blank ──────
// The secret must be supplied via the JwtSettings__SecretKey environment
// variable (set in docker-compose.yml from .env, or via dotnet user-secrets
// for local development). appsettings.json intentionally contains no secret.
var jwtSecret = configuration["JwtSettings:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException(
        "JwtSettings:SecretKey is not configured. " +
        "Set the JwtSettings__SecretKey environment variable or use 'dotnet user-secrets set JwtSettings:SecretKey <value>'.");
}

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3);
    });
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
    options.InstanceName = "HAMS_";
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = configuration.GetSection("JwtSettings");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
        ClockSkew = TimeSpan.Zero
    };

    // Allow the access token to be read from the HttpOnly cookie set by
    // AuthController. The standard Authorization header still takes precedence;
    // this fallback is only used when the header is absent.
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (string.IsNullOrEmpty(context.Token))
            {
                context.Token = context.Request.Cookies["access_token"];
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PatientOnly", policy => policy.RequireRole("Patient"));
    options.AddPolicy("ClinicianOnly", policy => policy.RequireRole("Clinician"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("ClinicalAccess", policy => policy.RequireRole("Clinician", "Administrator"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
        builder.WithOrigins(allowedOrigins)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials()
               .WithExposedHeaders("Content-Disposition", "Authorization");
    });
});

builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IEhrIntegrationService, EhrIntegrationService>();
builder.Services.AddApplicationServices();

// Repository Pattern - Phase 1
builder.Services.AddScoped<IRepository<Clinician>, Repository<Clinician>>();
builder.Services.AddScoped<IClinicianRepository, ClinicianRepository>();
builder.Services.AddScoped<IRepository<Patient>, Repository<Patient>>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IRepository<Appointment>, Repository<Appointment>>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IRepository<Department>, Repository<Department>>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IRepository<AuditLog>, Repository<AuditLog>>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

builder.Services.AddHttpClient<IEhrIntegrationService>(client =>
{
    var fhirBaseUrl = configuration["Ehr:FhirBaseUrl"] ?? "https://ehr.mockserver.local/fhir";
    client.BaseAddress = new Uri(fhirBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/fhir+json");

    var handler = new SocketsHttpHandler
    {
        SslOptions = new System.Net.Security.SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
            }
        }
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "HAMS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddHangfire(config =>
{
    config.UsePostgreSqlStorage(configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddHangfireServer();

builder.Services.AddScoped<ReminderJob>();

// Health checks — required for docker-compose health check (GET /health)
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: configuration.GetConnectionString("DefaultConnection")!,
        name: "postgres",
        tags: new[] { "db", "ready" });

var app = builder.Build();

var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobManager.AddOrUpdate<ReminderJob>(
    "48-hour-reminders",
    job => job.Send48HourRemindersAsync(),
    "0 * * * *");

recurringJobManager.AddOrUpdate<ReminderJob>(
    "2-hour-reminders",
    job => job.Send2HourRemindersAsync(),
    "*/15 * * * *");

recurringJobManager.AddOrUpdate<ReminderJob>(
    "daily-summary",
    job => job.SendDailySummaryAsync(),
    "0 18 * * *");

await app.SeedDataAsync();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseCustomMiddleware();

// Expose /health for Docker health checks — must be before MapControllers
app.MapHealthChecks("/health");

app.MapControllers();

// Hangfire dashboard — restricted to authenticated Administrators only.
// Without this filter the dashboard is public and exposes all job data.
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAdminAuthFilter() }
});

app.Run();