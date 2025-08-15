# scripts/setup-windows-runner.ps1
# Installs: Google Cloud SDK (gcloud), Node.js LTS, Firebase CLI (+optional Docker Desktop)
# Run PowerShell as Administrator

param(
  [switch]$InstallDocker = $false
)

function Ensure-Admin {
  $currentUser = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
  if (-not $currentUser.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Run PowerShell as Administrator."
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
    Write-Host "Installing Chocolatey..."
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
    Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
    Refresh-Path
  }
  if (Cmd-Exists "choco") { return "choco" }
  throw "No package manager detected (winget/choco). Install one and rerun."
}

function Refresh-Path {
  $global:env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" +
                     [System.Environment]::GetEnvironmentVariable("Path","User")
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
  Write-Host "gcloud already installed"
}

# Node.js LTS
if (-not (Cmd-Exists "node")) {
  if ($pkgMgr -eq "winget") {
    Install-WithWinget "OpenJS.NodeJS.LTS"
  } else {
    choco install -y nodejs-lts
  }
} else {
  Write-Host "Node.js already installed"
}

# refresh PATH so npm is resolvable in current session
Refresh-Path

# Firebase CLI (npm global)
if (-not (Cmd-Exists "firebase")) {
  if (Cmd-Exists "npm") {
    npm install -g firebase-tools
  } else {
    $npm = "C:\\Program Files\\nodejs\\npm.cmd"
    if (Test-Path $npm) {
      & $npm install -g firebase-tools
    } else {
      throw "npm not found. Reopen PowerShell or ensure Node.js is installed and in PATH."
    }
  }
} else {
  Write-Host "Firebase CLI already installed"
}

# Docker Desktop (optional)
if ($InstallDocker -and -not (Cmd-Exists "docker")) {
  if (Cmd-Exists "winget") {
    Install-WithWinget "Docker.DockerDesktop"
  } elseif (Cmd-Exists "choco") {
    choco install -y docker-desktop
  } else {
    Write-Error "Neither winget nor choco found. Install a package manager and rerun."
  }
  Write-Host "You may need to restart after installing Docker Desktop."
}

Write-Host "Versions:"
if (Cmd-Exists "gcloud") { gcloud --version }
if (Cmd-Exists "node") { node --version }
if (Cmd-Exists "npm") { npm --version }
if (Cmd-Exists "firebase") { firebase --version }
if (Cmd-Exists "docker") { docker --version }
Write-Host "Done."