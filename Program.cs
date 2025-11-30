using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Proyecto1.Hubs;
using Proyecto1.Infrastructure.Data;
using Proyecto1.Infrastructure.Repositories;
using Proyecto1.Services;
using System.Text;
using Proyecto1.Infrastructure.Repositories.Interfaces;
using Proyecto1.Services.Interfaces;

// 🔥 NUEVOS USING PARA SEED
using System.Security.Cryptography;
using Proyecto1.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Snakes and Ladders API", 
        Version = "v1",
        Description = "API para el juego multijugador Serpientes y Escaleras"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
            Array.Empty<string>()
        }
    });
});

// Database configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null)
    ));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };

        // Permitir token por query string para SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/game"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400; // 100 KB
});

// CORS 
builder.Services.AddCors(options =>
{
    // Política general para la API REST
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });

    // Política del Hub SignalR - permite TODOS los orígenes (desarrollo)
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)  // permite cualquier origen
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Repositories
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();

// Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IDiceService, DiceService>();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<ITurnService, TurnService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IGameService, GameService>();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Snakes and Ladders API v1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();

// IMPORTANTE: El orden importa
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SignalR Hub
app.MapHub<GameHub>("/hubs/game").RequireCors("SignalRPolicy");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
}));

// Database initialization (optional) + SEED ADMIN & SKINS
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        if (context.Database.CanConnect())
        {
            Console.WriteLine("✅ Database connection successful!");
            
            if (app.Environment.IsDevelopment())
            {
                var pendingMigrations = context.Database.GetPendingMigrations();
                if (pendingMigrations.Any())
                {
                    Console.WriteLine($"⚠ Applying {pendingMigrations.Count()} pending migrations...");
                    context.Database.Migrate();
                    Console.WriteLine("✅ Migrations applied successfully!");
                }
            }

            // 🔥 SEED ADMIN + SKINS BÁSICAS
            SeedAdminAndSkins(context);
        }
        else
        {
            Console.WriteLine("❌ Cannot connect to database. Check connection string.");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ An error occurred while connecting to the database.");
        Console.WriteLine($"❌ Database error: {ex.Message}");
    }
}

Console.WriteLine("🚀 Snakes and Ladders API is running!");
Console.WriteLine($"📍 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"🌐 Swagger UI: https://localhost:{builder.Configuration["Kestrel:Endpoints:Https:Port"] ?? "5001"}/swagger");
Console.WriteLine($"🎮 SignalR Hub: wss://localhost:{builder.Configuration["Kestrel:Endpoints:Https:Port"] ?? "5001"}/hubs/game");

app.Run();

// ===================== SEED ADMIN + SKINS =====================
static void SeedAdminAndSkins(AppDbContext context)
{
    // 1) Usuario admin
    var admin = context.Users.FirstOrDefault(u => u.Username == "admin");
    if (admin == null)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes("admindemo"));
        var passwordHash = Convert.ToBase64String(hashedBytes);

        admin = new User
        {
            Username = "admin",
            Email = "admin@demo.com",
            PasswordHash = passwordHash,
            Coins = 9999
        };

        context.Users.Add(admin);
        Console.WriteLine("🧑‍💻 Admin creado: admin / admindemo");
    }

    // 2) Skins básicas
    if (!context.TokenSkins.Any())
    {
        context.TokenSkins.AddRange(
            new TokenSkin
            {
                Name = "Clásico Verde",
                ColorKey = "green",
                IconKey = "classic",
                PriceCoins = 0,
                IsActive = true
            },
            new TokenSkin
            {
                Name = "Clásico Azul",
                ColorKey = "blue",
                IconKey = "classic",
                PriceCoins = 0,
                IsActive = true
            },
            new TokenSkin
            {
                Name = "Nerd 🤓",
                ColorKey = "purple",
                IconKey = "nerd",
                PriceCoins = 10,
                IsActive = true
            },
            new TokenSkin
            {
                Name = "Enojado 😡",
                ColorKey = "red",
                IconKey = "angry",
                PriceCoins = 15,
                IsActive = true
            }
        );

        Console.WriteLine("🎨 Skins básicas creadas");
    }

    context.SaveChanges();
}