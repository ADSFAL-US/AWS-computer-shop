"""Минимальные миграции для уже существующих баз.

Base.metadata.create_all() создаёт только отсутствующие таблицы, но не
добавляет новые колонки. Здесь этого достаточно: ADD COLUMN IF NOT EXISTS.
"""

import logging

from sqlalchemy import text
from sqlalchemy.engine import Engine

logger = logging.getLogger(__name__)

STATEMENTS = [
    "ALTER TABLE products ADD COLUMN IF NOT EXISTS description VARCHAR(512) NOT NULL DEFAULT ''",
    "ALTER TABLE products ADD COLUMN IF NOT EXISTS image_file VARCHAR(255) NOT NULL DEFAULT ''",
    "ALTER TABLE products ADD COLUMN IF NOT EXISTS image_token VARCHAR(32) NOT NULL DEFAULT ''",
    "ALTER TABLE products ADD COLUMN IF NOT EXISTS image_updated_at TIMESTAMPTZ",
    "ALTER TABLE products ADD COLUMN IF NOT EXISTS is_cross_sell BOOLEAN NOT NULL DEFAULT FALSE",
    "ALTER TABLE products ADD COLUMN IF NOT EXISTS cross_sell_category VARCHAR(32) NOT NULL DEFAULT 'accessory'",
    (
        "ALTER TABLE cross_sell_offers ADD COLUMN IF NOT EXISTS product_id INTEGER "
        "REFERENCES products(id) ON DELETE CASCADE"
    ),
    # Удаление товара не должно ломать историю заказов: позиция заказа
    # сохраняет название/цену, а ссылка на товар обнуляется.
    "ALTER TABLE order_items ALTER COLUMN product_id DROP NOT NULL",
    (
        "ALTER TABLE order_items DROP CONSTRAINT IF EXISTS order_items_product_id_fkey"
    ),
    (
        "ALTER TABLE order_items ADD CONSTRAINT order_items_product_id_fkey "
        "FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE SET NULL"
    ),
    "DROP TABLE IF EXISTS app_meta",
]


def _migrate_images_to_columns(conn) -> None:
    """Старые базы хранили картинку строкой в image_gradient — переносим её.

    `image_gradient` остаётся для градиента-заглушки; если в нём лежала base64
    или ссылка (данные из ранних версий), сохраняем файл и чистим колонку.
    """
    from . import catalog_service, images

    rows = conn.execute(
        text(
            "SELECT id, image_gradient FROM products "
            "WHERE COALESCE(image_file, '') = '' AND COALESCE(image_gradient, '') <> ''"
        )
    ).all()

    for product_id, value in rows:
        data = None
        source = ""
        if catalog_service.looks_like_url(value):
            try:
                data = images.download_remote(value)
                source = value
            except Exception:
                data = None
        elif len(value) > 256:
            data = catalog_service.try_decode_image(value)

        if not data:
            continue

        saved = images.save_image(product_id, data, source)
        if saved is None:
            continue

        filename, token = saved
        conn.execute(
            text(
                "UPDATE products SET image_file = :f, image_token = :t, "
                "image_updated_at = NOW(), image_gradient = '' WHERE id = :id"
            ),
            {"f": filename, "t": token, "id": product_id},
        )
        logger.info("Migrated stored image for product %s", product_id)


def run_migrations(engine: Engine) -> None:
    with engine.begin() as conn:
        for statement in STATEMENTS:
            conn.execute(text(statement))
        _migrate_images_to_columns(conn)
    logger.info("Schema migrations applied")
