# scripts/deploy-cloudrun.ps1
# Параметры:
#   -ProjectId hrhelper-57162
#   -Region us-central1
#   -AdminPassword "...."
#   -ServiceAccountKeyPath "C:\path\sa.json"
#   -AppVersion "v-1.0.0" (необязательно; по умолчанию берётся имя тега или метка времени)

param(
  [Parameter(Mandatory=$true)][string]$ProjectId,
  [Parameter()][string]$Region = "us-central1",
  [Parameter(Mandatory=$true)][string]$AdminPassword,
  [Parameter(Mandatory=$true)][string]$ServiceAccountKeyPath,
  [Parameter()][string]$AppVersion
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

if (-not $AppVersion -or $AppVersion.Trim() -eq "") {
  if ($env:GITHUB_REF_NAME) {
    $AppVersion = $env:GITHUB_REF_NAME
  } else {
    # попытка вытащить из git tag; если не получится — метка времени
    try {
      $tag = (git describe --tags --always 2>$null)
      if ($tag) { $AppVersion = $tag } else { $AppVersion = (Get-Date -Format 'yyyyMMddHHmm') }
    } catch { $AppVersion = (Get-Date -Format 'yyyyMMddHHmm') }
  }
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
  --set-env-vars ADMIN_PASSWORD="$AdminPassword",APP_VERSION="$AppVersion"

Write-Host "Деплой Firebase Hosting (rewrite на Cloud Run)..."
firebase --project "$ProjectId" deploy --only hosting --non-interactive

Write-Host "Готово. Версия: $AppVersion"