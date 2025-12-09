#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Script de build local pour le projet Unity Nox Runtime
.DESCRIPTION
    Reproduit le workflow GitHub Actions en local pour builder le projet Unity
.PARAMETER Target
    Plateforme cible: win64, linux64, osx, android, ios
.PARAMETER Development
    Active le mode développement
.PARAMETER UnityPath
    Chemin vers l'exécutable Unity (optionnel, sinon utilise .env)
.PARAMETER Clean
    Nettoie le dossier Library avant le build
.PARAMETER DryRun
    Affiche la commande sans l'exécuter
.EXAMPLE
    .\build.ps1
    .\build.ps1 -Target win64 -Development
    .\build.ps1 -Target android -Clean
#>

param(
    [ValidateSet("win64", "linux64", "osx", "android", "ios")]
    [string]$Target = "",
    
    [switch]$Development,
    
    [string]$UnityPath = "",
    
    [switch]$Clean,
    
    [switch]$DryRun
)

# Couleurs pour les messages
function Write-Info { param($msg) Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-Warning { param($msg) Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Error { param($msg) Write-Host "[ERROR] $msg" -ForegroundColor Red }

# Charger les variables d'environnement depuis .env
function Load-EnvFile {
    param([string]$Path)
    
    if (Test-Path $Path) {
        Write-Info "Chargement de $Path"
        Get-Content $Path | ForEach-Object {
            if ($_ -match '^\s*([^#][^=]+)=(.*)$') {
                $name = $matches[1].Trim()
                $value = $matches[2].Trim()
                # Supprimer les guillemets si présents
                $value = $value -replace '^["'']|["'']$', ''
                [Environment]::SetEnvironmentVariable($name, $value, "Process")
            }
        }
    }
}

# Mapping des plateformes
$PlatformMapping = @{
    "win64"   = "StandaloneWindows64"
    "linux64" = "StandaloneLinux64"
    "osx"     = "StandaloneOSX"
    "android" = "Android"
    "ios"     = "iOS"
}

# Charger .env.local en priorité, sinon .env
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$EnvLocalPath = Join-Path $ScriptDir ".env.local"
$EnvPath = Join-Path $ScriptDir ".env"

if (Test-Path $EnvLocalPath) {
    Load-EnvFile $EnvLocalPath
} elseif (Test-Path $EnvPath) {
    Load-EnvFile $EnvPath
}

# Fonction pour gérer les valeurs par défaut (compatible PS 5.1)
function Get-EnvOrDefault {
    param([string]$EnvVar, [string]$Default)
    $value = [Environment]::GetEnvironmentVariable($EnvVar, "Process")
    if ($value) { return $value } else { return $Default }
}

# Valeurs par défaut depuis l'environnement
$UnityVersion = Get-EnvOrDefault "UNITY_VERSION" "6000.0.27f1"
$ProjectPath = Get-EnvOrDefault "PROJECT_PATH" "."
$BuildTarget = if ($Target) { $Target } else { Get-EnvOrDefault "BUILD_TARGET" "win64" }
$devEnv = Get-EnvOrDefault "DEVELOPMENT" "true"
$IsDevelopment = if ($Development) { $true } else { $devEnv -eq "true" }
$BuildName = Get-EnvOrDefault "BUILD_NAME" "NoxRuntime"
$BuildOutput = Get-EnvOrDefault "BUILD_OUTPUT" "./build"

# Déterminer le chemin Unity
if (-not $UnityPath) {
    $UnityPath = $env:UNITY_EDITOR_PATH
}

# Auto-détection Unity si non spécifié
if (-not $UnityPath -or -not (Test-Path $UnityPath)) {
    Write-Info "Recherche automatique de Unity $UnityVersion..."
    
    $possiblePaths = @(
        "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe",
        "C:\Program Files (x86)\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe",
        "$env:USERPROFILE\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe",
        "/Applications/Unity/Hub/Editor/$UnityVersion/Unity.app/Contents/MacOS/Unity",
        "$env:HOME/Unity/Hub/Editor/$UnityVersion/Editor/Unity"
    )
    
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $UnityPath = $path
            break
        }
    }
}

