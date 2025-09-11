param(
    [Parameter(HelpMessage="Build configuration (Debug/Release)")]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [Parameter(HelpMessage="CMake generator")]
    [string]$Generator = "Visual Studio 17 2022",
    
    [Parameter(HelpMessage="Target platform")]
    [ValidateSet("x64", "x86", "ARM64")]
    [string]$Platform = "x64",
    
    [Parameter(HelpMessage="vcpkg triplet override")]
    [string]$Triplet = "",
    
    [Parameter(HelpMessage="Clean build directory before building")]
    [switch]$Clean,
    
    [Parameter(HelpMessage="Skip dependency installation")]
    [switch]$SkipInstall,
    
    [Parameter(HelpMessage="Show help message")]
    [switch]$Help
)

if ($Help) {
    Write-Host @"
Hactazia Video Player Native Library Build Script

Usage: .\build.ps1 [parameters]

Parameters:
  -Configuration <Debug|Release>  Build configuration (default: Release)
  -Generator <generator>          CMake generator (default: Visual Studio 17 2022)
  -Platform <x64|x86|ARM64>      Target platform (default: x64)
  -Triplet <triplet>             vcpkg triplet override
  -Clean                         Clean build directory before building
  -SkipInstall                   Skip dependency installation
  -Help                          Show this help message

Examples:
  .\build.ps1 -Configuration Debug
  .\build.ps1 -Platform x64 -Clean
  .\build.ps1 -SkipInstall
"@
    exit 0
}

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Hactazia Video Player Native Library Build Script" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

# Set triplet if not specified
if (-not $Triplet) {
    $Triplet = "$Platform-windows"
}

Write-Host "Build Configuration:" -ForegroundColor Yellow
Write-Host "  Configuration: $Configuration"
Write-Host "  Generator: $Generator"
Write-Host "  Platform: $Platform"
Write-Host "  Triplet: $Triplet"
Write-Host ""

# Check for vcpkg
Write-Host "Checking for vcpkg..." -ForegroundColor Green
if (-not $env:VCPKG_ROOT) {
    Write-Error "VCPKG_ROOT environment variable is not set"
    Write-Host "Please install vcpkg and set VCPKG_ROOT to the vcpkg installation directory" -ForegroundColor Red
    Write-Host "See: https://github.com/Microsoft/vcpkg#quick-start-windows" -ForegroundColor Red
    exit 1
}

$vcpkgExe = Join-Path $env:VCPKG_ROOT "vcpkg.exe"
if (-not (Test-Path $vcpkgExe)) {
    Write-Error "vcpkg.exe not found at $vcpkgExe"
    Write-Host "Please ensure vcpkg is properly installed" -ForegroundColor Red
    exit 1
}

Write-Host "Found vcpkg at: $env:VCPKG_ROOT" -ForegroundColor Green

# Install dependencies via vcpkg
if (-not $SkipInstall) {
    Write-Host "Installing dependencies via vcpkg..." -ForegroundColor Green
    
    if (-not (Test-Path "vcpkg.json")) {
        Write-Error "vcpkg.json not found in current directory"
        exit 1
    }
    
    Write-Host "Installing dependencies for triplet: $Triplet" -ForegroundColor Yellow
    & $vcpkgExe install --triplet $Triplet
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install dependencies via vcpkg"
        exit 1
    }
    
    Write-Host "Dependencies successfully installed" -ForegroundColor Green
} else {
    Write-Host "Skipping dependency installation" -ForegroundColor Yellow
}

# Check for Visual Studio
Write-Host "Checking for Visual Studio..." -ForegroundColor Green
if ($Generator -like "*Visual Studio*") {
    $devenv = Get-Command "devenv.exe" -ErrorAction SilentlyContinue
    if (-not $devenv) {
        Write-Error "Visual Studio not found"
        Write-Host "Please install Visual Studio with C++ development tools" -ForegroundColor Red
        exit 1
    }
    Write-Host "Found Visual Studio" -ForegroundColor Green
}

# Create build directory
$buildDir = "build"
if ($Clean -and (Test-Path $buildDir)) {
    Write-Host "Cleaning build directory..." -ForegroundColor Yellow
    Remove-Item $buildDir -Recurse -Force
}

if (-not (Test-Path $buildDir)) {
    New-Item -ItemType Directory -Path $buildDir | Out-Null
}

Set-Location $buildDir

# Run CMake configuration
Write-Host "Running CMake configuration..." -ForegroundColor Green
$vcpkgToolchain = Join-Path $env:VCPKG_ROOT "scripts\buildsystems\vcpkg.cmake"

$cmakeArgs = @(
    "..",
    "-G", $Generator,
    "-A", $Platform,
    "-DCMAKE_BUILD_TYPE=$Configuration",
    "-DCMAKE_TOOLCHAIN_FILE=$vcpkgToolchain",
    "-DVCPKG_TARGET_TRIPLET=$Triplet"
)

& cmake @cmakeArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "CMake configuration failed"
    Set-Location ..
    exit 1
}

# Build the project
Write-Host ""
Write-Host "Building project..." -ForegroundColor Green
& cmake --build . --config $Configuration --parallel

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    Set-Location ..
    exit 1
}

# Output is now directly placed in Unity Plugins directory by CMake
Write-Host ""
Write-Host "Build output is directly placed in Unity Plugins directory" -ForegroundColor Green

Set-Location ..

# Display results
Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Build process completed!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Cyan

# Determine platform and architecture for output path
$osName = "Windows"
$archName = if ($Platform -eq "x64") { "x86_64" } else { $Platform }
$unityPluginDir = "..\Plugins\$osName\$archName"

if (Test-Path $unityPluginDir) {
    Write-Host ""
    Write-Host "Output files in Unity Plugins directory:" -ForegroundColor Yellow
    Get-ChildItem -Path $unityPluginDir -File | Format-Table Name, Length, LastWriteTime
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Library built directly to $unityPluginDir" -ForegroundColor White
Write-Host "2. FFmpeg runtime DLLs included automatically" -ForegroundColor White
Write-Host "3. Configure the plugin import settings in Unity" -ForegroundColor White
Write-Host "4. Test the video player in your Unity project" -ForegroundColor White
