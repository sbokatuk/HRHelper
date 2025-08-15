# HRHelper

ASP.NET Core (.NET 8) Razor Pages app for special requests with expiring links.

## Local development

Requirements:
- .NET 8 SDK
- SQLite (bundled with provider)

Run:
```bash
export ADMIN_PASSWORD=admin
cd src/HRHelper
dotnet restore
dotnet run
```
Open `http://localhost:5205` (or shown URL) → `/admin` (password from `ADMIN_PASSWORD`).

Tests + coverage:
```bash
dotnet test HRHelper.sln -c Release --collect:"XPlat Code Coverage" --results-directory TestResults
```
Artifacts appear under `TestResults/**/coverage.cobertura.xml`.

## Configuration

- `ConnectionStrings:DefaultConnection`: SQLite path (on Cloud Run defaults to `/tmp/hrhelper.db`)
- `Storage.Provider`: `Local` | `AzureBlob` | `Gcs`
  - `Storage:Local:Root` (optional; defaults to temp folder in container)
  - `Storage:Azure:*` or `Storage:Gcs:*`
- Notifications (`Notifications:*`): Email (SMTP) and Telegram
- `ADMIN_PASSWORD`: admin password (env var)
- `APP_VERSION`: shown in footer/admin (injected automatically from tag by CI)

## CI

Workflow: `.github/workflows/ci.yml`
- Build job: builds once (Windows or self‑hosted Windows/Ubuntu per matrix)
- Test job: runs on Ubuntu (free) + self‑hosted Windows/Linux/macOS
- Coverage: Cobertura uploaded as artifact on all; summary posted from Linux only

Self‑hosted runners
- Windows: `scripts/setup-windows-runner.ps1` (installs gcloud, Node.js, Firebase CLI; optional Docker)
- WSL (Ubuntu): `scripts/setup-wsl-github-runner.ps1` (enables systemd, installs tooling, registers runner as systemd service)

## Deployment

### Cloud Run + Firebase Hosting (GitHub Actions)
Workflow: `.github/workflows/firebase-cloudrun.yml`
- Secrets:
  - `GCP_SA_KEY` (service account JSON)
  - `ADMIN_PASSWORD`
- Variables (or keep env defaults in workflow):
  - `GCP_PROJECT_ID` (e.g., `hrhelper-57162`)
  - `CLOUD_RUN_REGION` (e.g., `us-central1`)
- Trigger: tag `v-*` or run manually (OS selectable; default Windows)
- Injected envs to Cloud Run: `ADMIN_PASSWORD`, `APP_VERSION` (tag), notifications (`Notifications__*`), storage (`Storage__*`)

Manual deploy (source) via scripts:
```bash
# Linux/macOS
PROJECT_ID=... REGION=us-central1 ADMIN_PASSWORD=... SA_KEY=/path/sa.json APP_VERSION=v-1.0.0 \
./scripts/deploy-cloudrun-source.sh

# Windows PowerShell
.\n+scripts\deploy-cloudrun.ps1 -ProjectId "..." -Region "us-central1" -AdminPassword "..." -ServiceAccountKeyPath "C:\\keys\\sa.json" -AppVersion "v-1.0.0"
```

### Azure Web App (Container)
Workflow: `.github/workflows/azure-webapp-container.yml`
- Secrets: `AZURE_WEBAPP_PUBLISH_PROFILE`, optionally `ACR_USERNAME`/`ACR_PASSWORD`
- Variables: `AZURE_WEBAPP_NAME` (default `hrhelper-app`), optionally `ACR_LOGIN_SERVER`
- Runner: default Windows; requires Docker Desktop on Windows self‑hosted (or set OS to Ubuntu)
- Trigger: tag `v-azure-*` or manual run
- Injected envs: same set as Cloud Run (`ADMIN_PASSWORD`, `APP_VERSION`, notifications, storage)

## Notes

- Version footer: shows `APP_VERSION` (from tag) and `© <year> bokatuk.com` on all pages; admin pages also show the version.
- Storage defaults to local for development; use Azure Blob or GCS in production.
- `.gitignore` excludes `keys/`, typical secrets, `TestResults/`, and build artifacts.
