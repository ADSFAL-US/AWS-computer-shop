# Build script for ElectronCash Client
# Publishes as self-contained standalone executable to ./build folder
# Usage: .\build.ps1

$ErrorActionPreference = "Stop"

# Determine script root
$scriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
if ([string]::IsNullOrEmpty($scriptRoot)) {
    $scriptRoot = Get-Location
}
Set-Location $scriptRoot

Write-Host "Script root: $scriptRoot"

$projectPath = "ElectronCash/src/ElectronCash.Client/ElectronCash.Client.csproj"
$solutionPath = "ElectronCash/ElectronCash.sln"
$buildDir = "./build"
$configuration = "Release"

# Verify project exists
if (-not (Test-Path -Path $projectPath)) {
    Write-Host ("Error: Project file not found at " + $projectPath)
    Write-Host ("Current directory: " + (Get-Location))
    exit 1
}

Write-Host "========================================"
Write-Host "ElectronCash Client Build Script"
Write-Host "========================================"
Write-Host ""

# Clean build directory
Write-Host "Cleaning build directory..."
if (Test-Path -Path $buildDir) {
    Remove-Item -Path $buildDir -Recurse -Force
}
New-Item -ItemType Directory -Path $buildDir -Force | Out-Null

# Clean obj/bin
Write-Host "Cleaning project artifacts..."
if (Test-Path -Path "ElectronCash/src/ElectronCash.Client/bin") {
    Remove-Item -Path "ElectronCash/src/ElectronCash.Client/bin" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path -Path "ElectronCash/src/ElectronCash.Client/obj") {
    Remove-Item -Path "ElectronCash/src/ElectronCash.Client/obj" -Recurse -Force -ErrorAction SilentlyContinue
}

# Build solution
Write-Host "Building solution..."
& dotnet build $solutionPath -c $configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!"
    exit 1
}

# Publish as self-contained
Write-Host ""
Write-Host "Publishing self-contained executable..."
& dotnet publish $projectPath `
    -c $configuration `
    --self-contained `
    --runtime win-x64 `
    -o $buildDir/release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed!"
    exit 1
}

# Move executable to root of build
$exePath = $buildDir + "/release/ElectronCash.Client.exe"
if (Test-Path -Path $exePath) {
    Copy-Item -Path $exePath -Destination $buildDir
    Write-Host ""
    Write-Host "Build complete!"
    Write-Host ("  Executable: " + $buildDir + "/ElectronCash.Client.exe")
    Write-Host ("  Full build: " + $buildDir + "/release/")
    Write-Host ""
} else {
    Write-Host ("Error: Executable not found at " + $exePath)
    exit 1
}
