using Microsoft.EntityFrameworkCore;
using Task_Manager.Data;

var builder = WebApplication.CreateBuilder(args);

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

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
    var db = scope.ServiceProvider.GetRequiredService<Task_Manager_DbContext>();
    db.Database.Migrate();
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
