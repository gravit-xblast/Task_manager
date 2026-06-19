using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Task_Manager.Authentication;
using Task_Manager.Data;
using Task_Manager.Models;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// -------------------------
// Configuration JWT
// -------------------------
var secretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey n'est pas défini dans la configuration");


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
    // Ces politiques sont définies ici de manière centralisée.
    // Dans les controllers, utiliser [Authorize(Policy = "AdminOnly")]
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "SuperAdmin"));
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("Standard", "Admin", "SuperAdmin"));
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

    // CORRIGÉ : SecuritySchemeType.Http + Scheme "bearer" est la définition
    // correcte pour un schéma Bearer JWT selon la spec OpenAPI 3.0.
    // Avec ApiKey, Swagger UI demande de saisir "Bearer <token>" manuellement.
    // Avec Http + bearer, Swagger UI préfixe automatiquement "Bearer ".
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Entrez votre JWT (sans le préfixe 'Bearer ')",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // NOTE : AddSecurityRequirement ici s'applique globalement à tous les
    // endpoints dans Swagger UI (y compris /register et /token qui sont
    // [AllowAnonymous]). Ce n'est pas un bug de sécurité (le middleware
    // d'authentification respecte [AllowAnonymous] au runtime), mais
    // l'interface Swagger affiche un cadenas sur ces endpoints publics,
    // ce qui peut prêter à confusion.
    //
    // Pour être rigoureux, remplacer ce bloc par un OperationFilter
    // (ex: SecurityRequirementsOperationFilter de Swashbuckle.AspNetCore.Filters)
    // qui n'ajoute le cadenas que sur les endpoints [Authorize].
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
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
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            );
        }
    )
);


var app = builder.Build();

// -------------------------
// Migration automatique au démarrage
// -------------------------
using (var scope = app.Services.CreateScope())
{
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

    try
    {
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var superAdminEmail = builder.Configuration["SuperAdmin:Email"]
            ?? throw new InvalidOperationException("SuperAdmin:Email manquant");

        var existing = await userService.GetUserByEmail(superAdminEmail);
        if (existing is null)
        {
            await userService.RegisterUser(new RegisterRequest
            {
                UserName = builder.Configuration["SuperAdmin:UserName"] ?? "superadmin",
                Email = superAdminEmail,
                Password = builder.Configuration["SuperAdmin:Password"]
                    ?? throw new InvalidOperationException("SuperAdmin:Password manquant")
            }, UserStatus.SuperAdmin);

            app.Logger.LogInformation("Compte SuperAdmin créé.");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Erreur lors du seed du compte SuperAdmin.");
        throw;
    }
}

// -------------------------
// Pipeline HTTP
// ORDRE CRITIQUE : Authentication → Authorization → MapControllers
// Inverser les deux premiers rend [Authorize] inefficace.
// -------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();




