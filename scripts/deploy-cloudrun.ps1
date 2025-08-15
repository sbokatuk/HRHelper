# scripts/deploy-cloudrun.ps1
# Параметры:
#   -ProjectId hrhelper-57162
#   -Region us-central1
#   -AdminPassword "...."
#   -ServiceAccountKeyPath "C:\path\sa.json"

param(
  [Parameter(Mandatory=$true)][string]$ProjectId,
  [Parameter()][string]$Region = "us-central1",
  [Parameter(Mandatory=$true)][string]$AdminPassword,
  [Parameter(Mandatory=$true)][string]$ServiceAccountKeyPath
)

function Require-Cmd($name) {
  if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
    Write-Error "Команда '$name' не найдена. Установите зависимости и повторите."
    exit 1
  }
}

Require-Cmd "gcloud"
Require-Cmd "firebase"

if (-not (Test-Path $ServiceAccountKeyPath)) {
  Write-Error "Файл сервисного аккаунта не найден: $ServiceAccountKeyPath"
  exit 1
}

$env:GOOGLE_APPLICATION_CREDENTIALS = $ServiceAccountKeyPath

Write-Host "Аутентификация сервисного аккаунта..."
gcloud auth activate-service-account --key-file "$ServiceAccountKeyPath"

Write-Host "Выбор проекта..."
gcloud config set project "$ProjectId" | Out-Null

Write-Host "Включение API (идемпотентно)..."
gcloud services enable `
  run.googleapis.com `
  cloudbuild.googleapis.com `
  artifactregistry.googleapis.com `
  firebase.googleapis.com `
  firebasehosting.googleapis.com --quiet

Write-Host "Деплой в Cloud Run из исходников (Buildpacks + Cloud Build)..."
gcloud run deploy hrhelper `
  --source . `
  --region "$Region" `
  --platform managed `
  --allow-unauthenticated `
  --set-env-vars ADMIN_PASSWORD="$AdminPassword"

Write-Host "Деплой Firebase Hosting (rewrite на Cloud Run)..."
firebase --project "$ProjectId" deploy --only hosting --non-interactive

Write-Host "Готово."