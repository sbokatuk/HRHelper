# HRHelper

ASP.NET Core (.NET 8) Razor Pages app for special requests with expiring links.

## Run locally

```bash
export ADMIN_PASSWORD=admin
cd src/HRHelper
dotnet restore
dotnet run
```

Open http://localhost:5205 (or the shown URL), go to /admin.

## Configuration

- `ConnectionStrings:DefaultConnection` — SQLite path (Cloud Run uses /tmp/hrhelper.db automatically)
- `Storage.Provider` — Local | AzureBlob | Gcs
  - `Storage:Local:Root` (optional; defaults to temp folder in container)
  - `Storage:Azure:*` or `Storage:Gcs:*`
- Notifications (`Notifications:*`) — Email (SMTP) and Telegram
- Admin password from env var `ADMIN_PASSWORD`

## Deploy

### Cloud Run + Firebase Hosting (GitHub Actions)
- Add repo secrets:
  - `GCP_SA_KEY`: JSON of a service account in your project
  - `ADMIN_PASSWORD`: admin password
- Update `.github/workflows/firebase-cloudrun.yml` env `GCP_PROJECT_ID` and region.
- Trigger: create a tag `deploy-<anything>` or run workflow manually.

### Azure Web App (Container)
- Add repo secret `AZURE_WEBAPP_PUBLISH_PROFILE` (from Azure portal)
- Optionally configure ACR (`ACR_USERNAME`, `ACR_PASSWORD`, `ACR_LOGIN_SERVER` in env)
- Trigger: tag `deploy-azure-<anything>` or run manually.

## Security
- Never commit keys or JSON creds. `.gitignore` excludes typical secrets/keys.
- Prefer using env vars in CI/CD and platform secrets managers.
