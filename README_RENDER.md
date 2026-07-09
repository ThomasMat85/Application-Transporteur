# TransCars API - Deploiement Render

Ce dossier contient uniquement le serveur API a mettre sur Render.

## A mettre dans GitHub

Mets tout le contenu de ce dossier dans ton depot GitHub :

- `Controllers`
- `DTOs`
- `Data`
- `Data Serv`
- `Migrations`
- `Modele`
- `Models`
- `Properties`
- `Services`
- `Application Camion API.csproj`
- `Program.cs`
- `appsettings.json`
- `Dockerfile`
- `.dockerignore`
- `.gitignore`

Ne mets pas :

- `bin`
- `obj`
- `.vs`
- fichiers `.user`

## Creation sur Render

1. Va sur Render.
2. Clique `New Web Service`.
3. Connecte ton depot GitHub.
4. Choisis le depot qui contient cette API.
5. Environment : `Docker`.
6. Instance type : `Free`.
7. Ajoute la variable d'environnement :

```text
DATABASE_URL=postgresql://...
```

Utilise la connection string Neon, avec `sslmode=require`.

## Apres le deploiement

Render donnera une URL du type :

```text
https://transcars-api.onrender.com
```

Cette URL devra ensuite etre mise dans l'application Android.
