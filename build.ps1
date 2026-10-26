# Build script for ElectronCash Client
# Publishes as self-contained standalone executable to ./build folder
# Usage: .\build.ps1
#
# Приоритет выбора dotnet:
#   1) $env:USERPROFILE\.dotnet\dotnet.exe   (портативный SDK от bootstrap.ps1)
#   2) $env:DOTNET_ROOT\dotnet.exe
#   3) глобальный dotnet из PATH

$ErrorActionPreference = "Stop"

# Determine script root
$scriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
if ([string]::IsNullOrEmpty($scriptRoot)) {
    $scriptRoot = Get-Location
}
Set-Location $scriptRoot

Write-Host "Script root: $scriptRoot"

# ---------- Выбор экземпляра dotnet (приоритет — портативный) ----------
$bootstrapDotnetDir = Join-Path $env:USERPROFILE ".dotnet"
$bootstrapDotnet    = Join-Path $bootstrapDotnetDir "dotnet.exe"

$dotnetExe = $null
$dotnetSource = $null

if (Test-Path $bootstrapDotnet) {
    $dotnetExe = $bootstrapDotnet
    $dotnetSource = "bootstrap ($bootstrapDotnet)"
}
elseif ($env:DOTNET_ROOT -and (Test-Path (Join-Path $env:DOTNET_ROOT "dotnet.exe"))) {
    $dotnetExe = Join-Path $env:DOTNET_ROOT "dotnet.exe"
    $dotnetSource = "DOTNET_ROOT ($dotnetExe)"
}
else {
    $globalDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($globalDotnet) {
        $dotnetExe = $globalDotnet.Source
        $dotnetSource = "PATH ($dotnetExe)"
    }
}

if (-not $dotnetExe) {
    Write-Host "Error: dotnet не найден. Запустите bootstrap.ps1 или установите .NET SDK 8."
    exit 1
}

# Принудительно прокидываем окружение текущей сессии на выбранный экземпляр,
# чтобы SDK/MSBuild не подхватили случайно другой dotnet.
$dotnetDir = Split-Path -Parent $dotnetExe
$env:DOTNET_ROOT = $dotnetDir
if ($env:PATH -notlike "*$dotnetDir*") {
    $env:PATH = "$dotnetDir;$env:PATH"
}

Write-Host ("Using dotnet: {0}" -f $dotnetSource)

# Проверим, что это действительно .NET 8 SDK
$sdks = & $dotnetExe --list-sdks 2>$null
if ($LASTEXITCODE -ne 0 -or -not ($sdks -match "^8\.0\.")) {
    Write-Host "Error: выбранный dotnet не содержит SDK 8.x:"
    Write-Host ($sdks | Out-String)
    Write-Host "Запустите bootstrap.ps1 или установите .NET SDK 8."
    exit 1
}

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
& $dotnetExe build $solutionPath -c $configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!"
    exit 1
}

# Publish as self-contained
Write-Host ""
Write-Host "Publishing self-contained executable (requires .NET 8 runtime)..."

# Try self-contained first, fall back to portable if fails
& $dotnetExe publish $projectPath `
    -c $configuration `
    --self-contained `
    --runtime win-x64 `
    --no-restore `
    -o $buildDir/release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Self-contained publish failed, trying portable build..."
    if (Test-Path -Path "$buildDir/release") {
        Remove-Item -Path "$buildDir/release" -Recurse -Force -ErrorAction SilentlyContinue
    }

    & $dotnetExe publish $projectPath `
        -c $configuration `
        --no-restore `
        -o $buildDir/release

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Publish failed!"
        exit 1
    }

    Write-Host "Portable build successful (requires .NET 8 runtime on target machine)"
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