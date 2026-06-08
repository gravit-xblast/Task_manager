# Guide de migration Entity Framework — Docker + WSL2

## Vue d'ensemble du processus

```
Code C# (entités + DbContext)
        ↓
  dotnet ef migrations add   ← génère les fichiers de migration
        ↓
  docker-compose up          ← lance SQL Server + API
        ↓
  db.Database.Migrate()      ← applique la migration au démarrage (Program.cs)
        ↓
  Base SQL Server créée et à jour
```

---
## Étape 1 — Packages NuGet à installer

##### Entity Framework Core pour SQL Server :
```
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```
##### Outils CLI de migration (nécessaire pour "dotnet ef migrations add") :
```
dotnet add package Microsoft.EntityFrameworkCore.Tools
```
##### Design-time (nécessaire au build pour les migrations) :
```
dotnet add package Microsoft.EntityFrameworkCore.Design
```


---
## Étape 2 — Fichiers de configuration  

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=TaskManagerDB;User Id=sa;Password=YourStrong@Password1;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "AllowedHosts": "*"
}
```


## Étape 3 — Dockerfile

### WebAPI

### SQL Server

---

## Étape 4 — docker-compose.yml

---


## Étape 5 — Générer la migration initiale

#### Générer les fichiers de migration :
```bash
dotnet ef migrations add InitialCreate
```

> **La migration ne touche PAS encore la base.** Elle génère uniquement
> des fichiers C# décrivant les opérations SQL à effectuer.

---

## Étape 6 — Lancer Docker Compose

#### Depuis le dossier contenant docker-compose.yml :
```bash
docker compose up --build
```
#### Pour lancer en arrière-plan :
```bash
docker compose up --build -d
```

####  Voir les logs en temps réel :
```bash
docker compose logs -f api
```
#### Arrêt et suppression des conteneurs, réseaux et volumes du projet (***données***), ainsi que ceux non déclarés dans le compose```(ex: changement dans le docker-compose)``` :
```bash
docker compose down -v --remove-orphans
```
---


## Étape 7 — Configuration de redirection de port (port forwarding) sur Windows pour exposer un service tournant dans WSL2/Docker vers localhost.
#### Récupère l'IP de WSL2 depuis Linux :
```bash
ip addr show eth0 | grep "inet " | awk '{print $2}' | cut -d/ -f1 
```

#### Depuis invite de commande (CMD) en administrateur , Lancer la commande suivante :
```cmd
netsh interface portproxy add v4tov4 listenport=8080 listenaddress=0.0.0.0 connectport=8080 connectaddress=172.23.42.234
```
#### Vérification que la commande est bien enregistrée :
```cmd
netsh interface portproxy show all
```

#### Pour supprimer la règle, dans une invite de commande (CMD) en administrateur :
```cmd
netsh interface portproxy delete v4tov4 listenport=8080 listenaddress=0.0.0.0
```
#### Commande pour configurer un **port forwarding** sur Windows :
```cmd
netsh interface portproxy add v4tov4 listenport=8080 listenaddress=0.0.0.0 connectport=8080 connectaddress=172.23.42.234
```
---


## Étape 8 — Vérifier la migration

#### Voir les migrations appliquées en base
```bash
dotnet ef migrations list
```
#### Optionnel : voir le SQL qui sera (ou a été) exécuté :
```bash
dotnet ef migrations script
```
---


## Étape 9 — Workflow pour les migrations suivantes ***```(à chaque modification des entités)```*** :

##### 1. Modifier les entités ou le DbContext

##### 2. Générer une nouvelle migration (nommée explicitement)
```bash
dotnet ef migrations add NomDeLaMigration
```
##### 3. Mise à jour de la base de données
```bash
dotnet ef database update
```
##### 4. Rebuilder et redémarrer le pipeline (Docker Compose)
```bash
docker compose up --build
```
---

---

## Fonctionnement
**Ce qui se passe au démarrage :**
1. Docker lance `sqlserver` en premier
2. Le healthcheck attend que SQL Server réponde (`SELECT 1`)
3. Docker lance `api` seulement quand SQL Server est healthy
4. `Program.cs` exécute `db.Database.Migrate()` :
   - Crée la base `TaskManagerDB` si elle n'existe pas
   - Applique `InitialCreate` si pas encore appliquée
5. L'API est prête sur http://localhost:8080





