# Электронная касса (ЭК - АРМ)

**Автоматизированное рабочее место кассира магазина электроники**

Учебный проект на .NET 8 с современным UI/UX в тёмной теме и аккуратным дизайном.

## 📋 Стек

- **.NET 8.0** - Windows Forms
- **C# 12** - язык
- **WinForms** - UI-фреймворк (кастомные контролы, современный дизайн)
- **Mock Data** - захардкоженные данные для демонстрации

## 🏗️ Структура проекта

```
ElectronCash.sln
├── ElectronCash.Api       — REST API (не трогаем, оставляем для будущего)
├── ElectronCash.Client    — WinForms приложение (основное)
└── ElectronCash.Shared    — DTO и модели (POCO)
```

## 🚀 Быстрый старт

### Требования
- .NET 8.0 SDK или выше
- Windows 10/11 для запуска готового exe

### Сборка и публикация

**На Windows (PowerShell):**
```powershell
.\build.ps1
```

**На Linux/macOS (Bash):**
```bash
./build.sh
```

Скрипты создадут автономный exe-файл в папке `./build/`:
- `./build/ElectronCash.Client.exe` — готовое к запуску приложение
- `./build/release/` — полная сборка со всеми зависимостями

### Запуск на Windows
```
.\build\ElectronCash.Client.exe
```

### Разработка

**Через bootstrap-скрипт (собирает Release-версию в bin/):**
```bash
./bootstrap.sh        # Linux/macOS
# или
.\bootstrap.ps1       # Windows (если создан)
```

**Через dotnet CLI:**
```bash
cd ElectronCash
dotnet build
dotnet run --project src/ElectronCash.Client/ElectronCash.Client.csproj
```

## 🎨 Особенности дизайна

- **Тёмная тема** с электрической бирюзой (#00D9A3) как акцент
- **Современные POS-интерфейсы** (Square, iiko, Poster в качестве вдохновения)
- **Кастомные контролы**: FlatButton, CardPanel, StatCard, SearchBox, ProductTile, CartRow
- **Плавные анимации**: fade-in/out, slide, pulse
- **Segoe UI Variable Display** для типографики

## 📦 Паллитра цветов

| Название | Hex | RGB |
|----------|-----|-----|
| Background | #0F1419 | 15, 20, 25 |
| Surface | #1A2129 | 26, 33, 41 |
| Accent | #00D9A3 | 0, 217, 163 |
| Danger | #F87171 | 248, 113, 113 |
| Success | #34D399 | 52, 211, 153 |
| Info | #60A5FA | 96, 165, 250 |

## 📱 Функциональность (Этапы разработки)

- [ ] Этап 1: Theme, Models, MockDataService, Session ✅
- [ ] Этап 2: Кастомные контролы
- [ ] Этап 3: LoginForm
- [ ] Этап 4: MainForm + навигация
- [ ] Этап 5: DashboardControl
- [ ] Этап 6: ProductsControl + фильтры
- [ ] Этап 7: OrderControl + корзина
- [ ] Этап 8: CrossSellForm
- [ ] Этап 9: PaymentForm
- [ ] Этап 10: HistoryControl + AdminControl
- [ ] Этап 11: Финальная полировка

## 🔑 Учётные записи (мок-данные)

| Login | Password | Роль | ФИ |
|-------|----------|------|-----|
| admin | admin | Admin | Администратор |
| manager | manager | Manager | Иванов И.И. |
| cashier | cashier | Cashier | Петрова А.С. |

## 📊 Мок-данные

- **40+ товаров** из категории электроники (iPhone, MacBook, Samsung, итд.)
- **10 категорий** (Смартфоны, Ноутбуки, Планшеты, Наушники, итд.)
- **15 заказов** за последние 7 дней
- **Журнал аудита** для администратора
- **IP-whitelist** и другие настройки

Все данные захардкоженные в `MockDataService`, никаких сетевых запросов.

## 📝 Лицензия

MIT