# Docker Deployment Plan — ANDON App on Windows Server 2022

## Overview

This document covers containerising the ANDON Blazor Server app and running it on
Windows Server 2022 using Docker (Linux containers). SQL Server runs as a companion
container. The workflow is: **clone or pull from GitHub → `docker compose up`**.

---

## Architecture

```
Windows Server 2022
└── Docker Engine (Linux containers via Hyper-V / WSL2)
    ├── andon-app   (ASP.NET Core 8, port 8080 → host 80/443)
    └── andon-db    (SQL Server 2022, port 1433 — internal only)
```

Config files (`email-settings.json`, `general-settings.json`) are bind-mounted
from the host so settings survive container rebuilds without touching the image.

---

## Step 1 — Prerequisites on Windows Server 2022

### 1.1 Enable required Windows features

Run in an elevated PowerShell:

```powershell
# Enable Hyper-V and Containers feature
Install-WindowsFeature -Name Hyper-V, Containers -IncludeManagementTools -Restart
```

### 1.2 Install Docker Engine

Docker Desktop is not required — use Docker Engine (Community Edition):

```powershell
# Install Docker Engine via the official script
Invoke-WebRequest -UseBasicParsing https://get.docker.com/windows/static/stable/x86_64/docker-26.1.3.zip -OutFile docker.zip
# OR use the recommended winget / chocolatey route:
winget install Docker.DockerDesktop   # if Desktop is acceptable
# OR install Docker Engine directly:
# https://docs.docker.com/engine/install/windows-server/
```

