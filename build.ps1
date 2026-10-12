# Build script for ElectronCash Client
# Publishes as self-contained standalone executable to ./build folder
# Usage: .\build.ps1

$ErrorActionPreference = "Stop"

$projectPath = "ElectronCash/src/ElectronCash.Client/ElectronCash.Client.csproj"
$buildDir = "./build"
$configuration = "Release"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ElectronCash Client Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Clean build directory
Write-Host "Cleaning build directory..." -ForegroundColor Yellow
if (Test-Path $buildDir) {
    Remove-Item $buildDir -Recurse -Force
}
New-Item -ItemType Directory -Path $buildDir -Force | Out-Null

# Clean obj/bin
Write-Host "Cleaning project artifacts..." -ForegroundColor Yellow
if (Test-Path "ElectronCash/src/ElectronCash.Client/bin") {
    Remove-Item "ElectronCash/src/ElectronCash.Client/bin" -Recurse -Force
}
if (Test-Path "ElectronCash/src/ElectronCash.Client/obj") {
    Remove-Item "ElectronCash/src/ElectronCash.Client/obj" -Recurse -Force
}

# Build solution
Write-Host "Building solution..." -ForegroundColor Yellow
& dotnet build ElectronCash.sln -c $configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Publish as self-contained
Write-Host "" -ForegroundColor Green
Write-Host "Publishing self-contained executable..." -ForegroundColor Yellow
& dotnet publish $projectPath `
    -c $configuration `
    --self-contained `
    --runtime win-x64 `
    -o $buildDir/release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}

# Move executable to root of build
$exePath = "$buildDir/release/ElectronCash.Client.exe"
if (Test-Path $exePath) {
    Copy-Item $exePath $buildDir/
    Write-Host "" -ForegroundColor Green
    Write-Host "✓ Build complete!" -ForegroundColor Green
    Write-Host "  Executable: $buildDir/ElectronCash.Client.exe" -ForegroundColor Cyan
    Write-Host "  Full build: $buildDir/release/" -ForegroundColor Cyan
    Write-Host "" -ForegroundColor Green
} else {
    Write-Host "Error: Executable not found at $exePath" -ForegroundColor Red
    exit 1
}
