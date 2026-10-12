BUILD SCRIPTS FOR ELECTRONCASH CLIENT
====================================

Два автоматических скрипта для сборки проекта в самостоящийся exe-бандл.

LINUX / MAC
-----------
./build.sh

Требует: dotnet CLI установленный и в PATH
Результат: ./build/ElectronCash.Client.exe + ./build/release/ (полный бандл)


WINDOWS (PowerShell)
-------------------
.\build.ps1

Требует: .NET SDK 8.0+ и PowerShell
Результат: .\build\ElectronCash.Client.exe + .\build\release\ (полный бандл)


ЧТО ДЕЛАЕТ СКРИПТ
-----------------
1. Очищает старые артефакты (bin/, obj/, ./build/)
2. dotnet build -c Release
3. dotnet publish -c Release --self-contained --runtime win-x64
4. Копирует exe в корень ./build/

Результат:
  ./build/ElectronCash.Client.exe        — готовый exe (копия)
  ./build/release/                       — полный standalone-бандл с runtime

Бандл можно запустить на ЛЮБОЙ Windows-машине (даже без .NET SDK).


ОПЦИИ (если нужны)
------------------
Изменить runtime:
  В build.sh: измени RUNTIME="win-x64" на нужное значение
  Варианты: win-x64, win-x86, win-arm64

Убрать --self-contained:
  Убери --self-contained из скриптов
  Тогда exe будет зависеть от установленного .NET Runtime


QUICK START
-----------
Windows:  .\build.ps1
Linux:    ./build.sh

Затем запусти exe:
  Windows: .\build\ElectronCash.Client.exe
  WSL:     .\build\release\ElectronCash.Client.exe