For a headless server install (no GUI), use **Mirantis Container Runtime** or the
[Docker Engine MSI](https://docs.docker.com/engine/install/windows-server/).

### 1.3 Install Docker Compose plugin

```powershell
# Compose V2 ships with Docker Desktop; for Engine-only installs:
mkdir "$Env:ProgramFiles\Docker\cli-plugins" -Force
Invoke-WebRequest -UseBasicParsing `
  "https://github.com/docker/compose/releases/latest/download/docker-compose-windows-x86_64.exe" `
  -OutFile "$Env:ProgramFiles\Docker\cli-plugins\docker-compose.exe"
```

Verify:
```powershell
docker version
docker compose version
```

### 1.4 Switch Docker to Linux containers

Docker Desktop: right-click system tray → "Switch to Linux containers".
Docker Engine on Server 2022: Linux containers are the default when using WSL2 backend.

### 1.5 Install Git

```powershell
winget install Git.Git
```

---

## Step 2 — Files to add to the repository

### 2.1 `AndonApp/Dockerfile`

Place this file alongside `AndonApp.sln`:

```dockerfile
# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AndonApp/AndonApp.csproj AndonApp/
RUN dotnet restore AndonApp/AndonApp.csproj

COPY . .
RUN dotnet publish AndonApp/AndonApp.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN addgroup --system andon && adduser --system --ingroup andon andon
USER andon

COPY --from=build /app/publish .

# Config files are bind-mounted at runtime (see docker-compose.yml)
# so these COPY lines provide defaults only:
COPY AndonApp/AndonApp/appsettings.json ./appsettings.json

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "AndonApp.dll"]
```

> The `Dockerfile` context root is the repo root so the build stage can reach
> both the solution file and the project folder.

---

### 2.2 `docker-compose.yml` (repo root)

```yaml
services:

  andon-db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: andon-db
    restart: unless-stopped
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${DB_SA_PASSWORD}"   # set in .env
    volumes:
      - andon-db-data:/var/opt/mssql
    networks:
      - andon-net
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$$MSSQL_SA_PASSWORD" -Q "SELECT 1" -No
      interval: 10s
      timeout: 5s
      retries: 10

  andon-app:
    build:
      context: .
      dockerfile: AndonApp/Dockerfile
    container_name: andon-app
    restart: unless-stopped
    depends_on:
      andon-db:
        condition: service_healthy
    ports:
      - "80:8080"
    environment:
      ConnectionStrings__DefaultConnection: >-
        Server=andon-db,1433;Database=AndonDb;
        User Id=sa;Password=${DB_SA_PASSWORD};
        TrustServerCertificate=True;
        MultipleActiveResultSets=true
      ASPNETCORE_ENVIRONMENT: Production
      EMAIL_MODE: "${EMAIL_MODE:-LogOnly}"
      SMTP_HOST: "${SMTP_HOST:-}"
      SMTP_PORT: "${SMTP_PORT:-587}"
      SMTP_USER: "${SMTP_USER:-}"
      SMTP_PASS: "${SMTP_PASS:-}"
      EMAIL_FROM: "${EMAIL_FROM:-andon@example.com}"
    volumes:
      # Bind-mount config files so they survive image rebuilds
      - ./config/email-settings.json:/app/email-settings.json:ro
      - ./config/general-settings.json:/app/general-settings.json:ro
    networks:
      - andon-net

volumes:
  andon-db-data:

networks:
  andon-net:
```

---

### 2.3 `.env.example` (repo root — commit this, NOT `.env`)

```dotenv
# Copy to .env and fill in values before first run
DB_SA_PASSWORD=Change_Me_Strong_Password1!

EMAIL_MODE=LogOnly      # LogOnly | Smtp
SMTP_HOST=
SMTP_PORT=587
SMTP_USER=
SMTP_PASS=
EMAIL_FROM=andon@example.com
```

---

### 2.4 `config/` directory (repo root — commit example files)

Create two example files that operators copy and edit on the server:

**`config/email-settings.example.json`** → operator copies to `config/email-settings.json`

```json
{
  "EMAIL_MODE": "LogOnly",
  "SMTP_HOST": "",
  "SMTP_PORT": "587",
  "SMTP_USER": "",
  "SMTP_PASS": "",
  "EMAIL_FROM": "andon@example.com"
}
```

**`config/general-settings.example.json`** → operator copies to `config/general-settings.json`

```json
{
  "GeneralSettings": {
    "CompanyName": "Your Company Name",
    "AdherenceGreenThreshold": 100,
    "AdherenceAmberThreshold": 85
  }
}
```

Add a `.gitignore` rule so the live config files are never committed:

```gitignore
# In repo root .gitignore
.env
config/email-settings.json
config/general-settings.json
```

---

### 2.5 EF Core migration on startup

The app must run `database update` automatically when the container starts.
Add this near the top of `Program.cs` (after `builder.Build()`):

```csharp
// Auto-migrate on startup — safe to run on every boot
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AndonDbContext>();
    db.Database.Migrate();
}
```

This means deploying a new release automatically applies any new migrations.

---

## Step 3 — First-time server setup

```powershell
# 1. Clone the repo
git clone https://github.com/<your-org>/ANDON.git C:\andon
cd C:\andon

# 2. Create live config files from examples
Copy-Item config\email-settings.example.json  config\email-settings.json
Copy-Item config\general-settings.example.json config\general-settings.json
# Edit both files with real values using Notepad or VS Code

# 3. Create the .env file
Copy-Item .env.example .env
# Edit .env — at minimum set DB_SA_PASSWORD to something strong

# 4. Build and start
docker compose up -d --build

# 5. Watch logs to confirm healthy startup
docker compose logs -f andon-app
```

The app will be available at `http://<server-ip>/`.

Default admin login (from seed data):
- URL: `http://<server-ip>/admin/login`
- Username: `admin`
- Password: see README seed data section

---

## Step 4 — Updating to a new release

```powershell
cd C:\andon

# Pull latest code
git pull origin main   # or: git fetch && git checkout v1.2.3

# Rebuild and restart the app container only
# (the DB container and its data volume are untouched)
docker compose up -d --build andon-app

# Confirm
docker compose logs -f andon-app
```

EF Core migrations run automatically on startup, so the schema is updated
without any manual `dotnet ef` commands on the server.

---

## Step 5 — HTTPS (recommended for production)

### Option A — Reverse proxy with IIS (simplest on Windows Server)

1. Install IIS + **URL Rewrite** + **Application Request Routing** modules.
2. Install a TLS certificate (Let's Encrypt via win-acme, or your corporate cert).
3. Create an IIS site that reverse-proxies to `http://localhost:80`.

IIS config snippet (`web.config` in the IIS site root):
```xml
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="ReverseProxy" stopProcessing="true">
          <match url="(.*)" />
          <action type="Rewrite" url="http://localhost:80/{R:1}" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
```

### Option B — Terminate TLS inside the container

Mount a PFX certificate and set:
```yaml
environment:
  ASPNETCORE_URLS: "https://+:8443;http://+:8080"
  ASPNETCORE_Kestrel__Certificates__Default__Path: /certs/andon.pfx
  ASPNETCORE_Kestrel__Certificates__Default__Password: "${CERT_PASSWORD}"
volumes:
  - ./certs/andon.pfx:/certs/andon.pfx:ro
ports:
  - "443:8443"
  - "80:8080"
```

---

## Step 6 — Useful operational commands

```powershell
# View running containers
docker compose ps

# View app logs (live)
docker compose logs -f andon-app

# View DB logs
docker compose logs -f andon-db

# Stop everything (data volume preserved)
docker compose down

# Stop AND wipe the database (destructive!)
docker compose down -v

# Open a shell in the app container (debugging)
docker compose exec andon-app sh

# Connect to SQL Server from the DB container
docker compose exec andon-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<password>" -No
```

---

## Summary Checklist

- [ ] Windows Server 2022 — Hyper-V + Containers features enabled
- [ ] Docker Engine (or Docker Desktop) installed and running Linux containers
- [ ] Git installed
- [ ] `Dockerfile` added to repo
- [ ] `docker-compose.yml` added to repo
- [ ] `.env.example` added to repo; `.env` added to `.gitignore`
- [ ] `config/` example files added; live files added to `.gitignore`
- [ ] `db.Database.Migrate()` call in `Program.cs`
- [ ] Repo cloned to server, `.env` and `config/` files created and filled in
- [ ] `docker compose up -d --build` succeeds
- [ ] App accessible in browser
- [ ] HTTPS configured (IIS reverse proxy or Kestrel PFX)
