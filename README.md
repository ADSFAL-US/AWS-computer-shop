# Электронная касса (ЭК - АРМ)

**Автоматизированное рабочее место кассира магазина электроники**

Учебный проект на .NET 8 с современным UI/UX в тёмной теме и аккуратным дизайном.

## 📋 Стек

- **.NET 8.0** - Windows Forms (клиент)
- **C# 12** - язык
- **WinForms** - UI-фреймворк (кастомные контролы, современный дизайн)
- **Python 3.12 + FastAPI** - REST API
- **PostgreSQL 16** - база данных
- **Docker Compose** - запуск API и БД

## 🏗️ Структура проекта

```
ElectronCash.sln
├── ElectronCash.Client    — WinForms приложение (основное)
└── ElectronCash.Shared    — DTO и модели (POCO)

api/                       — FastAPI-бэкенд
├── app/
│   ├── main.py            — точка входа, lifespan, роутеры
│   ├── models.py          — SQLAlchemy-модели
│   ├── schemas.py         — Pydantic-схемы
│   ├── security.py        — PBKDF2-хеши и JWT
│   ├── migrations.py      — догоняющие ALTER'ы для существующих БД
│   ├── images.py          — хранение файлов изображений товаров
│   ├── catalog_service.py — бизнес-логика каталога
│   ├── seed.py            — создание первого администратора
│   └── routers/           — auth, catalog, products/images, orders, stats, admin
├── requirements.txt
└── Dockerfile

docker-compose.yml         — postgres + api
.env.example               — шаблон переменных окружения
```

## 🚀 Быстрый старт

### 1. Бэкенд (API + PostgreSQL)

