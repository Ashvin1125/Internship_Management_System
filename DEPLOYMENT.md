# Internship Management System — Deployment Guide

> Deploy to **Render**, **Railway**, or **Azure App Service** using the steps below.

---

## Quick Reference

| Item | Value |
|---|---|
| Runtime | .NET 8 |
| Database (Production) | SQLite (`ims.db`) |
| Database (Development) | SQL Server |
| Default Port | `$PORT` env var (fallback: `5000`) |

---

## Build & Publish

```bash
dotnet publish -c Release -o out
```

Output will be in the `out/` directory.

---

## Start Command

```bash
dotnet out/InternshipManagementSystem.dll
```

---

## Required Environment Variables

| Variable | Description | Required |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Must be `Production` for SQLite + production config | ✅ Yes |
| `PORT` | Port to bind (auto-set by Render/Railway) | Set by platform |

---

## Platform-Specific Instructions

### 🚀 Render

1. Create a new **Web Service** on [render.com](https://render.com).
2. Connect your GitHub repository.
3. Set the following in Render's dashboard:
   - **Build Command:** `dotnet publish -c Release -o out`
   - **Start Command:** `dotnet out/InternshipManagementSystem.dll`
   - **Environment:** `ASPNETCORE_ENVIRONMENT=Production`
4. Render automatically injects the `PORT` environment variable. ✅
5. The app will auto-migrate the SQLite database on first boot.

### 🚂 Railway

1. Create a new project on [railway.app](https://railway.app).
2. Connect your GitHub repository.
3. Railway auto-detects `.NET`. Add these settings under **Variables**:
   - `ASPNETCORE_ENVIRONMENT=Production`
4. Set the **Start Command** to:
   ```bash
   dotnet out/InternshipManagementSystem.dll
   ```
5. Railway injects `PORT` automatically. ✅

### ☁️ Azure App Service

1. Publish directly via Azure CLI:
   ```bash
   dotnet publish -c Release -o out
   az webapp deploy --resource-group <rg> --name <app-name> --src-path out/
   ```
2. In **Configuration > Application Settings**, add:
   - `ASPNETCORE_ENVIRONMENT` = `Production`
3. Azure manages HTTPS and port binding automatically. ✅

---

## How It Works at Startup

When `ASPNETCORE_ENVIRONMENT=Production`, the app will:

1. **Bind** to `http://0.0.0.0:$PORT`
2. **Use SQLite** (`Data Source=ims.db`) instead of SQL Server
3. **Auto-create & migrate** the database via `context.Database.Migrate()`
4. **Seed initial data** (Admin user, etc.) via `DbSeeder.Initialize()`
5. **Create** `wwwroot/uploads/` directory if missing
6. **Trust forwarded headers** from the cloud proxy (X-Forwarded-For, X-Forwarded-Proto)

---

## Health Check

Once deployed, verify the app is running:

```
GET /api/health
```

Expected response:
```json
{
  "status": "Healthy",
  "timestamp": "2025-01-01T00:00:00Z"
}
```

---

## Important Notes

> [!WARNING]
> SQLite stores `ims.db` **on the container's local filesystem**. On platforms like Render (free tier), the filesystem is **ephemeral** — the database resets on redeploy. For persistent data, consider using a managed database (PostgreSQL via Render, PlanetScale, etc.) and update the connection string accordingly.

> [!NOTE]
> The `out/` directory and `ims.db` are excluded from Git by default. Never commit the database file to source control.

> [!TIP]
> For production, set a strong `ASPNETCORE_DataProtection__*` key or use Azure Key Vault / platform secret management to protect cookie encryption keys.

---

## .gitignore Recommendations

Add these to your `.gitignore`:

```
out/
ims.db
*.db-shm
*.db-wal
wwwroot/uploads/
```
