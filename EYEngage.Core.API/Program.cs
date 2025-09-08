// Program.cs - Configuration avec optimisations pour éviter les timeouts SQL
using EYEngage.Core.API.Middleware;
using EYEngage.Core.Application.InterfacesServices;
using EYEngage.Core.Application.Services;
using EYEngage.Core.Domain;
using EYEngage.Infrastructure;
using Google.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1) CORS - Configuration mise à jour pour supporter les cookies
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "https://localhost:3000"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .SetIsOriginAllowed(origin => true);
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNestJS", builder =>
    {
        builder
            .WithOrigins("http://localhost:3001") // URL de NestJS
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});


// 2) Base de données avec optimisations pour éviter les timeouts
builder.Services.AddDbContext<EYEngageDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("EYEngageDatabase"),
        sqlServerOptionsAction: sqlOptions =>
        {
            // Configuration pour éviter les timeouts
            sqlOptions.CommandTimeout(60); // Timeout de 60 secondes pour les commandes

            // Activer les retry en cas d'erreur transitoire
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);

            // Optimisations supplémentaires
            sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        })
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()) // Pour debug en dev
        .EnableDetailedErrors(builder.Environment.IsDevelopment()); // Erreurs détaillées en dev

    // Configuration du pool de connexions
    options.ConfigureWarnings(warnings =>
    {
        // Ignorer certains warnings en production
        if (!builder.Environment.IsDevelopment())
        {
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.SensitiveDataLoggingEnabledWarning);
        }
    });
}, ServiceLifetime.Scoped); // Explicitement Scoped pour éviter les problèmes de lifetime

// Configuration du pool de connexions SQL
builder.Services.Configure<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptions>(options =>
{
    // Configuration additionnelle si nécessaire
});
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters.RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
});

// 3) Identity Configuration avec optimisations
builder.Services.AddIdentityCore<User>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddRoles<Role>()
.AddEntityFrameworkStores<EYEngageDbContext>()
.AddDefaultTokenProviders();

// 4) JWT Authentication - Configuration améliorée
var jwtSecret = builder.Configuration["JwtSettings:Secret"];
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
var jwtAudience = builder.Configuration["JwtSettings:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret!)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["ey-session"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            if (context.Exception?.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Add("Token-Expired", "true");
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            if (!context.Response.HasStarted)
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                var response = JsonSerializer.Serialize(new
                {
                    error = "Unauthorized",
                    message = "Authentication required"
                });

                return context.Response.WriteAsync(response);
            }
            return Task.CompletedTask;
        }
    };
});

// 5) Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ActiveUser", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context =>
                context.User.IsInRole("SuperAdmin") ||
                context.User.HasClaim(c => c.Type == "IsActive" && c.Value == "True")
              )
    );

    options.AddPolicy("SuperAdmin", policy =>
        policy.RequireRole("SuperAdmin")
    );

    options.AddPolicy("AdminOrAgent", policy =>
        policy.RequireRole("SuperAdmin", "Admin", "AgentEY")
    );

    options.AddPolicy("AllEmployees", policy =>
        policy.RequireRole("SuperAdmin", "Admin", "AgentEY", "EmployeeEY")
    );
});

// 6) Services avec optimisations
builder.Services.AddHttpContextAccessor();

// Services principaux
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
// Services supplémentaires
builder.Services.AddHttpClient();
builder.Services.AddScoped<GeminiService>();


// Configuration du logging pour diagnostiquer les problèmes
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
}

// 7) Controllers avec configuration JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 8) Documentation API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EY Engage API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});
// Configuration Kestrel pour éviter les timeouts
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
});

var app = builder.Build();

// Configuration du pipeline de middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseCors("AllowNestJS");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SessionValidationMiddleware>();
app.MapControllers();
app.MapGet("/health", () => "OK").AllowAnonymous();

// Initialisation de la base de données avec gestion des erreurs améliorée
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Starting database migration and seed...");

        var context = services.GetRequiredService<EYEngageDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<Role>>();

        // Vérifier la connexion à la base de données
        var canConnect = await context.Database.CanConnectAsync();
        if (!canConnect)
        {
            logger.LogError("Cannot connect to database. Please check connection string and SQL Server status.");
            throw new Exception("Database connection failed");
        }

        // Utiliser la stratégie d'exécution pour gérer les retry avec transactions
        var executionStrategy = context.Database.CreateExecutionStrategy();

        await executionStrategy.ExecuteAsync(async () =>
        {
            // Transaction gérée par la stratégie d'exécution
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                // Migrer la base de données
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migration completed successfully");

                // Créer les rôles
                string[] roleNames = { "SuperAdmin", "Admin", "AgentEY", "EmployeeEY" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new Role { Name = roleName });
                        logger.LogInformation($"Role {roleName} created");
                    }
                }

                // Créer le SuperAdmin
                var superAdminEmail = "superadmin@ey.com";
                var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);

                if (superAdmin == null)
                {
                    superAdmin = new User
                    {
                        UserName = superAdminEmail,
                        Email = superAdminEmail,
                        FullName = "Super Admin",
                        Department = Department.Consulting,
                        Fonction = "System Administrator", // AJOUT: Valeur pour Fonction
                        Sector = "Technology", // AJOUT: Valeur pour Sector si requis
                        PhoneNumber = "+216 00 000 000", // AJOUT: Numéro de téléphone si requis
                        IsActive = true,
                        IsFirstLogin = false,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow, // AJOUT: Date de création
                        UpdatedAt = DateTime.UtcNow  // AJOUT: Date de mise à jour
                    };

                    var result = await userManager.CreateAsync(superAdmin, "Admin@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                        logger.LogInformation("SuperAdmin user created successfully");
                    }
                    else
                    {
                        logger.LogError($"Failed to create SuperAdmin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }

                await transaction.CommitAsync();
                logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Error during database initialization, transaction rolled back");
                throw;
            }
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Fatal error during database initialization");
        // Ne pas faire planter l'application en production
        if (!app.Environment.IsDevelopment())
        {
            logger.LogWarning("Application continuing despite database initialization failure");
        }
        else
        {
            throw; // En développement, on veut voir l'erreur
        }
    }
}

app.Run();