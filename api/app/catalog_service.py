"""Сервисные операции с каталогом: код товара, изображения, кросс-продажи."""

import base64
import binascii
import logging
import re
from datetime import datetime, timezone

from sqlalchemy import func, select
from sqlalchemy.orm import Session

from . import images
from .models import CrossSellOffer, Product

logger = logging.getLogger(__name__)

_CYRILLIC = {
    "а": "a", "б": "b", "в": "v", "г": "g", "д": "d", "е": "e", "ё": "e",
    "ж": "zh", "з": "z", "и": "i", "й": "y", "к": "k", "л": "l", "м": "m",
    "н": "n", "о": "o", "п": "p", "р": "r", "с": "s", "т": "t", "у": "u",
    "ф": "f", "х": "h", "ц": "c", "ч": "ch", "ш": "sh", "щ": "sch", "ъ": "",
    "ы": "y", "ь": "", "э": "e", "ю": "yu", "я": "ya",
}


def decode_base64(value: str) -> bytes:
    """base64 с необязательным data-URI префиксом."""
    if "," in value[:64] and value.lstrip().startswith("data:"):
        value = value.split(",", 1)[1]
    cleaned = re.sub(r"\s+", "", value)
    cleaned += "=" * (-len(cleaned) % 4)
    # urlsafe и обычный алфавит
    cleaned = cleaned.replace("-", "+").replace("_", "/")
    return base64.b64decode(cleaned, validate=False)


def looks_like_url(value: str) -> bool:
    lowered = value.strip().lower()
    return lowered.startswith("http://") or lowered.startswith("https://")


def slug(value: str, fallback: str) -> str:
    lowered = value.strip().lower()
    transliterated = "".join(_CYRILLIC.get(ch, ch) for ch in lowered)
    cleaned = re.sub(r"[^a-z0-9]+", "-", transliterated).strip("-")
    return (cleaned or fallback)[:24]


def _unique(db: Session, column, value: str, product_id: int | None = None) -> str:
    candidate = value
    suffix = 2
    while True:
        query = select(Product).where(column == candidate)
        if product_id is not None:
            query = query.where(Product.id != product_id)
        if db.scalar(query) is None:
            return candidate
        candidate = f"{value[:26]}-{suffix}"
        suffix += 1


def next_sku(db: Session, name: str = "") -> str:
    base = f"SK-{slug(name, 'ITEM')}" if name else "SK-ITEM"
    return _unique(db, Product.sku, base.upper())


def next_barcode(db: Session) -> str:
    """13-значный штрихкод, свободный в базе."""
    max_id = db.scalar(select(func.coalesce(func.max(Product.id), 0))) or 0
    candidate = f"20{max_id + 1:011d}"
    while db.scalar(select(Product).where(Product.barcode == candidate)) is not None:
        max_id += 1
        candidate = f"20{max_id + 1:011d}"
    return candidate[:32]


def ensure_unique(db: Session, product: Product, *, sku: str | None, barcode: str | None) -> None:
    if sku and sku != product.sku:
        if db.scalar(select(Product).where(Product.sku == sku, Product.id != product.id)):
            raise ValueError("Товар с таким артикулом уже существует")
        product.sku = sku
    if barcode and barcode != product.barcode:
        if db.scalar(select(Product).where(Product.barcode == barcode, Product.id != product.id)):
            raise ValueError("Товар с таким штрихкодом уже существует")
        product.barcode = barcode


def set_image(product: Product, data: bytes, source_url: str = "") -> bool:
    """Сохраняет новое изображение и обновляет версию кеша."""
    saved = images.save_image(product.id, data, source_url)
    if saved is None:
        return False

    filename, token = saved
    old = product.image_file
    product.image_file = filename
    product.image_token = token
    product.image_updated_at = datetime.now(timezone.utc)
    if old and old != filename:
        images.delete_image(old)
    return True


def clear_image(product: Product) -> None:
    if product.image_file:
        images.delete_image(product.image_file)
    product.image_file = ""
    product.image_token = ""
    product.image_updated_at = None


def drop_image_if_out_of_stock(product: Product) -> None:
    """Освобождаем диск: товара нет на складе — картинка больше не нужна."""
    from .config import settings

    if settings.delete_images_when_out_of_stock and product.stock <= 0:
        clear_image(product)


def sync_cross_sell_offer(db: Session, product: Product) -> None:
    """Держит дополнительное предложение в соответствии с флагом товара."""
    offer = db.scalar(select(CrossSellOffer).where(CrossSellOffer.product_id == product.id))

    if not product.is_cross_sell:
        if offer is not None:
            db.delete(offer)
        return

    if offer is None:
        offer = CrossSellOffer(product_id=product.id)
        db.add(offer)

    offer.name = product.name
    offer.description = product.description
    offer.price = product.price
    offer.icon = product.icon
    offer.category = product.cross_sell_category


def try_decode_image(value: str) -> bytes | None:
    """Из строки base64/data-URI достаёт байты картинки (None, если не вышло)."""
    try:
        return decode_base64(value)
    except (binascii.Error, ValueError):
        return None
