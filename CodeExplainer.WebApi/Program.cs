using System.Reflection;
using System.Text;
using System.Text.Json;
using CodeExplainer.BusinessObject;
using CodeExplainer.Services.Implements;
using CodeExplainer.Services.Interfaces;
using CodeExplainer.Shared.Jwt;
using CodeExplainer.Shared.Utils;
using CodeExplainer.WebApi.Hubs;
using dotenv.net;
using MaIN.Core;
using MaIN.Domain.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

namespace CodeExplainer.WebApi;

public class Program
{
    public static void Main(string[] args)
    {
        DotEnv.Load();
        var builder = WebApplication.CreateBuilder(args);
        
        var corsPolicy = "AllowSpecificOrigins";
        
        var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();
        if (allowedOrigins == null || allowedOrigins.Length == 0)
        {
            var originsFromEnv = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
            if (!string.IsNullOrEmpty(originsFromEnv))
            {
                allowedOrigins = originsFromEnv.Split(',', StringSplitOptions.RemoveEmptyEntries);
            }
        }
        
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Database connection string is not set in environment variables.");
            }
        }
        
        // Determine AI provider and API key. Support Gemini and OpenAI via environment variables or configuration.
        var aiProvider = Environment.GetEnvironmentVariable("AI_PROVIDER") ?? builder.Configuration["AI:Provider"] ?? "Gemini";

        // Prefer provider-specific env vars, fall back to generic AI:ApiKey or legacy OpenAI:ApiKey
        string? aiApiKey = null;
        if (aiProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            aiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        }

        // Also accept OPENAI_API_KEY for backward compatibility
        if (string.IsNullOrWhiteSpace(aiApiKey))
        {
            aiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");    
        }

        if (string.IsNullOrWhiteSpace(aiApiKey))
        {
            aiApiKey = builder.Configuration["AI:ApiKey"] ?? builder.Configuration["OpenAI:ApiKey"];
        }

        if (string.IsNullOrWhiteSpace(aiApiKey))
        {
            throw new InvalidOperationException($"API key for AI provider '{aiProvider}' is not set in environment variables or configuration.");
        }

        // Choose DB provider: prefer Postgres via connection string, but allow a dev-only SQLite fallback.
        var useSqliteEnv = Environment.GetEnvironmentVariable("USE_SQLITE");
        var useSqlite = string.Equals(useSqliteEnv, "true", StringComparison.OrdinalIgnoreCase);

        if (useSqlite || (string.IsNullOrWhiteSpace(connectionString) && builder.Environment.IsDevelopment()))
        {
            // Development fallback to SQLite to allow running the app without Postgres configured.
            builder.Logging.AddConsole();
            var loggerFactory = LoggerFactory.Create(lb => lb.AddConsole());
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("Using SQLite for development (local-dev.db). To use Postgres set SQL_CONNECTION_STRING or ConnectionStrings:DefaultConnection.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite("Data Source=local-dev.db"));
        }
        else
        {
            builder.Services.AddDbContext<ApplicationDbContext>(options
                => options.UseNpgsql(connectionString));
        }
        
        builder.Services.Configure<Jwt>(options =>
        {
            options.Secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["Jwt:Secret"] ?? string.Empty;
            options.Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"] ?? string.Empty;
            options.Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"] ?? string.Empty;
            options.ExpiryInMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? builder.Configuration["Jwt:ExpiryInMinutes"] ?? "60");
        });
        
        builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
        
        builder.Services.AddCors(options => 
        {
            options.AddPolicy(name: corsPolicy,
                policy =>
                {
                    if (allowedOrigins != null && allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                    else
                    {
                        policy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }
                });
        });
        
        var currentAssembly  = typeof(Program).Assembly;
        var referencedAssemblies = currentAssembly .GetReferencedAssemblies()
            .Where(x => x.Name!.StartsWith("CodeExplainer") && x.Name != null); //! Replace "YourApp" with your actual project prefix
        
        var assemblies = referencedAssemblies
            .Select(Assembly.Load)
            .Append(currentAssembly);

        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            if (type.IsClass && !type.IsAbstract)
            {
                foreach (var iface in type.GetInterfaces())
                {
                    if (iface.Name == $"I{type.Name}")
                    {
                        builder.Services.AddScoped(iface, type);
                    }
                }
            }
        }
        
        builder.Services.AddTransient<IEmailSender, EmailSender>();
        builder.Services.AddTransient<CloudinaryUploader>();
        // Add services to the container.

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase; //* Use original property names
                options.JsonSerializerOptions.PropertyNameCaseInsensitive =
                    true; //* Enable case-insensitive property names
            });
        
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "Code Explainer", Version = "v1", Description = "An API endpoint to response code for user" });
            
            //* Add JWT Authentication to Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer"
            });
            
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                    new string[]{}
                }
            });
        });
        
        var authBuilder = builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = "External";
        });

        authBuilder.AddJwtBearer("Bearer", _ => { });

        authBuilder.AddCookie("External", options =>
        {
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = 401; // Unauthorized
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = 403; // Forbidden
                return Task.CompletedTask;
            };
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        });

        // Only register Google authentication if client id/secret are provided to avoid OAuth validation errors
        var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? builder.Configuration["Authentication:Google:ClientId"];
        var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? builder.Configuration["Authentication:Google:ClientSecret"];
        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            authBuilder.AddGoogle(options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.SaveTokens = true;
                options.ClaimActions.MapJsonKey("picture", "picture");
                options.CallbackPath = "/signin-google";
            });
        }
        
        builder.Services.PostConfigure<JwtBearerOptions>("Bearer", options =>
        {
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"] ?? string.Empty;
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"] ?? string.Empty;
            var secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["Jwt:Secret"] ?? string.Empty;

            // If a JWT secret is not provided, avoid constructing a zero-length SymmetricSecurityKey
            // which causes IDX10703. In development scenarios we relax validation but still set a
            // ClockSkew to zero. For production, ensure JWT_SECRET is set to a strong value.
            if (string.IsNullOrWhiteSpace(secret))
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    ClockSkew = TimeSpan.Zero
                };
            }
            else
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ClockSkew = TimeSpan.Zero, //* Disable the default 5-minute clock skew
                    RequireExpirationTime = true //* Require the token to have an expiration time
                };
            }

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Cookies["ACCESS_TOKEN"];
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(context.Exception, "Authentication failed.");
                    return Task.CompletedTask;
                },
                OnTokenValidated = _ =>
                {
                    // You can add additional validation here if needed
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    // Skip the default logic.
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    var result = JsonSerializer.Serialize(new { error = "You are not authorized" });
                    return context.Response.WriteAsync(result);
                }
            };
        });
        
        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();
        
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var logger = sp.GetService<ILogger<Program>>();
            var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString");
                if (string.IsNullOrWhiteSpace(redisConnectionString))
                {
                    logger?.LogWarning("Redis connection string is not set. Redis features will be disabled.");
                    return null!; // intentionally return null to allow the app to start; IDatabase resolution will fail later if used
                }
            }

            var configurationOptions = ConfigurationOptions.Parse(redisConnectionString, true);
            configurationOptions.AbortOnConnectFail = false;

            try
            {
                return ConnectionMultiplexer.Connect(configurationOptions);
            }
            catch (RedisConnectionException ex)
            {
                logger?.LogError(ex, "Failed to connect to Redis. Redis features will be disabled.");
                return null!; // return null to avoid crashing startup
            }
        });
        
        builder.Services.AddScoped<IDatabase>(sp =>
        {
            var connectionMultiplexer = sp.GetService<IConnectionMultiplexer>();
            if (connectionMultiplexer == null)
            {
                var logger = sp.GetService<ILogger<Program>>();
                var stack = Environment.StackTrace;
                logger?.LogError("IDatabase requested but no Redis connection is configured. Set REDIS_CONNECTION_STRING or Redis:ConnectionString. Call stack: {stack}", stack);
                throw new InvalidOperationException("Redis is not configured. Set REDIS_CONNECTION_STRING or Redis:ConnectionString to use Redis features. Call stack: " + stack);
            }

            return connectionMultiplexer.GetDatabase();
        });
        
        MaINBootstrapper.Initialize(null, options =>
        {
            var backend = Environment.GetEnvironmentVariable("AI_BACKEND") ?? builder.Configuration["AI:Provider"] ?? "Gemini";
            options.BackendType = backend switch
            {
                "OpenAI" => BackendType.OpenAi,
                "Claude" => BackendType.DeepSeek,
                _ => BackendType.Gemini
            };

            // Configure provider-specific keys. Use the resolved aiApiKey (which reads GEMINI_API_KEY, OPENAI_API_KEY,
            // or configuration AI:ApiKey / OpenAI:ApiKey) for the chosen backend.
            if (options.BackendType == BackendType.Gemini)
            {
                options.GeminiKey = aiApiKey;
            }
            else if (options.BackendType == BackendType.OpenAi)
            {
                // If MaIN supports an OpenAI key property in options, set it here. Otherwise, some backends may read
                // from env/config internally; providing aiApiKey keeps compatibility.
                options.GeminiKey = aiApiKey; // keep as a fallback for libraries expecting a single key field
            }
        });

        builder.Services.AddSignalR();
        
        var app = builder.Build();

        // Apply pending EF Core migrations at startup only when explicitly enabled.
        // Control with environment variable `APPLY_MIGRATIONS=true` or configuration `ApplyMigrations: true`.
        var applyMigrationsEnv = Environment.GetEnvironmentVariable("APPLY_MIGRATIONS") ?? builder.Configuration["ApplyMigrations"];
        if (string.Equals(applyMigrationsEnv, "true", StringComparison.OrdinalIgnoreCase))
        {
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db.Database.Migrate();
                }
                catch (Exception ex)
                {
                    // Log migration failures but do NOT rethrow so the app can start for local dev scenarios.
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Database migration failed on startup. Skipping migration and continuing.");
                }
            }
        }
        else
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Automatic EF Core migrations are disabled. To enable, set APPLY_MIGRATIONS=true.");
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("EnableSwagger"))
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            // Custom error handling endpoint
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    var logger = app.Services.GetRequiredService<ILogger<Program>>();
                    var exceptionHandlerPathFeature =
                        context.Features.Get<IExceptionHandlerFeature>();

                    if (exceptionHandlerPathFeature?.Error != null)
                    {
                        logger.LogError(exceptionHandlerPathFeature.Error, "An unhandled exception has occurred.");
                    }

                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                        { error = "Internal Server Error" }));
                });
            });
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseRouting();
        app.UseCors(corsPolicy);

        app.UseAuthentication();
        app.UseAuthorization();
        
        app.MapControllers();

        app.MapHub<ChatHub>("/hubs/chat");
        app.MapHub<NotificationHub>("/hubs/notification");

        app.Run();
    }
}