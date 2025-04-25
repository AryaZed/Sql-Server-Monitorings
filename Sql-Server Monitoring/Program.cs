using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi.Models;
using Sql_Server_Monitoring.Application.BackgroundService;
using Sql_Server_Monitoring.Application.Hub;
using Sql_Server_Monitoring.Application.Services;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Infrastructure.Data;
using Sql_Server_Monitoring.Middleware;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.SpaServices.Extensions;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();

        // Configure the HTTP request pipeline
        ConfigureApp(app, app.Environment);

        app.Run();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add controllers and JSON options
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        // Add API versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });
        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Add Swagger
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SQL Server Manager API",
                Version = "v1",
                Description = "API for managing and monitoring SQL Server databases",
                Contact = new OpenApiContact
                {
                    Name = "Your Organization",
                    Email = "contact@example.com",
                    Url = new Uri("https://example.com")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Add JWT authentication support in Swagger UI
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
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
                        Array.Empty<string>()
                    }
                });
        });

        // CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

                if (allowedOrigins.Length > 0)
                {
                    builder.WithOrigins(allowedOrigins)
                           .AllowAnyMethod()
                           .WithHeaders("Authorization", "Content-Type", "X-Requested-With")
                           .AllowCredentials()
                           .SetIsOriginAllowedToAllowWildcardSubdomains();
                }
                else
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .WithHeaders("Authorization", "Content-Type", "X-Requested-With");
                }
            });
        });

        // Health checks
        services.AddHealthChecks()
            .AddSqlServer(
                configuration.GetConnectionString("DefaultConnection") ?? "",
                name: "sqlserver", 
                tags: new[] { "database", "sql" },
                timeout: TimeSpan.FromSeconds(5))
            .AddCheck("self", () => HealthCheckResult.Healthy(), 
                tags: new[] { "service" });
                
        services.AddHealthChecksUI(setupSettings: setup =>
        {
            setup.SetEvaluationTimeInSeconds(60);
            setup.MaximumHistoryEntriesPerEndpoint(10);
        }).AddInMemoryStorage();

        // Authentication and Authorization
        services.AddAuthentication("Bearer")
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Auth:Authority"];
                options.Audience = configuration["Auth:Audience"];
                // In development environment, we don't require HTTPS metadata
                options.RequireHttpsMetadata = !Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? true;
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminPolicy", policy =>
                policy.RequireRole("admin"));

            options.AddPolicy("UserPolicy", policy =>
                policy.RequireRole("user", "admin"));
        });

        // Register repositories
        services.AddSingleton<IDatabaseRepository, DatabaseRepository>();
        services.AddSingleton<IIssueRepository, IssueRepository>();
        services.AddSingleton<IOptimizationScriptRepository, OptimizationScriptRepository>();
        services.AddSingleton<ISettingsRepository, SettingsRepository>();
        services.AddSingleton<IStoredProcedureRepository, StoredProcedureRepository>();
        services.AddSingleton<IAlertRepository, AlertRepository>();

        // Register services
        services.AddScoped<IDatabaseAnalyzerService, DatabaseAnalyzerService>();
        services.AddSingleton<IDatabaseMonitorService, DatabaseMonitorService>();
        services.AddScoped<IQueryAnalyzerService, QueryAnalyzerService>();
        services.AddScoped<IBackupService, BackupService>();
        services.AddScoped<IStoredProcedureService, StoredProcedureService>();
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        services.AddSingleton<IAlertService, AlertService>();
        services.AddScoped<IDatabaseOptimizerService, DatabaseOptimizerService>();
        services.AddScoped<IAgentJobsService, AgentJobsService>();
        services.AddScoped<IDbccCheckService, DbccCheckService>();
        services.AddScoped<IIdentityColumnService, IdentityColumnService>();

        // Add HTTP client for external API calls
        services.AddHttpClient();

        // Add SignalR for real-time updates
        services.AddSignalR();

        // Add monitoring background service
        services.AddHostedService<MonitoringBackgroundService>();
        
        // Add caching
        services.AddMemoryCache();

        // Add SPA static files support
        services.AddSpaStaticFiles(configuration =>
        {
            configuration.RootPath = "ClientApp/build";
        });
    }

    private static void ConfigureApp(WebApplication app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        // Enable Swagger
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "SQL Server Manager API v1");
            options.RoutePrefix = "swagger";
        });

        // Global exception handling middleware
        app.UseExceptionHandlingMiddleware();

        // Configure middleware
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        if (!env.IsDevelopment())
        {
            app.UseSpaStaticFiles();
        }
        
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        // Health checks
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        
        app.MapHealthChecksUI(options => 
        {
            options.UIPath = "/health-ui";
        });

        // Map controllers and SignalR hubs
        app.MapControllers();
        app.MapHub<MonitoringHub>("/hubs/monitoring");

        // Configure SPA
        app.UseSpa(spa =>
        {
            spa.Options.SourcePath = "ClientApp";

            if (env.IsDevelopment())
            {
                // In development, proxy requests to the React dev server
                spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");
            }
        });
    }
}