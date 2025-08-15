# scripts/setup-wsl-github-runner.ps1
# Устанавливает WSL (Ubuntu), включает systemd, ставит gcloud + Node.js + Firebase CLI,
# регистрирует GitHub Actions runner внутри WSL и запускает его как systemd-сервис.
# Запускать PowerShell от имени администратора.
# Пример:
#   powershell -ExecutionPolicy Bypass -File .\scripts\setup-wsl-github-runner.ps1 `
#     -RepoUrl "https://github.com/<OWNER>/<REPO>" `
#     -RunnerToken "<TOKEN_FROM_GITHUB>" `
#     -RunnerName "wsl-01" `
#     -RunnerVersion "2.319.1"

param(
  [Parameter(Mandatory=$true)][string]$RepoUrl,
  [Parameter(Mandatory=$true)][string]$RunnerToken,
  [Parameter()][string]$RunnerName = "wsl-01",
  [Parameter()][string]$RunnerVersion = "2.319.1"
)

function Ensure-Admin {
  $currentUser = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
  if (-not $currentUser.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Запустите PowerShell от имени администратора."
    exit 1
  }
}

function Cmd-Exists($name) { $null -ne (Get-Command $name -ErrorAction SilentlyContinue) }

Ensure-Admin

Write-Host "Включаю компоненты WSL и виртуализации..."
dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart | Out-Null
dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart | Out-Null

# Проверка установленной Ubuntu
$distros = & wsl -l -v 2>$null
$hasUbuntu = $distros -match "Ubuntu"

if (-not $hasUbuntu) {
  Write-Host "Устанавливаю Ubuntu WSL..."
  wsl --install -d Ubuntu
  Write-Host "Ubuntu устанавливается. В этом шаге Windows может потребовать перезагрузку и создание пользователя в Ubuntu."
  Write-Host "После завершения настройки Ubuntu заново запустите этот скрипт."
  exit 0
}

Write-Host "Включаю systemd внутри WSL (Ubuntu)..."
# Вписать конфиг systemd и перезапустить WSL
wsl -d Ubuntu -e bash -lc "set -e; sudo mkdir -p /etc; if [ ! -f /etc/wsl.conf ] || ! grep -q '\[boot\]' /etc/wsl.conf; then echo -e '[boot]\nsystemd=true' | sudo tee /etc/wsl.conf >/dev/null; else sudo sed -i 's/^\[boot\].*/[boot]\nsystemd=true/g' /etc/wsl.conf; fi"
wsl --shutdown
# Старт дистрибутива вновь
wsl -d Ubuntu -e bash -lc "true" | Out-Null

Write-Host "Обновляю пакеты и ставлю утилиты (curl, unzip, jq, сертификаты)..."
wsl -d Ubuntu -e bash -lc "set -e; sudo apt-get update -y; sudo apt-get install -y curl unzip jq ca-certificates apt-transport-https gnupg"

Write-Host "Устанавливаю Node.js LTS (20.x) и Firebase CLI..."
wsl -d Ubuntu -e bash -lc "set -e; curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -; sudo apt-get install -y nodejs; sudo npm i -g firebase-tools"

Write-Host "Устанавливаю Google Cloud SDK (gcloud)..."
wsl -d Ubuntu -e bash -lc "set -e; echo 'deb [signed-by=/usr/share/keyrings/cloud.google.gpg] http://packages.cloud.google.com/apt cloud-sdk main' | sudo tee /etc/apt/sources.list.d/google-cloud-sdk.list >/dev/null; curl -s https://packages.cloud.google.com/apt/doc/apt-key.gpg | sudo gpg --dearmor -o /usr/share/keyrings/cloud.google.gpg; sudo apt-get update -y; sudo apt-get install -y google-cloud-cli"

Write-Host "Готовлю папку actions-runner и ставлю GitHub runner (v$RunnerVersion)..."
$runnerFile = "actions-runner-linux-x64-$RunnerVersion.tar.gz"
$runnerUrl = "https://github.com/actions/runner/releases/download/v$RunnerVersion/$runnerFile"

wsl -d Ubuntu -e bash -lc @"
set -e
mkdir -p \$HOME/actions-runner
cd \$HOME/actions-runner
if [ ! -f "$runnerFile" ]; then
  echo "Скачиваю $runnerFile..."
  curl -L -o "$runnerFile" "$runnerUrl"
fi
tar xzf "$runnerFile"
"@

Write-Host "Регистрирую раннер в репозитории..."
# Конфигурация раннера
$escapedRepoUrl = $RepoUrl.Replace('"','\"')
$escapedToken   = $RunnerToken.Replace('"','\"')
$escapedName    = $RunnerName.Replace('"','\"')

wsl -d Ubuntu -e bash -lc @"
set -e
cd \$HOME/actions-runner
./config.sh --url \"$escapedRepoUrl\" --token \"$escapedToken\" --name \"$escapedName\" --labels \"self-hosted,linux,wsl\" --unattended --replace
"@

Write-Host "Устанавливаю и запускаю runner как systemd-сервис..."
wsl -d Ubuntu -e bash -lc "set -e; cd \$HOME/actions-runner; sudo ./svc.sh install; sudo ./svc.sh start; sudo ./svc.sh status"

Write-Host "Проверка версий внутри WSL:"
wsl -d Ubuntu -e bash -lc "gcloud --version || true"
wsl -d Ubuntu -e bash -lc "node --version || true"
wsl -d Ubuntu -e bash -lc "npm --version || true"
wsl -d Ubuntu -e bash -lc "firebase --version || true"

Write-Host "Готово. Раннер зарегистрирован как сервис внутри WSL (Ubuntu) и запущен."
Write-Host "В GitHub: Settings Actions Runners вы увидите раннер с метками: self-hosted, linux, wsl"