if (-not $UnityPath -or -not (Test-Path $UnityPath)) {
    Write-Error "Unity $UnityVersion introuvable!"
    Write-Info "Spécifiez le chemin avec -UnityPath ou dans .env.local"
    Write-Info "Chemins recherchés:"
    $possiblePaths | ForEach-Object { Write-Host "  - $_" }
    exit 1
}

Write-Success "Unity trouvé: $UnityPath"

# Vérifier si Unity est déjà ouvert avec ce projet
$unityProcesses = Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -match "runtime" }
if ($unityProcesses) {
    Write-Error "Unity est déjà ouvert avec ce projet!"
    Write-Warning "Fermez Unity ou utilisez 'Build > Build And Run' depuis l'éditeur"
    Write-Info "Processus Unity détectés:"
    $unityProcesses | ForEach-Object { Write-Host "  - PID: $($_.Id) - $($_.MainWindowTitle)" -ForegroundColor Gray }
    exit 1
}

# Résoudre le chemin du projet
$ProjectPath = Resolve-Path $ProjectPath -ErrorAction SilentlyContinue
if (-not $ProjectPath) {
    $ProjectPath = $ScriptDir
}

Write-Info "Projet: $ProjectPath"
Write-Info "Plateforme: $BuildTarget ($($PlatformMapping[$BuildTarget]))"
Write-Info "Mode développement: $IsDevelopment"

# Nettoyer si demandé
if ($Clean) {
    $LibraryPath = Join-Path $ProjectPath "Library"
    if (Test-Path $LibraryPath) {
        Write-Warning "Nettoyage du dossier Library..."
        Remove-Item -Recurse -Force $LibraryPath
        Write-Success "Library supprimé"
    }
}

# Créer le dossier de build
$BuildTargetName = $PlatformMapping[$BuildTarget]
$OutputPath = Join-Path $BuildOutput $BuildTargetName
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

# Fichier de log
$LogFile = Join-Path $ProjectPath "build_log.txt"

# Construire les arguments Unity
$unityArgs = @(
    "-quit",
    "-batchmode",
    "-nographics",
    "-projectPath", $ProjectPath,
    "-executeMethod", "dev.nox.game_builder.BuildGame.PerformBuild",
    "-buildTarget", $BuildTarget,
    "-development", $IsDevelopment.ToString().ToLower(),
    "-logFile", $LogFile
)

# Afficher la commande
Write-Info "Commande:"
Write-Host "  `"$UnityPath`" $($unityArgs -join ' ')" -ForegroundColor Gray
Write-Info "Logs: $LogFile"

if ($DryRun) {
    Write-Warning "Mode DryRun - Commande non exécutée"
    exit 0
}

# Exécuter le build
Write-Info "Démarrage du build..."
$startTime = Get-Date

# Supprimer l'ancien log
if (Test-Path $LogFile) { Remove-Item $LogFile -Force }

try {
    $process = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -NoNewWindow -PassThru -Wait
    $exitCode = $process.ExitCode
} catch {
    Write-Error "Erreur lors de l'exécution: $_"
    exit 1
}

$duration = (Get-Date) - $startTime

# Vérifier le résultat
if ($exitCode -eq 0) {
    Write-Success "Build terminé avec succès en $($duration.ToString('hh\:mm\:ss'))"
    Write-Info "Output: $OutputPath"
    
    # Lister les fichiers générés
    if (Test-Path $OutputPath) {
        Write-Info "Fichiers générés:"
        Get-ChildItem $OutputPath -Recurse -File | Select-Object -First 20 | ForEach-Object {
            Write-Host "  - $($_.FullName)" -ForegroundColor Gray
        }
    }
} else {
    Write-Error "Build échoué avec le code: $exitCode"
    
    # Afficher les dernières lignes du log
    if (Test-Path $LogFile) {
        Write-Info "Dernières lignes du log:"
        Get-Content $LogFile -Tail 50 | ForEach-Object { Write-Host $_ -ForegroundColor Gray }
    }
    
    # Vérifier les logs de build
    $buildReportPath = Join-Path $ProjectPath "Library\LastBuild.buildreport"
    if (Test-Path $buildReportPath) {
        Write-Info "Rapport de build disponible: $buildReportPath"
    }
    
    Write-Info "Log complet: $LogFile"
    
    exit $exitCode
}