Нужен только [Docker Desktop](https://www.docker.com/products/docker-desktop/)
(Windows/macOS) или `docker` + плагин `docker compose` (Linux). Python и
PostgreSQL ставить не нужно — всё поднимается в контейнерах.

```bash
cp .env.example .env      # при необходимости измените пароли
docker compose up -d --build
```

- API: http://localhost:8000
- Swagger: http://localhost:8000/docs
- Health: http://localhost:8000/health

При первом запуске автоматически создаются таблицы и учётная запись
администратора `admin:admin`. Товаров нет — их добавляют через админку
клиента (вручную или импортом base64), изображения сохраняются в том
`imagedata`.

#### Где что прописывать

| Файл | Что настраивается |
|------|-------------------|
| `.env` (создаётся из `.env.example`) | Пароли БД, `JWT_SECRET`, логин/пароль админа, порты, лимиты изображений |
| `docker-compose.yml` | Сервисы `db` и `api`, их порты, тома `pgdata` (БД) и `imagedata` (картинки) |
| `api/requirements.txt` | Python-зависимости (ставятся при сборке образа) |
| `api/Dockerfile` | Образ API: Python 3.12, `uvicorn app.main:app` на порту 8000 |

Переменные окружения `api` (можно задать в `.env`, подставляются через
`${...:-значение-по-умолчанию}` в `docker-compose.yml`):

| Переменная | По умолчанию | Назначение |
|------------|--------------|------------|
| `POSTGRES_DB` / `POSTGRES_USER` / `POSTGRES_PASSWORD` | `electroncash` | Доступ к PostgreSQL |
| `POSTGRES_PORT` | `5432` | Порт БД на хосте |
| `API_PORT` | `8000` | Порт API на хосте |
| `JWT_SECRET` | `change-me-in-production` | Подпись токенов — **смените в проде** |
| `ADMIN_LOGIN` / `ADMIN_PASSWORD` | `admin` / `admin` | Первый администратор |
| `IMAGE_DIR` | `/data/images` в контейнере | Каталог файлов изображений |
| `IMAGE_MAX_BYTES` | `10485760` | Максимальный размер картинки |
| `DELETE_IMAGES_WHEN_OUT_OF_STOCK` | `true` | Удалять файл, когда товар распродан |

Сменить порт API: `API_PORT=9000` в `.env` →
`docker compose up -d`. Клиент при этом запускается как
`ELECTRONCASH_API_URL=http://localhost:9000`.

#### Ежедневные команды

```bash
docker compose up -d            # запустить (после первого --build не нужен)
docker compose logs -f api      # смотреть логи API
docker compose restart api      # перезапустить API
docker compose down             # остановить контейнеры (данные сохраняются)
docker compose down -v          # остановить и УДАЛИТЬ базу и картинки
```

Полностью пустая база (все товары/заказы/категории и файлы картинок):

```bash
docker compose exec -T db psql -U electroncash -d electroncash \
  -c "TRUNCATE order_items, orders, cross_sell_offers, products, categories RESTART IDENTITY CASCADE;"
docker compose exec -T api sh -c "rm -f /data/images/*"
```

> `docker compose down -v` удаляет и том `pgdata`, и том `imagedata` — после него
> API стартует с чистой базой и заново создаёт администратора.

### 2. Клиент

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

Адрес API по умолчанию — `http://localhost:8000`. Переопределяется переменной
окружения `ELECTRONCASH_API_URL`.

### Логи приложения

Клиент — WinExe без консоли, поэтому все необработанные ошибки пишутся в файл:

```
%LOCALAPPDATA%\ElectronCash\client.log
```

Файл ротируется в `client.log.old` при достижении 1 МБ.

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

**API без Docker:**
```bash
python -m venv .venv
.venv\Scripts\activate          # Windows
pip install -r api/requirements.txt
set DATABASE_URL=postgresql+psycopg2://electroncash:electroncash@localhost:5432/electroncash
uvicorn app.main:app --reload --app-dir api
```

## 🔌 Эндпоинты API

| Метод | Путь | Описание |
|-------|------|----------|
| POST | `/api/auth/login` | Вход, возвращает JWT и пользователя |
| GET | `/api/auth/me` | Текущий пользователь |
| GET | `/api/categories` | Категории |
| GET | `/api/products` | Товары (search, category_id, tags, min_price, max_price, only_in_stock) |
| GET | `/api/products/{id}` | Товар по ID |
| GET | `/api/products/barcode/{barcode}` | Товар по штрихкоду |
| GET | `/api/products/{id}/image` | Метаданные изображения (есть ли, версия, ссылка) |
| GET | `/api/products/{id}/image/file` | Скачать файл изображения |
| GET | `/api/cross-sell-offers` | Кросс-продажи (привязаны к товарам склада) |
| GET | `/api/orders` | Заказы |
| GET | `/api/orders/{id}` | Заказ по ID |
| POST | `/api/orders` | Создать заказ (списывает остатки при `Paid`) |
| GET | `/api/stats` | Выручка за день, средний чек, заказы, остатки |
| GET | `/api/stats/top-products` | Топ товаров по продажам |
| GET | `/api/stats/sales` | Продажи за 7 дней |
| GET/POST/PUT/DELETE | `/api/admin/users` | Управление пользователями (Admin) |
| GET/POST | `/api/admin/categories` | Категории (Admin) |
| GET/POST/PUT/DELETE | `/api/admin/products` | Товары (Admin) |
| POST/DELETE | `/api/admin/products/{id}/image` | Загрузка/удаление изображения (Admin) |
| POST | `/api/admin/products/import` | Импорт из base64-списка (Admin) |
| POST | `/api/admin/products/maintenance/cleanup-images` | Удаление «осиротевших» файлов |
| GET | `/api/admin/audit-log` | Журнал аудита (Admin) |
| GET/POST/DELETE | `/api/admin/ip-whitelist` | IP-whitelist (Admin) |

Все эндпоинты, кроме `/api/auth/login` и `/health`, требуют заголовок
`Authorization: Bearer <token>`.

## 🖼️ Изображения товаров

Файлы лежат отдельными файлами на диске (`IMAGE_DIR`, в Docker — том
`imagedata`). В базе хранятся имя файла и токен версии.

Схема обмена:

1. Клиент получает товар с `has_image` и `image_token`.
2. Если картинки этой версии нет в локальном кеше — запрашивает
   `GET /api/products/{id}/image`. Бэкенд отвечает, где лежит файл и по какой
   ссылке его скачать.
3. Клиент скачивает файл по `download_url` и кеширует его на диске
   (`%LOCALAPPDATA%\ElectronCash\image-cache`), проверяя версию по токену.
4. Когда товар пропадает из UI (распродан/удалён) — клиент удаляет его картинку
   из кеша, а бэкенд удаляет файл при `stock = 0` или удалении товара.
5. Просроченные записи кеша (> 30 дней) вычищаются при обновлении каталога.

Загрузка изображения — в админке: файл с диска или ссылка (`http/https`),
бэкенд скачивает и сохраняет файл сам.

## 📥 Импорт товаров (второй способ)

В админке: «Товары → Импорт». На вход подаётся **base64 от JSON-массива**,
внутри которого могут быть ссылки на изображения:

```json
[
  {
    "name": "Sony WH-1000XM5",
    "price": 42999,
    "stock": 10,
    "category": "Наушники",
    "image_url": "https://example.com/photo.jpg",
    "is_cross_sell": false
  }
]
```

Бэкенд (`POST /api/admin/products/import`) декодирует base64, разбирает JSON,
скачивает картинки по ссылкам и сохраняет файлы у себя. Повторный импорт
обновляет товары по `sku`/`barcode`, а без них — по названию.
В ответ приходит отчёт по каждому товару (создан/обновлён/ошибка).


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

## 📱 Функциональность

- [x] Тёмная тема, кастомные контролы, адаптивная раскладка
- [x] Авторизация (JWT), роли Admin / Cashier
- [x] Каталог из БД: поиск, фильтр по категориям, остатки
- [x] Корзина: количество с учётом склада, удаление, итог
- [x] Доп. предложения (кросс-продажи) с добавлением в заказ
- [x] Оформление заказа со списанием остатков
- [x] Админка: товары (CRUD, фото файлом/ссылкой), категории, импорт base64
- [x] Изображения: хранение на диске, раздача по токену версии, кеш клиента
- [x] Пользователи, журнал аудита, IP-whitelist

## 🔑 Учётные записи

В базе изначально есть только администратор:

| Login | Password | Роль | ФИ |
|-------|----------|------|-----|
| admin | admin | Admin | Администратор |

Остальных пользователей (кассиров, менеджеров) администратор создаёт
в разделе «Админка → Пользователи».

## 📊 Стартовые данные

Демо-каталог **не засеивается**. При первом старте API создаётся только
администратор; товары и категории добавляются через админку клиента:

- вручную («Товары → Добавить») с загрузкой изображения файлом или ссылкой;
- импортом base64 («Товары → Импорт») — пример формата лежит в
  [`testdata/products.base64.txt`](testdata/products.base64.txt).

Категории при импорте создаются автоматически по полю `category`, изображения
по `image_url` скачиваются бэкендом и сохраняются на диск.

## 📝 Лицензия

MIT