# bootstrap.ps1
# Устанавливает портативный .NET SDK 8.0 без прав администратора
# Запуск:  powershell -ExecutionPolicy Bypass -File .\bootstrap.ps1

[CmdletBinding()]
param(
    [string]$SdkVersion = "8.0.404",
    [string]$InstallDir = "$env:USERPROFILE\.dotnet"
)

$ErrorActionPreference = "Stop"

function Write-Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Write-Ok($msg)   { Write-Host "    $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "    $msg" -ForegroundColor Yellow }

# ---------- 1. Проверяем, есть ли уже нужный SDK ----------
Write-Step "Проверка установленного .NET SDK"

$dotnetExe = Join-Path $InstallDir "dotnet.exe"
$needInstall = $true

if (Test-Path $dotnetExe) {
    $installed = & $dotnetExe --list-sdks 2>$null
    if ($installed -match "^8\.0\.") {
        Write-Ok "SDK 8.x уже установлен в $InstallDir"
        $needInstall = $false
    } else {
        Write-Warn "В $InstallDir найден dotnet, но без SDK 8.x. Переустановим."
    }
} else {
    # Может, SDK стоит глобально (вдруг у кого-то есть права)
    $globalDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($globalDotnet) {
        $ver = & dotnet --list-sdks 2>$null
        if ($ver -match "^8\.0\.") {
            Write-Ok "Найден глобальный .NET SDK 8.x: $($globalDotnet.Source)"
            $needInstall = $false
        }
    }
}

# ---------- 2. Скачиваем и распаковываем SDK ----------
if ($needInstall) {
    Write-Step "Установка .NET SDK $SdkVersion в $InstallDir"

    $url  = "https://dotnetcli.azureedge.net/dotnet/Sdk/$SdkVersion/dotnet-sdk-$SdkVersion-win-x64.zip"
    $zip  = Join-Path $env:TEMP "dotnet-sdk-$SdkVersion-win-x64.zip"

    if (-not (Test-Path $InstallDir)) {
        New-Item -ItemType Directory -Path $InstallDir | Out-Null
    }

    Write-Host "    Скачивание: $url"
    # В старых PowerShell Invoke-WebRequest тормозит — используем WebClient
    $wc = New-Object System.Net.WebClient
    $wc.DownloadFile($url, $zip)
    Write-Ok "Скачано: $zip"

    Write-Host "    Распаковка..."
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zip, $InstallDir, $true)
    Remove-Item $zip -Force
    Write-Ok "Распаковано в $InstallDir"

    # ---------- 3. Прописываем переменные среды (User, без админа) ----------
    Write-Step "Настройка переменных среды (User)"

    [System.Environment]::SetEnvironmentVariable("DOTNET_ROOT", $InstallDir, "User")
    [System.Environment]::SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1", "User")
    [System.Environment]::SetEnvironmentVariable("DOTNET_NOLOGO", "1", "User")

    $userPath = [System.Environment]::GetEnvironmentVariable("PATH", "User")
    if ($userPath -notlike "*$InstallDir*") {
        [System.Environment]::SetEnvironmentVariable("PATH", "$userPath;$InstallDir", "User")
        Write-Ok "PATH обновлён (User)"
    } else {
        Write-Ok "PATH уже содержит $InstallDir"
    }

    # Обновляем PATH в текущей сессии
    $env:DOTNET_ROOT = $InstallDir
    $env:PATH = "$env:PATH;$InstallDir"
}

# ---------- 4. Проверка ----------
Write-Step "Проверка"

$finalDotnet = if (Test-Path $dotnetExe) { $dotnetExe } else { "dotnet" }
& $finalDotnet --version
$sdks = & $finalDotnet --list-sdks

if ($sdks -match "8\.0\.") {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host " Готово! .NET SDK 8.x доступен." -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "ВАЖНО: открой НОВЫЙ терминал, чтобы подхватились переменные среды." -ForegroundColor Yellow
    Write-Host "Потом выполни:" -ForegroundColor Yellow
    Write-Host "    dotnet restore" -ForegroundColor White
    Write-Host "    dotnet build"   -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "Что-то пошло не так. Проверь вывод выше." -ForegroundColor Red
    exit 1
}