# scripts/setup-windows-runner.ps1
# Устанавливает: Google Cloud SDK (gcloud), Node.js LTS, Firebase CLI (+опционально Docker Desktop)
# Запускать PowerShell от имени администратора

param(
  [switch]$InstallDocker = $false
)

function Ensure-Admin {
  $currentUser = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
  if (-not $currentUser.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Запустите PowerShell от имени администратора."
    exit 1
  }
}

function Cmd-Exists($name) {
  $null -ne (Get-Command $name -ErrorAction SilentlyContinue)
}

function Install-WithWinget {
  param([string]$id)
  Write-Host "winget install $id"
  winget install --id $id --exact --accept-package-agreements --accept-source-agreements --silent
}

function Ensure-WingetOrChoco {
  if (Cmd-Exists "winget") { return "winget" }
  if (-not (Cmd-Exists "choco")) {
    Write-Host "Устанавливаю Chocolatey..."
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
    Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
  }
  return "choco"
}

Ensure-Admin

$pkgMgr = Ensure-WingetOrChoco

# Google Cloud SDK
if (-not (Cmd-Exists "gcloud")) {
  if ($pkgMgr -eq "winget") {
    Install-WithWinget "Google.CloudSDK"
  } else {
    choco install -y googlecloudsdk
  }
} else {
  Write-Host "gcloud уже установлен"
}

# Node.js LTS
if (-not (Cmd-Exists "node")) {
  if ($pkgMgr -eq "winget") {
    Install-WithWinget "OpenJS.NodeJS.LTS"
  } else {
    choco install -y nodejs-lts
  }
} else {
  Write-Host "Node.js уже установлен"
}

# Firebase CLI (npm global)
if (-not (Cmd-Exists "firebase")) {
  npm install -g firebase-tools
} else {
  Write-Host "Firebase CLI уже установлен"
}

# Docker Desktop (опционально)
if ($InstallDocker -and -not (Cmd-Exists "docker")) {
  if ($pkgMgr -eq "winget") {
    Install-WithWinget "Docker.DockerDesktop"
  } else {
    choco install -y docker-desktop
  }
  Write-Host "Перезагрузите компьютер после установки Docker Desktop."
}

Write-Host "Проверка версий:"
if (Cmd-Exists "gcloud") { gcloud --version }
if (Cmd-Exists "node") { node --version }
if (Cmd-Exists "npm") { npm --version }
if (Cmd-Exists "firebase") { firebase --version }
if (Cmd-Exists "docker") { docker --version }
Write-Host "Готово."