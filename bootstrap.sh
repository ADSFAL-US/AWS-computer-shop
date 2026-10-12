#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

GLOBAL_JSON="$SCRIPT_DIR/global.json"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet не найден в PATH." >&2
  echo "Arch Linux:  sudo pacman -S dotnet-sdk-8.0 aspnet-runtime-8.0" >&2
  echo "Другие ОС:   https://dotnet.microsoft.com/download/dotnet/8.0" >&2
  exit 1
fi

echo "==> Установленные SDK:"
dotnet --list-sdks

if ! dotnet --list-sdks | grep -qE '^8\.'; then
  echo "Нужен .NET SDK 8.x. Установи его и запусти снова." >&2
  exit 1
fi

# Если global.json требует версию, которой нет — разрешаем rollForward
if [[ -f "$GLOBAL_JSON" ]] && ! (cd "$SCRIPT_DIR/ElectronCash" && dotnet --version >/dev/null 2>&1); then
  echo "==> global.json требует отсутствующий SDK. Добавляю rollForward=latestMajor"
  if command -v python3 >/dev/null 2>&1; then
    python3 - "$GLOBAL_JSON" <<'PY'
import json, sys, pathlib
p = pathlib.Path(sys.argv[1])
data = json.loads(p.read_text())
data.setdefault("sdk", {})["rollForward"] = "latestMajor"
p.write_text(json.dumps(data, indent=2) + "\n")
print("Обновлён", p)
PY
  else
    echo "python3 не найден — открой $GLOBAL_JSON и добавь \"rollForward\": \"latestMajor\" в секцию sdk вручную." >&2
    exit 1
  fi
fi

cd "$SCRIPT_DIR/ElectronCash"
echo "==> Версия SDK: $(dotnet --version)"

echo "==> dotnet restore"
dotnet restore

echo "==> dotnet build"
dotnet build --configuration Release --no-restore

if [[ "${1:-}" == "--run" ]]; then
  API_PROJECT=$(find . -name 'ElectronCash.Api.csproj' -print -quit)
  if [[ -z "$API_PROJECT" ]]; then
    echo "Не найден ElectronCash.Api.csproj" >&2
    exit 1
  fi
  echo "==> Запуск API: $API_PROJECT"
  exec dotnet run --project "$API_PROJECT" --configuration Release --no-build
fi

echo
echo "Готово. Запустить API:   ./bootstrap.sh --run"
