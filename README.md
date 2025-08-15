# HRHelper

ASP.NET Core (.NET 8) Razor Pages app for special requests with expiring links.

## Run locally

```bash
export ADMIN_PASSWORD=admin
cd src/HRHelper
dotnet restore
dotnet ef database update || dotnet run # initial migration is automatic via EnsureCreated/Migrate
dotnet run
```

Open http://localhost:5000 (or the shown URL), go to /admin, password from `ADMIN_PASSWORD`.

## Configuration

- ConnectionStrings:DefaultConnection — SQLite file path
- Storage.Provider — Local | AzureBlob | Gcs
  - Azure: ConnectionString, Container
  - Gcs: Bucket

## Azure (Container App / App Service via Docker)

Build and push image, then deploy:

```bash
docker build -t hrhelper:latest -f Dockerfile .
```

Configure `ADMIN_PASSWORD` and storage env vars in the service.

## Firebase Hosting + Cloud Run

- Build container and deploy to Cloud Run (or Cloud Run for Anthos) and point Firebase Hosting rewrite to the service URL. See `firebase.json`.

## Notes

- Admin can create three types of requests: Assignment, English Video, Questionnaire
- Users submit results; admin sees completed submissions in /admin.
