using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Task_Manager.Authentication;
using Task_Manager.Data;


var builder = WebApplication.CreateBuilder(args);

// -------------------------
// Configuration JWT
// -------------------------
var secretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey n'est pas défini dans la configuration");

// temp - debug
Console.WriteLine(
    $"Secret = '{builder.Configuration["Jwt:SecretKey"]}'"
);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero   // Pas de tolérance sur l'expiration
    };
});


// -------------------------
// Autorisation basée sur les rôles
// -------------------------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
});


// -------------------------
// Services
// -------------------------
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Task_manager API", Version = "v1" });

    // Ajout du support Bearer Token dans Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Format: Bearer {token}",
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
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// DbContext
builder.Services.AddDbContext<Task_Manager_DbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            // Retry automatique si SQL Server n'est pas encore prêt
            // (cas fréquent au démarrage Docker : le container SQL
            // met ~15-30s à être pleinement disponible).
            // EnableRetryOnFailure retente jusqu'à 5 fois avec
            // un délai croissant entre chaque tentative.
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            );
        }
    )
);



var app = builder.Build();

// ==============================================================
// SECTION 3 — MIGRATION AUTOMATIQUE AU DÉMARRAGE
//
// IMPORTANT pour Docker : on ne peut pas lancer "dotnet ef
// database update" manuellement à chaque déploiement.
// Ce bloc applique automatiquement les migrations en attente
// à chaque démarrage du container.
//
// Flux :
//   - Récupère une instance du DbContext depuis le DI container
//   - Appelle database.Migrate() qui :
//       a) Crée la base si elle n'existe pas
//       b) Applique toutes les migrations non encore appliquées
//       c) Ne touche à rien si tout est à jour
//
// Le retry d'EnableRetryOnFailure (configuré sur UseSqlServer
// ci-dessus) gère les tentatives si SQL Server n'est pas encore
// prêt. Si toutes les tentatives échouent, l'app lève une
// exception au démarrage — comportement souhaitable (le container
// s'arrête et Docker Compose peut le redémarrer via restart: on-failure).
// ==============================================================
using (var scope = app.Services.CreateScope())
{
    //var db = scope.ServiceProvider.GetRequiredService<Task_Manager_DbContext>();
    //db.Database.Migrate();

    try
    {
        var db = scope.ServiceProvider.GetRequiredService<Task_Manager_DbContext>();

        app.Logger.LogInformation("Application des migrations...");
        db.Database.Migrate();
        app.Logger.LogInformation("Migrations appliquées avec succès.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Erreur lors de l'application des migrations.");
        throw;
    }
}

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication(); // si auth utilisée
app.UseAuthorization();
app.MapControllers();
app.Run();





