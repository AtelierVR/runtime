# Script de build pour le mod BlacklistTest
# Génère le dossier et l'archive du mod

param(
    [switch]$Archive,      # Créer une archive .noxmod
    [switch]$Install,      # Installer dans le dossier mods
    [string]$OutputDir = "build"
)

$ErrorActionPreference = "Stop"
$ModName = "blacklist_test"
$ModId = "com.example.blacklist_test"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Building BlacklistTest Mod" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Chemins
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path "$ScriptDir\..\.."
$UnityAssemblies = "$ProjectRoot\Library\ScriptAssemblies"
$UnityEngineDir = "$ProjectRoot\Library\ScriptAssemblies\..\UnityAssemblies"
$OutputPath = "$ScriptDir\$OutputDir"
$ModOutputPath = "$OutputPath\$ModName"

# Vérifier que les assemblies Unity existent
if (-not (Test-Path $UnityAssemblies)) {
    Write-Host "Error: Unity assemblies not found at $UnityAssemblies" -ForegroundColor Red
    Write-Host "Please open the project in Unity first to generate the assemblies." -ForegroundColor Yellow
    exit 1
}

# Créer le dossier de sortie
Write-Host "`nCreating output directory..." -ForegroundColor Yellow
if (Test-Path $ModOutputPath) {
    Remove-Item -Recurse -Force $ModOutputPath
}
New-Item -ItemType Directory -Path $ModOutputPath -Force | Out-Null

# Trouver le compilateur C#
$cscPath = $null

# Essayer dotnet d'abord
$dotnetPath = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnetPath) {
    Write-Host "Using dotnet SDK for compilation..." -ForegroundColor Green
    
    # Compiler avec dotnet
    Push-Location $ScriptDir
    try {
        dotnet build BlacklistTestMod.csproj -c Release -o "$ModOutputPath"
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build failed"
        }
    }
    finally {
        Pop-Location
    }
}
else {
    # Fallback: utiliser csc directement
    $cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    if (-not (Test-Path $cscPath)) {
        $cscPath = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    }
    
    if (-not (Test-Path $cscPath)) {
        Write-Host "Error: Could not find C# compiler (csc.exe or dotnet)" -ForegroundColor Red
        Write-Host "Please install .NET SDK or Visual Studio Build Tools" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "Using csc.exe for compilation..." -ForegroundColor Green
    
    # Références
    $references = @(
        "/reference:`"$UnityAssemblies\Nox.CCK.dll`"",
        "/reference:`"$UnityAssemblies\UniTask.dll`""
    )
    
    # Trouver UnityEngine.CoreModule.dll
    $unityCoreDll = Get-ChildItem -Path "$ProjectRoot\Library" -Filter "UnityEngine.CoreModule.dll" -Recurse | Select-Object -First 1
    if ($unityCoreDll) {
        $references += "/reference:`"$($unityCoreDll.FullName)`""
    }
    
    $refArgs = $references -join " "
    $sourceFiles = Get-ChildItem -Path $ScriptDir -Filter "*.cs" | ForEach-Object { "`"$($_.FullName)`"" }
    $sourceArgs = $sourceFiles -join " "
    
    $cscArgs = "/target:library /out:`"$ModOutputPath\BlacklistTestMod.dll`" $refArgs $sourceArgs"
    
    Write-Host "Compiling..." -ForegroundColor Yellow
    $process = Start-Process -FilePath $cscPath -ArgumentList $cscArgs -NoNewWindow -PassThru -Wait
    
    if ($process.ExitCode -ne 0) {
        Write-Host "Compilation failed!" -ForegroundColor Red
        exit 1
    }
}

Write-Host "Compilation successful!" -ForegroundColor Green

# Copier les fichiers du mod
Write-Host "`nCopying mod files..." -ForegroundColor Yellow
Copy-Item "$ScriptDir\nox.mod.json" "$ModOutputPath\"

# Nettoyer les fichiers inutiles
$filesToRemove = @("*.deps.json", "*.pdb", "Nox.CCK.dll", "UniTask.dll", "UnityEngine*.dll")
foreach ($pattern in $filesToRemove) {
    Get-ChildItem -Path $ModOutputPath -Filter $pattern -ErrorAction SilentlyContinue | Remove-Item -Force
}

Write-Host "Mod files copied!" -ForegroundColor Green

# Créer l'archive si demandé
if ($Archive) {
    Write-Host "`nCreating archive..." -ForegroundColor Yellow
    $archivePath = "$OutputPath\$ModName.noxmod"
    $zipPath = "$OutputPath\$ModName.zip"
    
    if (Test-Path $archivePath) {
        Remove-Item $archivePath -Force
    }
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    
    # Compress-Archive ne supporte que .zip, on crée un .zip puis on renomme
    Compress-Archive -Path "$ModOutputPath\*" -DestinationPath $zipPath -Force
    Rename-Item -Path $zipPath -NewName "$ModName.noxmod"
    Write-Host "Archive created: $archivePath" -ForegroundColor Green
}

# Installer si demandé
if ($Install) {
    Write-Host "`nInstalling mod..." -ForegroundColor Yellow
    $modsPath = "$env:APPDATA\.nox\mods"
    
    if (-not (Test-Path $modsPath)) {
        New-Item -ItemType Directory -Path $modsPath -Force | Out-Null
    }
    
    $installPath = "$modsPath\$ModName"
    if (Test-Path $installPath) {
        Remove-Item -Recurse -Force $installPath
    }
    
    Copy-Item -Recurse $ModOutputPath $installPath
    Write-Host "Mod installed to: $installPath" -ForegroundColor Green
}

# Afficher le résumé
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Build Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Output folder: $ModOutputPath" -ForegroundColor White

if ($Archive) {
    Write-Host "Archive: $OutputPath\$ModName.noxmod" -ForegroundColor White
}

if ($Install) {
    Write-Host "Installed to: $env:APPDATA\.nox\mods\$ModName" -ForegroundColor White
}

Write-Host "`nFiles in output:" -ForegroundColor Yellow
Get-ChildItem $ModOutputPath | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Gray }
